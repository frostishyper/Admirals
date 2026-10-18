using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-turn snapshot of game state built by querying GameMaster APIs.
/// Provides a consistent view of the participant's units, visible enemies,
/// and movement phase state. Built once at phase start and shared across
/// all AI subsystems to avoid redundant queries.
/// </summary>
public class AITurnContext
{
    /// <summary>
    /// The participant this context represents (the AI player).
    /// </summary>
    public RuntimeParticipant OwnParticipant { get; set; }

    /// <summary>
    /// The opposing participant in this match.
    /// </summary>
    public RuntimeParticipant EnemyParticipant { get; set; }

    /// <summary>
    /// All units belonging to the AI participant (includes destroyed units).
    /// </summary>
    public List<RuntimeUnit> OwnUnits { get; set; }

    /// <summary>
    /// Alive units belonging to the AI participant (LifeState == Active).
    /// Filtered subset of OwnUnits used for tactical decision-making.
    /// </summary>
    public List<RuntimeUnit> OwnAliveUnits { get; set; }

    /// <summary>
    /// Enemy units currently visible to the AI participant through fog-of-war.
    /// Queried via GameKnowledge.IsUnitVisibleToParticipant for each enemy unit.
    /// Empty list during Hunt stance or when all enemies are concealed.
    /// </summary>
    public List<RuntimeUnit> EnemyVisibleUnits { get; set; }

    /// <summary>
    /// Reference to the authoritative game map instance.
    /// Used for terrain queries, tile validation, and spatial calculations.
    /// </summary>
    public GameMap GameMap { get; set; }

    /// <summary>
    /// Reference to the grid manager for coordinate system and map dimensions.
    /// </summary>
    public GridManager GridManager { get; set; }

    /// <summary>
    /// Current gameplay phase (Deployment, Move, Search, Battle, etc.).
    /// </summary>
    public GameplayPhase CurrentPhase { get; set; }

    /// <summary>
    /// Current turn number (1-indexed, starts at 1 after deployment completes).
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Maximum number of units that can move during this Movement phase.
    /// Calculated as (ActiveUnits + 1) / 2 by GameMovement.
    /// Only valid during Movement phase.
    /// </summary>
    public int MovementLimit { get; set; }

    /// <summary>
    /// Number of units that have successfully committed movements this turn.
    /// Tracked by GameMovement and incremented on each successful TryCommitMovement.
    /// Only valid during Movement phase.
    /// </summary>
    public int MovesCommitted { get; set; }

    /// <summary>
    /// Whether the AI participant can currently submit movement actions.
    /// False if the other participant has priority or movement phase has ended.
    /// Only valid during Movement phase.
    /// </summary>
    public bool CanAct { get; set; }

    /// <summary>
    /// Initializes a new turn context with empty collections.
    /// Use the static Build factory method to construct a complete context from GameMaster.
    /// </summary>
    public AITurnContext()
    {
        OwnUnits = new List<RuntimeUnit>();
        OwnAliveUnits = new List<RuntimeUnit>();
        EnemyVisibleUnits = new List<RuntimeUnit>();
    }

    /// <summary>
    /// Factory method to build a complete turn context by querying GameMaster APIs.
    /// Performs all necessary participant-filtered queries to construct a consistent
    /// view of game state for the specified participant.
    /// </summary>
    /// <param name="gameMaster">The authoritative GameMaster orchestrator instance.</param>
    /// <param name="ownParticipant">The participant this context represents (the AI player).</param>
    /// <returns>A fully populated AITurnContext instance.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if gameMaster or ownParticipant is null.</exception>
    public static AITurnContext Build(GameMaster gameMaster, RuntimeParticipant ownParticipant)
    {
        if (gameMaster == null)
        {
            throw new System.ArgumentNullException(nameof(gameMaster));
        }

        if (ownParticipant == null)
        {
            throw new System.ArgumentNullException(nameof(ownParticipant));
        }

        AITurnContext context = new AITurnContext();
        context.OwnParticipant = ownParticipant;
        context.CurrentPhase = gameMaster.CurrentPhase;
        context.TurnNumber = gameMaster.TurnNumber;
        context.GameMap = gameMaster.GameMap;

        // Determine enemy participant
        if (ReferenceEquals(ownParticipant, gameMaster.ParticipantA))
        {
            context.EnemyParticipant = gameMaster.ParticipantB;
        }
        else if (ReferenceEquals(ownParticipant, gameMaster.ParticipantB))
        {
            context.EnemyParticipant = gameMaster.ParticipantA;
        }
        else
        {
            Debug.LogError("[AI][TurnContext] OwnParticipant does not match either Match participant");
            context.EnemyParticipant = null;
        }

        // Query own participant's unit roster
        if (gameMaster.GameUnits.TryGetRoster(ownParticipant, out ParticipantUnitRoster ownRoster))
        {
            foreach (RuntimeUnit unit in ownRoster.RuntimeUnits)
            {
                if (unit != null)
                {
                    context.OwnUnits.Add(unit);

                    if (unit.LifeState == RuntimeUnitLifeState.Active)
                    {
                        context.OwnAliveUnits.Add(unit);
                    }
                }
            }
        }

        // Query enemy participant's unit roster and filter by visibility
        if (context.EnemyParticipant != null &&
            gameMaster.GameUnits.TryGetRoster(context.EnemyParticipant, out ParticipantUnitRoster enemyRoster))
        {
            foreach (RuntimeUnit enemyUnit in enemyRoster.RuntimeUnits)
            {
                if (enemyUnit != null &&
                    enemyUnit.LifeState == RuntimeUnitLifeState.Active &&
                    gameMaster.GameKnowledge.IsUnitVisibleToParticipant(ownParticipant, enemyUnit))
                {
                    context.EnemyVisibleUnits.Add(enemyUnit);
                }
            }
        }

        // Query movement phase state if applicable
        if (context.CurrentPhase == GameplayPhase.Movement &&
            gameMaster.GameMovement != null &&
            gameMaster.GameMovement.TryGetView(ownParticipant, out GameMovement.ParticipantView movementView))
        {
            context.MovementLimit = movementView.MovementLimit;
            context.MovesCommitted = movementView.MovesCommitted;
            context.CanAct = movementView.CanAct;
        }

        // Find GridManager in scene (required for spatial calculations)
        context.GridManager = Object.FindObjectOfType<GridManager>();
        if (context.GridManager == null)
        {
            Debug.LogWarning("[AI][TurnContext] GridManager not found in scene");
        }

        return context;
    }
}
