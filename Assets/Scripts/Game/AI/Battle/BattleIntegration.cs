using UnityEngine;

/// <summary>
/// Provides utility methods for managing battle context and querying unit state.
/// Supports integration between BattleDecider and StrategicAdmiralAIController.
/// </summary>
/// <remarks>
/// Validates: Requirements 4.1, 10.1, 10.2, 10.3, 10.4, 11.4
/// 
/// This static utility class offers four key integration functions:
/// 1. ResetBattleContext - Clears fleet coordination tracking between Battle phases
/// 2. IsUnitOperationalForBattle - Validates unit readiness for combat actions
/// 3. FindOwnUnit - Locates units in the participant's snapshot by runtime ID
/// 4. FindVisibleEnemyAtCoordinate - Locates enemy units at specific coordinates
/// 
/// These utilities support proper state management and unit validation throughout
/// the Battle phase decision flow.
/// </remarks>
public static class BattleIntegration
{
    /// <summary>
    /// Resets the BattleContext to clear fleet coordination tracking.
    /// Should be called at the start of each Battle phase to reset per-phase state.
    /// </summary>
    /// <param name="context">Battle context to reset (passed by reference).</param>
    /// <remarks>
    /// Validates: Requirements 6.2, 11.4
    /// 
    /// This method clears the EngagedTargetsThisPhase set, ensuring that
    /// coordination penalties from the previous Battle phase don't affect
    /// the current phase's target selection. Other context fields (Snapshot, Access)
    /// are not modified as they should be updated by the caller when appropriate.
    /// </remarks>
    public static void ResetBattleContext(ref BattleContext context)
    {
        // Clear engaged targets tracking to start fresh each Battle phase
        context.EngagedTargetsThisPhase?.Clear();
    }

    /// <summary>
    /// Checks whether a unit is operational and ready for battle actions.
    /// </summary>
    /// <param name="unit">Unit snapshot to validate.</param>
    /// <returns>
    /// True if the unit is on the map, has an active life state, and is operational for battle.
    /// False otherwise.
    /// </returns>
    /// <remarks>
    /// Validates: Requirements 10.1, 10.2
    /// 
    /// A unit is considered operational for battle if:
    /// 1. It is currently placed on the map (IsOnMap = true)
    /// 2. It has an Active life state (not Destroyed, Retreated, etc.)
    /// 
    /// Note: This method does NOT check HasAttacked status. That check is performed
    /// at the BattleDecider level using GameBattle.UnitView, as HasAttacked is tracked
    /// per Battle phase and not part of the unit snapshot.
    /// 
    /// This check ensures the AI doesn't attempt to issue battle commands for units
    /// that have been removed from play or are otherwise incapacitated.
    /// </remarks>
    public static bool IsUnitOperationalForBattle(ParticipantUnitSnapshot unit)
    {
        // Null check
        if (unit == null)
            return false;

        // Must be on map and in active life state
        return unit.IsOnMap && unit.LifeState == RuntimeUnitLifeState.Active;
    }

    /// <summary>
    /// Finds a unit in the participant's snapshot by runtime unit ID.
    /// </summary>
    /// <param name="snapshot">Game state snapshot containing own units.</param>
    /// <param name="runtimeUnitID">Runtime identifier of the unit to find.</param>
    /// <returns>
    /// ParticipantUnitSnapshot if found, null otherwise.
    /// </returns>
    /// <remarks>
    /// Validates: Requirements 10.1
    /// 
    /// This helper method provides a convenient way to locate units in the
    /// OwnUnits collection by their runtime ID. Used by BattleDecider when
    /// identifying which unit should take action during the Battle phase.
    /// 
    /// Returns null if the unit ID is empty/null, the snapshot is null,
    /// or no matching unit is found in the collection.
    /// </remarks>
    public static ParticipantUnitSnapshot FindOwnUnit(ParticipantGameSnapshot snapshot, string runtimeUnitID)
    {
        // Validate inputs
        if (snapshot == null || string.IsNullOrEmpty(runtimeUnitID))
            return null;

        // Search through own units
        for (int i = 0; i < snapshot.OwnUnits.Length; i++)
        {
            if (snapshot.OwnUnits[i].RuntimeUnitID == runtimeUnitID)
                return snapshot.OwnUnits[i];
        }

        // Not found
        return null;
    }

    /// <summary>
    /// Finds a visible enemy unit at the specified coordinate.
    /// </summary>
    /// <param name="coordinate">Map coordinate to search.</param>
    /// <param name="snapshot">Game state snapshot containing visible enemies.</param>
    /// <returns>
    /// VisibleEnemyUnitSnapshot if found, null otherwise.
    /// </returns>
    /// <remarks>
    /// Validates: Requirements 4.1
    /// 
    /// This helper method searches through the VisibleEnemies collection to find
    /// an enemy unit occupying the specified coordinate. Used by TargetScorer and
    /// BattleDecider when evaluating potential engagement targets.
    /// 
    /// Returns null if the snapshot is null, VisibleEnemies is null/empty,
    /// or no enemy is found occupying the specified coordinate.
    /// </remarks>
    public static VisibleEnemyUnitSnapshot FindVisibleEnemyAtCoordinate(
        Vector2Int coordinate,
        ParticipantGameSnapshot snapshot)
    {
        // Validate inputs
        if (snapshot == null || snapshot.VisibleEnemies == null)
            return null;

        // Search through visible enemies
        foreach (var enemy in snapshot.VisibleEnemies)
        {
            if (enemy != null && enemy.OccupiedCoordinates != null)
            {
                // Check if enemy occupies the specified coordinate
                foreach (var coord in enemy.OccupiedCoordinates)
                {
                    if (coord == coordinate)
                        return enemy;
                }
            }
        }

        // Not found
        return null;
    }
}
