using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Per-turn snapshot of game state built from ParticipantGameSnapshot.
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
    /// Uses ParticipantUnitSnapshot instead of RuntimeUnit for snapshot-based architecture.
    /// Filtered subset of OwnUnits used for tactical decision-making.
    /// </summary>
    public List<ParticipantUnitSnapshot> OwnAliveUnits { get; set; }

    /// <summary>
    /// Enemy units currently visible to the AI participant through fog-of-war.
    /// Uses ParticipantUnitSnapshot instead of RuntimeUnit for snapshot-based architecture.
    /// Empty list during Hunt stance or when all enemies are concealed.
    /// </summary>
    public List<VisibleEnemyUnitSnapshot> EnemyVisibleUnits { get; set; }

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
    /// Use the static Build factory methods to construct a complete context.
    /// </summary>
    public AITurnContext()
    {
        OwnUnits = new List<RuntimeUnit>();
        OwnAliveUnits = new List<ParticipantUnitSnapshot>();
        EnemyVisibleUnits = new List<VisibleEnemyUnitSnapshot>();
    }

    /// <summary>
    /// Factory method to build turn context from ParticipantGameSnapshot (snapshot-based architecture).
    /// This is the preferred constructor for ParticipantAIController implementations.
    /// </summary>
    /// <param name="snapshot">The participant's game snapshot from ParticipantAccess.</param>
    /// <param name="ownParticipant">The participant this context represents.</param>
    /// <param name="enemyParticipant">The opposing participant.</param>
    /// <returns>A fully populated AITurnContext using snapshot data.</returns>
    public static AITurnContext BuildFromSnapshot(
        ParticipantGameSnapshot snapshot,
        RuntimeParticipant ownParticipant,
        RuntimeParticipant enemyParticipant)
    {
        if (snapshot == null)
        {
            throw new System.ArgumentNullException(nameof(snapshot));
        }

        if (ownParticipant == null)
        {
            throw new System.ArgumentNullException(nameof(ownParticipant));
        }

        AITurnContext context = new AITurnContext();
        context.OwnParticipant = ownParticipant;
        context.EnemyParticipant = enemyParticipant;
        context.CurrentPhase = snapshot.CurrentPhase;
        context.TurnNumber = snapshot.TurnNumber;

        // Extract alive own units from snapshot
        context.OwnAliveUnits = snapshot.OwnUnits
            .Where(u => u.LifeState == RuntimeUnitLifeState.Active)
            .ToList();

        // Extract visible enemy units from snapshot
        context.EnemyVisibleUnits = snapshot.VisibleEnemies != null ? snapshot.VisibleEnemies.ToList() : new List<VisibleEnemyUnitSnapshot>();

        // Movement phase state (if applicable)
        if (snapshot.CurrentPhase == GameplayPhase.Movement && snapshot.Movement != null)
        {
            context.MovementLimit = snapshot.Movement.MovementLimit;
            context.MovesCommitted = snapshot.Movement.MovesCommitted;
            context.CanAct = snapshot.Movement.CanAct;
        }

        return context;
    }

    /// <summary>
    /// Legacy factory method to build turn context from GameMaster (direct query architecture).
    /// Retained for compatibility with existing AI controllers that haven't migrated to snapshot-based architecture.
    /// New implementations should use BuildFromSnapshot instead.
    /// </summary>
    /// <param name="gameMaster">The authoritative GameMaster orchestrator instance.</param>
    /// <param name="ownParticipant">The participant this context represents (the AI player).</param>
    /// <returns>A fully populated AITurnContext instance.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if gameMaster or ownParticipant is null.</exception>

    /// <summary>
    /// Helper method to create a ParticipantUnitSnapshot from a RuntimeUnit.
    /// Used by the legacy Build method to convert RuntimeUnit references to snapshots.
    /// </summary>
}


