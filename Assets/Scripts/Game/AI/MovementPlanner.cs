using System.Collections.Generic;
using UnityEngine;
using Admirals.AI;

/// <summary>
/// Tactical destination selection system that scores potential tiles for individual units.
/// Evaluates attack opportunities, danger assessment, formation cohesion, and stance-driven
/// directional bias to select optimal movement destinations. Works within the movement budget
/// and pathfinding constraints provided by GameMovement.
/// 
/// This is the core tactical decision-maker that translates FleetStance into concrete movements.
/// </summary>
public static class MovementPlanner
{
    /// <summary>
    /// Movement decision containing destination, target information, and flee status.
    /// </summary>
    public struct Decision
    {
        /// <summary>
        /// Selected destination tile coordinates.
        /// </summary>
        public Vector2Int destination;

        /// <summary>
        /// Enemy unit being targeted from this destination (null if repositioning).
        /// Informational only - actual attacks happen in Battle phase.
        /// </summary>
        public RuntimeUnit likelyTarget;

        /// <summary>
        /// True if this movement is a flee-to-safety override for critically damaged units.
        /// </summary>
        public bool isFleeing;
    }

    // ========================================================================
    // NEW SNAPSHOT-BASED API (Tasks 8.1-8.4)
    // ========================================================================

    /// <summary>
    /// Plans movement for a unit using ParticipantGameSnapshot and ParticipantAccess.
    /// Scores all valid destinations based on FleetStance and returns a movement command.
    /// 
    /// Task 8.1: Refactored signature to use ParticipantUnitSnapshot and ParticipantAccess
    /// Task 8.2: Queries destinations using ParticipantAccess.TryGetMovementArea
    /// Task 8.3: Implements stance-based destination scoring
    /// Task 8.4: Validates destinations and constructs ParticipantCommand
    /// </summary>
    /// <param name="unit">Unit snapshot to plan movement for</param>
    /// <param name="fleetSnapshot">Fleet snapshot with stance and strategic data</param>
    /// <param name="context">Turn context with visible enemies</param>
    /// <param name="access">ParticipantAccess for validation</param>
    /// <param name="snapshot">Full game snapshot for map data</param>
    /// <returns>ParticipantCommand to commit movement, or null if no valid movement</returns>
    public static GameParticipantAPI.ParticipantCommand PlanMovement(
        ParticipantUnitSnapshot unit,
        FleetSnapshot fleetSnapshot,
        AITurnContext context,
        GameParticipantAPI.ParticipantAccess access,
        ParticipantGameSnapshot snapshot)
    {
        // Performance logging (Requirement 20.3)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Task 8.2: Query valid destinations using ParticipantAccess
        if (!access.TryGetMovementArea(unit.RuntimeUnitID, out Vector2Int[] movementArea, out string errorMessage))
        {
            Debug.LogWarning($"[AI][Movement] Cannot retrieve movement area for unit {unit.InstanceName}: {errorMessage}");
            return null;
        }

        // If no destinations available, return null
        if (movementArea == null || movementArea.Length == 0)
        {
            Debug.Log($"[AI][Movement] No valid movement destinations for unit {unit.InstanceName}");
            return null;
        }

        // Task 8.3: Score all destinations with all facings
        List<ScoredDestination> scoredDestinations = new List<ScoredDestination>();
        
        foreach (Vector2Int destination in movementArea)
        {
            // Try all four facings
            foreach (UnitFacing facing in System.Enum.GetValues(typeof(UnitFacing)))
            {
                float score = ScoreDestination(destination, facing, unit, fleetSnapshot, context, snapshot);
                scoredDestinations.Add(new ScoredDestination
                {
                    Destination = destination,
                    Facing = facing,
                    Score = score
                });
            }
        }

        // Task 8.4: Sort by score descending and validate
        scoredDestinations.Sort((a, b) => b.Score.CompareTo(a.Score));

        foreach (var scored in scoredDestinations)
        {
            // Validate the movement
            if (access.ValidateMovement(unit.RuntimeUnitID, scored.Destination, scored.Facing, out GameMovement.MovementPlan plan, out string validationError))
            {
                // Valid movement found - construct and return command
                var command = GameParticipantAPI.ParticipantCommand.CommitMovement(snapshot, unit.RuntimeUnitID, scored.Destination, scored.Facing);

                // Log performance if threshold exceeded (Requirement 20.3)
                stopwatch.Stop();
                const float thresholdMs = 20f;
                if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
                {
                    Debug.Log($"[AI][Movement][Perf] PlanMovement for {unit.InstanceName} took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (threshold: {thresholdMs}ms)");
                }

                return command;
            }
            else
            {
                // Log validation failure at Info level
                Debug.Log($"[AI][Movement] Validation failed for {unit.InstanceName} to {scored.Destination} facing {scored.Facing}: {validationError}");
            }
        }

        // No valid destinations found
        Debug.Log($"[AI][Movement] No valid movements found for unit {unit.InstanceName} after validation");

        // Log performance if threshold exceeded (Requirement 20.3)
        stopwatch.Stop();
        const float thresholdMs2 = 20f;
        if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs2)
        {
            Debug.Log($"[AI][Movement][Perf] PlanMovement for {unit.InstanceName} took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (no valid movement, threshold: {thresholdMs2}ms)");
        }

        return null;
    }

    /// <summary>
    /// Helper struct for tracking scored destination candidates.
    /// </summary>
    private struct ScoredDestination
    {
        public Vector2Int Destination;
        public UnitFacing Facing;
        public float Score;
    }

    /// <summary>
    /// Scores a destination based on FleetStance and tactical factors.
    /// Task 8.3: Implements stance-based destination scoring.
    /// Uses FleetSnapshot.EnemySnapshots (snapshot-based) or Contacts (legacy) for enemy position data.
    /// </summary>
    /// <param name="destination">Destination coordinate to score</param>
    /// <param name="facing">Facing direction at destination</param>
    /// <param name="unit">Unit being moved</param>
    /// <param name="fleetSnapshot">Fleet snapshot with stance and contacts</param>
    /// <param name="context">Turn context</param>
    /// <param name="snapshot">Game snapshot for fog tile data</param>
    /// <returns>Score for this destination (higher is better)</returns>
    private static float ScoreDestination(
        Vector2Int destination,
        UnitFacing facing,
        ParticipantUnitSnapshot unit,
        FleetSnapshot fleetSnapshot,
        AITurnContext context,
        ParticipantGameSnapshot snapshot)
    {
        float score = 0f;
        Vector2Int currentPos = Vector2Int.zero; // RuntimeUnit N/A
        Vector2Int mapCenter = CalculateMapCenter(snapshot);

        // Get enemy positions (prefer snapshot-based EnemySnapshots, fallback to legacy Contacts)
        bool hasEnemies = (fleetSnapshot.EnemySnapshots != null && fleetSnapshot.EnemySnapshots.Count > 0) ||
                         (fleetSnapshot.Contacts != null && fleetSnapshot.Contacts.Count > 0);

        switch (fleetSnapshot.CurrentStance)
        {
            case FleetStance.Retreat:
                // Retreat stance: move away from enemies toward home
                // Score by distance from HomeCentroid toward enemies (negated = moving away is positive)
                if (hasEnemies)
                {
                    // Find nearest enemy
                    Vector2Int nearestEnemy = GetNearestEnemyPosition(fleetSnapshot, currentPos);

                    // Moving toward home and away from enemy is good
                    int distToHomeFromDest = SpatialCalculations.ManhattanDistance(destination, fleetSnapshot.HomeCentroid);
                    int distToHomeFromCurrent = SpatialCalculations.ManhattanDistance(currentPos, fleetSnapshot.HomeCentroid);
                    int distToEnemyFromDest = SpatialCalculations.ManhattanDistance(destination, nearestEnemy);
                    int distToEnemyFromCurrent = SpatialCalculations.ManhattanDistance(currentPos, nearestEnemy);

                    // Reward moving toward home (negative distance change)
                    score += (distToHomeFromCurrent - distToHomeFromDest) * 2.0f;
                    // Reward moving away from enemy (positive distance change)
                    score += (distToEnemyFromDest - distToEnemyFromCurrent) * 1.5f;
                }
                else
                {
                    // No enemies visible, just move toward home
                    int distToHome = SpatialCalculations.ManhattanDistance(destination, fleetSnapshot.HomeCentroid);
                    score -= distToHome; // Negated: closer to home is better
                }
                break;

            case FleetStance.Advance:
                // Advance stance: move toward nearest contact
                if (hasEnemies)
                {
                    // Find nearest contact from destination
                    int minDist = int.MaxValue;
                    
                    if (fleetSnapshot.EnemySnapshots != null)
                    {
                        foreach (var contact in fleetSnapshot.EnemySnapshots)
                        {
                            int dist = SpatialCalculations.ManhattanDistance(destination, contact.MovementPivotCoordinate);
                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }
                    }
                    else if (fleetSnapshot.Contacts != null)
                    {
                        // Legacy path
                        foreach (var contact in fleetSnapshot.Contacts)
                        {
                            int dist = SpatialCalculations.ManhattanDistance(destination, destination /* RuntimeUnit has no position property */);
                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }
                    }
                    
                    score -= minDist; // Negated: closer to contact is better
                }
                        
    else
    {
        // No enemies visible: add forward momentum toward enemy side
        // Calculate enemy side as opposite direction from home
        Vector2Int enemySide = mapCenter + (mapCenter - fleetSnapshot.HomeCentroid);
        int distToEnemySide = SpatialCalculations.ManhattanDistance(destination, enemySide);
        score -= distToEnemySide * 0.5f; // Moderate bonus for pushing forward
    }
    break;


            case FleetStance.Hunt:
                // Hunt stance: move toward unexplored fog tiles
                int fogTileCount = 0;
                int fogTileDistance = 0;

                // Sample tiles around destination to estimate fog proximity
                for (int dx = -3; dx <= 3; dx++)
                {
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        Vector2Int samplePos = new Vector2Int(destination.x + dx, destination.y + dy);
                        
                        // Find this tile in snapshot
                        foreach (var tile in snapshot.Tiles)
                        {
                            if (tile.Coordinate == samplePos && tile.Visibility == ParticipantTileVisibility.Fog)
                            {
                                fogTileCount++;
                                fogTileDistance += SpatialCalculations.ManhattanDistance(destination, samplePos);
                                break;
                            }
                        }
                    }
                }

                // Reward positions near more fog tiles
                score += fogTileCount * 5.0f;
                // Penalize distance to fog tiles (closer is better)
                if (fogTileCount > 0)
                {
                    score -= (fogTileDistance / (float)fogTileCount) * 2.0f;
                }
                if (fogTileCount > 0)
{
    score -= (fogTileDistance / (float)fogTileCount) * 2.0f;
}

// ADD FORWARD MOMENTUM: Bonus for moving toward map center
int distToCenter = SpatialCalculations.ManhattanDistance(destination, mapCenter);
int currentDistToCenter = SpatialCalculations.ManhattanDistance(fleetSnapshot.FleetCentroid, mapCenter);

// Reward moving closer to center (push forward aggressively)
score += (currentDistToCenter - distToCenter) * 3.0f;

// If already past center, push toward enemy side
if (distToCenter < currentDistToCenter / 2)
{
    Vector2Int enemySide = mapCenter + (mapCenter - fleetSnapshot.HomeCentroid);
    int distToEnemySide = SpatialCalculations.ManhattanDistance(destination, enemySide);
    score -= distToEnemySide * 1.5f;
}
break;


            case FleetStance.Hold:
                // Hold stance: minimize displacement from current position
                int displacement = SpatialCalculations.ManhattanDistance(destination, currentPos);
                score -= displacement * 3.0f; // Negated: staying put is better
                break;

            case FleetStance.DefensiveFallback:
                // DefensiveFallback stance: weighted balance (enemy distance * 0.6 - home distance * 0.4)
                int distToHomeDefensive = SpatialCalculations.ManhattanDistance(destination, fleetSnapshot.HomeCentroid);
                
                if (hasEnemies)
                {
                    // Find nearest enemy distance from destination
                    Vector2Int nearestEnemy = GetNearestEnemyPosition(fleetSnapshot, destination);
                    int minEnemyDist = SpatialCalculations.ManhattanDistance(destination, nearestEnemy);

                    // Balance: want to be away from enemies but not too far from home
                    score += minEnemyDist * 0.6f - distToHomeDefensive * 0.4f;
                }
                else
                {
                    // No enemies, just favor closer to home
                    score -= distToHomeDefensive * 0.4f;
                }
                break;
        }

        return score;
    }

    /// <summary>
    /// Finds the nearest enemy position from a given position.
    /// Supports both snapshot-based and legacy enemy tracking.
    /// </summary>
    private static Vector2Int GetNearestEnemyPosition(FleetSnapshot fleetSnapshot, Vector2Int fromPosition)
    {
        Vector2Int nearestEnemy = Vector2Int.zero;
        int minDist = int.MaxValue;

        // Prefer snapshot-based enemy data
        if (fleetSnapshot.EnemySnapshots != null && fleetSnapshot.EnemySnapshots.Count > 0)
        {
            foreach (var enemy in fleetSnapshot.EnemySnapshots)
            {
                int dist = SpatialCalculations.ManhattanDistance(fromPosition, fromPosition /* RuntimeUnit fallback - position unknown */);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestEnemy = fromPosition /* RuntimeUnit fallback - position unknown */;
                }
            }
        }
        // Fallback to legacy contacts
        else if (fleetSnapshot.Contacts != null && fleetSnapshot.Contacts.Count > 0)
        {
            foreach (var enemy in fleetSnapshot.Contacts)
            {
                int dist = SpatialCalculations.ManhattanDistance(fromPosition, fromPosition /* RuntimeUnit fallback - position unknown */);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestEnemy = fromPosition /* RuntimeUnit fallback - position unknown */;
                }
            }
        }

        return nearestEnemy;
    }

/// <summary>
/// Calculates map center from snapshot tiles.
/// </summary>
private static Vector2Int CalculateMapCenter(ParticipantGameSnapshot snapshot)
{
    if (snapshot.Tiles == null || snapshot.Tiles.Length == 0)
    {
        return Vector2Int.zero;
    }

    int maxX = 0;
    int maxY = 0;
    foreach (var tile in snapshot.Tiles)
    {
        if (tile.Coordinate.x > maxX)
            maxX = tile.Coordinate.x;
        if (tile.Coordinate.y > maxY)
            maxY = tile.Coordinate.y;
    }

    return new Vector2Int(maxX / 2, maxY / 2);
}

}




