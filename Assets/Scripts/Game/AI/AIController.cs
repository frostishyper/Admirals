using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main AI orchestrator MonoBehaviour that coordinates AI subsystems and manages phase transitions.
/// This component should be attached to a GameObject in the scene with an assigned RuntimeParticipant.
/// 
/// The AIController subscribes to GameMaster phase events and coordinates:
/// - Deployment: Initial fleet placement via DeploymentPlanner
/// - Movement: Strategic stance evaluation and unit movement via FleetCommander and MovementPrioritizer
/// - Search: Active scan planning (future implementation)
/// - Battle: Target selection and fire coordination (future implementation)
/// 
/// The controller enforces movement limitations, tracks claimed targets to prevent convergence,
/// and maintains all turn-scoped state. All interactions with GameMaster occur through
/// participant-filtered APIs to respect fog-of-war and authorization rules.
/// </summary>
public class AIController : MonoBehaviour
{
    /// <summary>
    /// The RuntimeParticipant this AI controller manages.
    /// Must be assigned in the Inspector before the match starts.
    /// </summary>
    [SerializeField]
    private RuntimeParticipant assignedParticipant;

    // GameMaster reference found during initialization
    private GameMaster gameMaster;

    // Strategic layer - lazily initialized on first Move phase
    private FleetCommander fleetCommander;

    // Turn-scoped state (cleared at phase start)
    private HashSet<RuntimeUnit> claimedTargets;
    private HashSet<string> movedUnitIDs;
    private int movesCommitted;

    // Phase tracking for change detection
    private GameplayPhase lastObservedPhase = GameplayPhase.None;

    /// <summary>
    /// Initializes the AI controller by finding the GameMaster in the scene.
    /// Validates that assignedParticipant is properly configured.
    /// </summary>
    private void Awake()
    {
        // Find GameMaster in scene (required for all AI operations)
        gameMaster = FindObjectOfType<GameMaster>();

        if (gameMaster == null)
        {
            Debug.LogError("[AI][Controller] GameMaster not found in scene. AIController cannot function.");
            enabled = false;
            return;
        }

        // Validate assigned participant
        if (assignedParticipant == null)
        {
            Debug.LogError("[AI][Controller] No RuntimeParticipant assigned. Please assign a participant in the Inspector.");
            enabled = false;
            return;
        }

        // Initialize turn-scoped state collections
        claimedTargets = new HashSet<RuntimeUnit>();
        movedUnitIDs = new HashSet<string>();
        movesCommitted = 0;

        Debug.Log($"[AI][Controller] Initialized for participant: {assignedParticipant.ParticipantName}");
    }

    /// <summary>
    /// Subscribe to GameMaster phase change events when the controller is enabled.
    /// NOTE: GameMaster does not currently expose a PhaseChanged event. This will be
    /// implemented once the event system is added to GameMaster.
    /// For now, we use Update() polling to detect phase changes.
    /// </summary>
    private void OnEnable()
    {
        // TODO: Subscribe to GameMaster.PhaseChanged event when it's implemented
        // gameMaster.PhaseChanged += OnPhaseChanged;

        // Reset phase tracking
        lastObservedPhase = GameplayPhase.None;

        Debug.Log("[AI][Controller] AI controller enabled and ready for phase events");
    }

    /// <summary>
    /// Unsubscribe from GameMaster phase change events and clear turn state.
    /// Ensures clean shutdown and prevents memory leaks.
    /// </summary>
    private void OnDisable()
    {
        // TODO: Unsubscribe from GameMaster.PhaseChanged event when it's implemented
        // if (gameMaster != null)
        // {
        //     gameMaster.PhaseChanged -= OnPhaseChanged;
        // }

        // Clear any turn-scoped state
        ClearTurnState();

        Debug.Log("[AI][Controller] AI controller disabled and cleaned up");
    }

    /// <summary>
    /// Temporary polling mechanism to detect phase changes until GameMaster.PhaseChanged event is implemented.
    /// This should be replaced with event-driven logic once the event system is available.
    /// </summary>
    private void Update()
    {
        if (gameMaster == null || !gameMaster.InitializationComplete)
        {
            return;
        }

        // Detect phase change by comparing current phase with last observed phase
        GameplayPhase currentPhase = gameMaster.CurrentPhase;

        if (currentPhase != lastObservedPhase)
        {
            lastObservedPhase = currentPhase;
            OnPhaseChanged(currentPhase);
        }
    }

    /// <summary>
    /// Handles phase transition events from GameMaster.
    /// Dispatches to appropriate phase handlers after validating participant identity.
    /// </summary>
    /// <param name="newPhase">The new gameplay phase that just began.</param>
    private void OnPhaseChanged(GameplayPhase newPhase)
    {
        if (gameMaster == null || assignedParticipant == null)
        {
            return;
        }

        // Validate that this AI should act in this phase
        // NOTE: Phase-specific participant validation will occur in individual handlers
        // as different phases may have different activation rules (e.g., deployment is per-participant,
        // but movement/battle may depend on which participant's turn it is)

        Debug.Log($"[AI][Controller] Phase changed to: {newPhase}");

        // Clear turn-scoped state at the start of each phase
        ClearTurnState();

        // Dispatch to appropriate phase handler
        switch (newPhase)
        {
            case GameplayPhase.Deployment:
                HandleDeploymentPhase();
                break;

            case GameplayPhase.Movement:
                HandleMovementPhase();
                break;

            case GameplayPhase.Search:
                HandleSearchPhase();
                break;

            case GameplayPhase.Battle:
                HandleBattlePhase();
                break;

            case GameplayPhase.PreBattle:
            case GameplayPhase.End:
            case GameplayPhase.None:
                // No AI action needed for these phases
                break;

            default:
                Debug.LogWarning($"[AI][Controller] Unhandled phase: {newPhase}");
                break;
        }
    }

    /// <summary>
    /// Handles Deployment phase: initial fleet placement on deployment zone.
    /// Validates participant identity and delegates to DeploymentPlanner.
    /// </summary>
    private void HandleDeploymentPhase()
    {
        try
        {
        Debug.Log("[AI][Controller] Deployment phase - starting fleet placement");

        // Build turn context
        AITurnContext context = BuildTurnContext();

        // Validate that we can act in deployment phase
        if (!context.CanAct)
        {
            Debug.Log($"[AI][Controller] Cannot act in deployment phase (CanAct = false)");
            return;
        }

        // Lazy-initialize FleetCommander for initial snapshot generation
        if (fleetCommander == null)
        {
            int startingFleetSize = context.OwnUnits.Count;

            fleetCommander = new FleetCommander(
                context.GridManager,
                context.GameMap,
                assignedParticipant,
                startingFleetSize
            );

            Debug.Log($"[AI][Controller] FleetCommander initialized with {startingFleetSize} starting units");
        }

        // Generate initial fleet snapshot for role classification
        FleetSnapshot snapshot = fleetCommander.Evaluate(context);

        // Execute deployment via DeploymentPlanner
        bool success = DeploymentPlanner.ExecuteDeployment(
            context,
            snapshot,
            gameMaster.GameDeployment,
            context.GridManager
        );

        if (!success)
        {
            Debug.LogError("[AI][Controller] Deployment planning failed");
        }
        else
        {
            Debug.Log("[AI][Controller] Deployment planning completed successfully");
        }
    }

    /// <summary>
    /// Handles Movement phase: strategic stance evaluation and unit movement.
    /// Validates participant identity and delegates to FleetCommander and MovementPrioritizer.
    /// Processes unit movements sequentially until MovementLimit is reached.
    /// </summary>
    private void HandleMovementPhase()
    {
        try
        {
        Debug.Log("[AI][Controller] Movement phase - starting movement planning");

        // Build turn context
        AITurnContext context = BuildTurnContext();

        // Validate that we can act in movement phase
        if (!context.CanAct)
        {
            Debug.Log($"[AI][Controller] Cannot act in movement phase (CanAct = false)");
            return;
        }

        // Lazy-initialize FleetCommander on first Movement phase
        if (fleetCommander == null)
        {
            int startingFleetSize = context.OwnUnits.Count;

            fleetCommander = new FleetCommander(
                context.GridManager,
                context.GameMap,
                assignedParticipant,
                startingFleetSize
            );

            Debug.Log($"[AI][Controller] FleetCommander initialized with {startingFleetSize} starting units");
        }

        // Evaluate fleet stance and generate snapshot
        FleetSnapshot snapshot = fleetCommander.Evaluate(context);

        Debug.Log($"[AI][Controller] Fleet stance: {snapshot.CurrentStance}, MovementLimit: {context.MovementLimit}");

        // Prioritize units for movement
        List<RuntimeUnit> prioritizedUnits = MovementPrioritizer.PrioritizeUnits(
            context,
            snapshot,
            gameMaster.GameMovement,
            context.GridManager
        );

        Debug.Log($"[AI][Controller] Prioritized {prioritizedUnits.Count} units for movement consideration");

        // Process unit movements sequentially until MovementLimit reached
        int unitsProcessed = 0;
        int successfulMoves = 0;

        foreach (RuntimeUnit unit in prioritizedUnits)
        {
            // Check if we've reached movement limit
            if (context.MovesCommitted >= context.MovementLimit)
            {
                Debug.Log($"[AI][Controller] Movement limit reached ({context.MovesCommitted}/{context.MovementLimit})");
                break;
            }

            // Skip if unit already moved (shouldn't happen with proper prioritization, but defensive check)
            if (movedUnitIDs.Contains(unit.RuntimeID))
            {
                Debug.LogWarning($"[AI][Controller] Unit {unit.RuntimeID} already moved, skipping");
                continue;
            }

            unitsProcessed++;

            // Try to move the unit
            bool moved = TryMoveUnit(unit, context, snapshot);
            if (moved)
            {
                successfulMoves++;
                movedUnitIDs.Add(unit.RuntimeID);
                movesCommitted++;
                
                // Rebuild context to get updated positions
                context = BuildTurnContext();
            }
        }

        Debug.Log($"[AI][Controller] Movement phase complete: processed {unitsProcessed} units, {successfulMoves} successful moves");
    }


    /// <summary>
    /// Attempts to move a single unit to its best tactical destination.
    /// Calls MovementPlanner to select destination, validates movement with GameMovement,
    /// and commits if valid. Updates claimedTargets for target coordination.
    /// Implements Requirements 3.4-3.7, 13.1-13.6, 20.1-20.5.
    /// </summary>
    /// <param name="unit">The unit to move.</param>
    /// <param name="context">Current turn context with game state.</param>
    /// <param name="snapshot">Fleet snapshot with stance and tactical state.</param>
    /// <returns>True if movement was successfully committed; false if no valid move found or commit failed.</returns>
    private bool TryMoveUnit(RuntimeUnit unit, AITurnContext context, FleetSnapshot snapshot)
    {
        if (unit == null || context == null || snapshot == null)
        {
            Debug.LogError("[AI][Controller] Null parameter in TryMoveUnit");
            return false;
        }

        // Call MovementPlanner to choose best destination
        MovementPlanner.Decision decision = MovementPlanner.ChooseDestination(
            unit,
            context,
            snapshot,
            claimedTargets
        );

        // Validate movement with GameMovement API
        GameMovement.ValidationResult validationResult = gameMaster.GameMovement.ValidateMovement(
            assignedParticipant,
            unit,
            decision.destination
        );

        if (!validationResult.IsValid)
        {
            Debug.Log($"[AI][Controller] Movement validation failed for {unit.RuntimeID}: {validationResult.ErrorMessage}");
            return false;
        }

        // Commit the movement atomically
        GameMovement.CommitResult commitResult = gameMaster.GameMovement.TryCommitMovement(
            assignedParticipant,
            unit,
            decision.destination
        );

        if (!commitResult.Success)
        {
            Debug.LogWarning($"[AI][Controller] Movement commit failed for {unit.RuntimeID}: {commitResult.ErrorMessage}");
            return false;
        }

        // Update claimed targets if this move is targeting a specific enemy
        if (decision.likelyTarget != null)
        {
            claimedTargets.Add(decision.likelyTarget);
        }

        // Log the movement
        string intent = decision.isFleeing
            ? "fleeing to safety"
            : decision.likelyTarget != null
                ? $"moving toward {decision.likelyTarget.RuntimeID}"
                : "repositioning";

        Debug.Log($"[AI][Controller] {unit.RuntimeID} moved to {decision.destination} ({intent})");

        return true;
    }
    /// <summary>
    /// Handles Search phase: active scan planning for fog-of-war reconnaissance.
    /// Future implementation for active scanning abilities.
    /// Currently a stub - no action taken.
    /// </summary>
    private void HandleSearchPhase()
    {
        try
        {
            Debug.Log("[AI][Controller] Search phase - executing scanner selection");

            AITurnContext context = BuildTurnContext();

            if (!context.CanAct)
            {
                Debug.Log($"[AI][Controller] Cannot act in search phase (CanAct = false)");
                return;
            }

            // Execute search via SearchPlanner
            SearchPlanner.ExecuteSearch(context, gameMaster);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AI][Controller] Exception in search phase: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handles Battle phase: target selection and fire coordination.
    /// Future implementation for weapon targeting and attack execution.
    /// Currently a stub - no action taken.
    /// </summary>
    private void HandleBattlePhase()
    {
        Debug.Log("[AI][Controller] Battle phase - no target selection implemented yet");

        // Future implementation:
        // AITurnContext context = BuildTurnContext();
        //
        // if (!context.CanAct)
        // {
        //     return;
        // }
        //
        // // Refresh FleetCommander contacts (don't change stance mid-turn)
        // FleetSnapshot snapshot = fleetCommander.RefreshContacts(context);
        //
        // // Prioritize targets based on threat and opportunity
        // List<RuntimeUnit> prioritizedTargets = ...;
        //
        // // For each own unit with weapons:
        // //   - Select best target from prioritized list
        // //   - Execute attack via GameMaster.GameBattle (when implemented)
        // //   - Track claimed targets to spread fire
    }

    /// <summary>
    /// Clears all turn-scoped state at the start of each phase.
    /// Prevents state leakage between phases and turns.
    /// Implements Requirement 27.4: Reset claimed targets, move counters at phase start.
    /// </summary>
    private void ClearTurnState()
    {
        claimedTargets?.Clear();
        movedUnitIDs?.Clear();
        movesCommitted = 0;

        Debug.Log("[AI][Controller] Turn-scoped state cleared");
    }

    /// <summary>
    /// Builds a fresh AITurnContext by querying GameMaster APIs.
    /// Encapsulates all participant-filtered data access in one place.
    /// </summary>
    /// <returns>A populated AITurnContext with current game state.</returns>
    private AITurnContext BuildTurnContext()
    {
        return AITurnContext.Build(gameMaster, assignedParticipant);
    }
}
