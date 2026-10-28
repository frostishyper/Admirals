using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Admirals.AI;

/// <summary>
/// Static utility class for deployment phase zone analysis and initial placement strategy.
/// Analyzes deployment zone geometry, estimates enemy direction, and calculates appropriate
/// unit facing for deployment using ParticipantAccess validation.
/// </summary>
public static class DeploymentPlanner
{
    /// <summary>
    /// Plans the next deployment action using snapshot data and ParticipantAccess validation.
    /// Implements formation-based deployment with role-specific positioning.
    /// </summary>
    /// <param name="access">ParticipantAccess for validation and deployment zone queries.</param>
    /// <param name="snapshot">ParticipantGameSnapshot with deployment state.</param>
    /// <param name="context">Turn context with participant and game state information.</param>
    /// <param name="fleetSnapshot">Fleet snapshot with unit roles and tactical state.</param>
    /// <returns>ParticipantCommand for next deployment action, or null if no valid action available.</returns>
    public static GameParticipantAPI.ParticipantCommand PlanDeployment(
        GameParticipantAPI.ParticipantAccess access,
        ParticipantGameSnapshot snapshot,
        AITurnContext context,
        FleetSnapshot fleetSnapshot)
    {
        // Performance logging (Requirement 20.4)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (access == null || snapshot == null || context == null || fleetSnapshot == null)
        {
            Debug.LogWarning("[AI][Deployment] Null parameter in PlanDeployment");
            return null;
        }

        // Task 10.2: Query deployment zone coordinates
        if (!access.TryGetDeploymentZoneCoordinates(out Vector2Int[] deploymentZoneArray))
        {
            Debug.LogWarning("[AI][Deployment] Failed to retrieve deployment zone coordinates");
            return null;
        }

        if (deploymentZoneArray == null || deploymentZoneArray.Length == 0)
        {
            Debug.LogWarning("[AI][Deployment] Deployment zone is empty");
            return null;
        }

        List<Vector2Int> deploymentZone = new List<Vector2Int>(deploymentZoneArray);

        // Task 10.2: Find first unplaced unit from OwnUnits
        ParticipantUnitSnapshot unplacedUnit = null;
        foreach (var unit in snapshot.OwnUnits)
        {
            if (unit == null || unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            // Check if unit is not yet placed (not on map)
            if (!unit.IsOnMap)
            {
                unplacedUnit = unit;
                break;
            }
        }

        // Task 10.2: If no unplaced units remain, confirm deployment
        if (unplacedUnit == null)
        {
            Debug.Log("[AI][Deployment] All units placed, confirming deployment");

            // Log performance if threshold exceeded (Requirement 20.4)
            stopwatch.Stop();
            const float thresholdMs = 30f;
            if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
            {
                Debug.Log($"[AI][Deployment][Perf] PlanDeployment took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (confirm, threshold: {thresholdMs}ms)");
            }

            return GameParticipantAPI.ParticipantCommand.ConfirmDeployment(snapshot);
        }

        Debug.Log($"[AI][Deployment] Planning placement for unit {unplacedUnit.RuntimeUnitID}");

        // Task 10.3: Calculate deployment zone centroid and enemy direction
        Vector2Int centroid = CalculateDeploymentCentroid(deploymentZone);
        Vector2Int mapCenter = CalculateMapCenter(snapshot);
        Vector2Int enemyDirection = (mapCenter - centroid);
        if (enemyDirection != Vector2Int.zero)
        {
            enemyDirection = NormalizeDirection(enemyDirection);
        }
        else
        {
            enemyDirection = new Vector2Int(1, 0); // Default: East
        }

        UnitFacing facing = CalculateFacingToward(enemyDirection);

        Debug.Log($"[AI][Deployment] Zone centroid: {centroid}, Enemy direction: {enemyDirection}, Facing: {facing}");

        // Task 10.3: Classify unit role and find formation position
        UnitRole role = ClassifyDeploymentRole(unplacedUnit);
        Debug.Log($"[AI][Deployment] Unit {unplacedUnit.RuntimeUnitID} classified as {role}");

        // Get already-placed units for spacing calculations
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        foreach (var unit in snapshot.OwnUnits)
        {
            if (unit != null && unit.IsOnMap)
            {
                occupiedPositions.Add(unit.MovementPivotCoordinate);
            }
        }

        // Task 10.3: Find formation position based on role
        Vector2Int formationPosition = FindFormationPosition(
            role,
            deploymentZone,
            centroid,
            enemyDirection,
            occupiedPositions,
            fleetSnapshot,
            snapshot);

        // Task 10.4: Validate formation position and construct command
        if (access.ValidateDeploymentPlacement(
            unplacedUnit.FleetEntryID,
            formationPosition,
            facing,
            out string validationMessage))
        {
            Debug.Log($"[AI][Deployment] Placing {role} at formation position {formationPosition}");

            // Log performance if threshold exceeded (Requirement 20.4)
            stopwatch.Stop();
            const float thresholdMs = 30f;
            if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
            {
                Debug.Log($"[AI][Deployment][Perf] PlanDeployment took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (threshold: {thresholdMs}ms)");
            }

            return GameParticipantAPI.ParticipantCommand.PlaceDeployment(
                snapshot,
                unplacedUnit.FleetEntryID,
                formationPosition,
                facing);
        }

        Debug.Log($"[AI][Deployment] Formation position invalid: {validationMessage}, trying fallback");

        // Task 10.4: Fallback - try any valid position
        Vector2Int fallbackPosition = FindAnyValidPlacement(
            unplacedUnit.FleetEntryID,
            deploymentZone,
            facing,
            occupiedPositions,
            access);

        if (fallbackPosition != Vector2Int.zero)
        {
            Debug.Log($"[AI][Deployment] Placing at fallback position {fallbackPosition}");

            // Log performance if threshold exceeded (Requirement 20.4)
            stopwatch.Stop();
            const float thresholdMs = 30f;
            if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
            {
                Debug.Log($"[AI][Deployment][Perf] PlanDeployment took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (fallback, threshold: {thresholdMs}ms)");
            }

            return GameParticipantAPI.ParticipantCommand.PlaceDeployment(
                snapshot,
                unplacedUnit.FleetEntryID,
                fallbackPosition,
                facing);
        }

        // Task 10.4: No valid placements found
        Debug.LogWarning($"[AI][Deployment] No valid placement found for unit {unplacedUnit.RuntimeUnitID}");

        // Log performance if threshold exceeded (Requirement 20.4)
        stopwatch.Stop();
        const float thresholdMs2 = 30f;
        if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs2)
        {
            Debug.Log($"[AI][Deployment][Perf] PlanDeployment took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (no valid placement, threshold: {thresholdMs2}ms)");
        }

        return null;
    }

    /// <summary>
    /// Calculates the geometric centroid of the deployment zone.
    /// Computes the mean position of all tiles in the zone.
    /// </summary>
    /// <param name="deploymentZone">List of all tiles in the deployment zone.</param>
    /// <returns>The mean position (centroid) of all deployment zone tiles.</returns>
    /// <remarks>
    /// Requirement 11.2: Calculate deployment zone centroid as mean position of all deployment tiles.
    /// </remarks>
    public static Vector2Int CalculateDeploymentCentroid(List<Vector2Int> deploymentZone)
    {
        if (deploymentZone == null || deploymentZone.Count == 0)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Empty deployment zone, returning zero");
            return Vector2Int.zero;
        }

        int sumX = 0;
        int sumY = 0;

        foreach (Vector2Int tile in deploymentZone)
        {
            sumX += tile.x;
            sumY += tile.y;
        }

        int avgX = sumX / deploymentZone.Count;
        int avgY = sumY / deploymentZone.Count;

        return new Vector2Int(avgX, avgY);
    }

    /// <summary>
    /// Task 10.3: Classifies unit deployment role using FleetCommander logic.
    /// </summary>
    private static UnitRole ClassifyDeploymentRole(ParticipantUnitSnapshot unit)
    {
        if (unit == null || unit.Definition == null)
            return UnitRole.Striker;

        Definition_Unit def = unit.Definition;

        // Extract capabilities - using same thresholds as FleetCommander
        int maxSpeed = def.MovementPattern?.Layers?[0]?.Strength ?? 0;
        int maxRange = 5; // Default range for role classification
        // Note: Weapon range not available in Definition_Weapon, using default
        int maxHealth = def.Health;
        int weaponCount = unit.Weapons != null ? unit.Weapons.Length : 0;

        // FleetCommander thresholds
        const int ScoutMinSpeed = 4;
        const int ScoutMinRange = 4;
        const int AnchorMinHealth = 25;
        const int AnchorMinWeapons = 2;

        // Scout: Fast + Long Range
        if (maxSpeed >= ScoutMinSpeed && maxRange >= ScoutMinRange)
        {
            return UnitRole.Scout;
        }

        // Anchor: Durable + Multi-weapon
        if (maxHealth >= AnchorMinHealth && weaponCount >= AnchorMinWeapons)
        {
            return UnitRole.Anchor;
        }

        // Default: Striker
        return UnitRole.Striker;
    }

    /// <summary>
    /// Task 10.3: Finds formation position based on unit role.
    /// Anchors go to center, Scouts to flanks, Strikers between.
    /// </summary>
    private static Vector2Int FindFormationPosition(
        UnitRole role,
        List<Vector2Int> deploymentZone,
        Vector2Int centroid,
        Vector2Int enemyDirection,
        HashSet<Vector2Int> occupiedPositions,
        FleetSnapshot fleetSnapshot,
        ParticipantGameSnapshot snapshot)
    {
        // Calculate perpendicular axis for flank positioning
        Vector2Int perpendicularAxis = new Vector2Int(enemyDirection.y, -enemyDirection.x);

        Vector2Int targetPosition;

        switch (role)
        {
            case UnitRole.Anchor:
                // Task 10.3: Position Anchor units in center of zone
                targetPosition = centroid;
                break;

            case UnitRole.Scout:
                // Task 10.3: Position Scout units on flanks
                // Alternate between left and right flanks based on how many scouts already placed
                int scoutCount = 0;
                foreach (var unit in snapshot.OwnUnits)
                {
                    if (unit.IsOnMap)
                    {
                        UnitRole placedRole = ClassifyDeploymentRole(unit);
                        if (placedRole == UnitRole.Scout)
                            scoutCount++;
                    }
                }

                // Odd scouts go left, even scouts go right
                int flankOffset = 3; // Distance from center
                if (scoutCount % 2 == 0)
                {
                    targetPosition = centroid - perpendicularAxis * flankOffset; // Left flank
                }
                else
                {
                    targetPosition = centroid + perpendicularAxis * flankOffset; // Right flank
                }
                break;

            case UnitRole.Striker:
            default:
                // Task 10.3: Position Striker units between Anchors and Scouts
                // Place slightly off-center
                int strikerOffset = 2;
                targetPosition = centroid + perpendicularAxis * strikerOffset;
                break;
        }

        // Task 10.3: Find nearest unoccupied position with minimum spacing of 2
        return FindNearestUnoccupiedPosition(targetPosition, deploymentZone, occupiedPositions, 2);
    }

    /// <summary>
    /// Task 10.3: Finds nearest unoccupied position to target with minimum spacing.
    /// </summary>
    private static Vector2Int FindNearestUnoccupiedPosition(
        Vector2Int target,
        List<Vector2Int> validPositions,
        HashSet<Vector2Int> occupiedPositions,
        int minSpacing)
    {
        // Sort positions by distance from target
        var sortedPositions = validPositions
            .OrderBy(pos => SpatialCalculations.ManhattanDistance(pos, target))
            .ToList();

        foreach (var pos in sortedPositions)
        {
            if (occupiedPositions.Contains(pos))
                continue;

            // Check spacing constraint
            bool spacingViolated = false;
            foreach (var occupied in occupiedPositions)
            {
                if (SpatialCalculations.ManhattanDistance(pos, occupied) < minSpacing)
                {
                    spacingViolated = true;
                    break;
                }
            }

            if (!spacingViolated)
                return pos;
        }

        // Fallback: return nearest position even if spacing violated
        return sortedPositions.FirstOrDefault();
    }

    /// <summary>
    /// Task 10.4: Fallback method to find any valid placement position.
    /// </summary>
    private static Vector2Int FindAnyValidPlacement(
        string fleetEntryID,
        List<Vector2Int> deploymentZone,
        UnitFacing facing,
        HashSet<Vector2Int> occupiedPositions,
        GameParticipantAPI.ParticipantAccess access)
    {
        // Try all positions in deployment zone, sorted by distance from center
        Vector2Int centroid = CalculateDeploymentCentroid(deploymentZone);
        var sortedPositions = deploymentZone
            .OrderBy(pos => SpatialCalculations.ManhattanDistance(pos, centroid))
            .ToList();

        foreach (var pos in sortedPositions)
        {
            if (occupiedPositions.Contains(pos))
                continue;

            if (access.ValidateDeploymentPlacement(fleetEntryID, pos, facing, out string validationMessage))
            {
                return pos;
            }
            else
            {
                Debug.Log($"[AI][Deployment] Validation failed at {pos}: {validationMessage}");
            }
        }

        return Vector2Int.zero; // No valid position found
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

    /// <summary>
    /// Converts a direction vector to a UnitFacing enum value.
    /// Selects the cardinal direction (North, East, South, West) that best aligns
    /// with the given direction vector using dot product comparison.
    /// </summary>
    /// <param name="direction">Direction vector (should be normalized or at least non-zero).</param>
    /// <returns>The UnitFacing enum value that best matches the direction vector.</returns>
    /// <remarks>
    /// Requirement 11.6: Calculate facing direction toward estimated enemy direction for all units.
    /// 
    /// Uses dot product to find the cardinal direction with maximum alignment:
    /// - North: (0, 1)
    /// - East: (1, 0)
    /// - South: (0, -1)
    /// - West: (-1, 0)
    /// </remarks>
    public static UnitFacing CalculateFacingToward(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Zero direction vector, defaulting to North");
            return UnitFacing.North;
        }

        // Define cardinal direction vectors
        Vector2Int north = new Vector2Int(0, 1);
        Vector2Int east = new Vector2Int(1, 0);
        Vector2Int south = new Vector2Int(0, -1);
        Vector2Int west = new Vector2Int(-1, 0);

        // Calculate dot products for alignment measurement
        float dotNorth = (direction.x * north.x + direction.y * north.y);
        float dotEast = (direction.x * east.x + direction.y * east.y);
        float dotSouth = (direction.x * south.x + direction.y * south.y);
        float dotWest = (direction.x * west.x + direction.y * west.y);

        // Find the maximum dot product (best alignment)
        float maxDot = Mathf.Max(dotNorth, Mathf.Max(dotEast, Mathf.Max(dotSouth, dotWest)));

        // Return the facing with maximum alignment
        if (maxDot == dotNorth)
        {
            return UnitFacing.North;
        }
        else if (maxDot == dotEast)
        {
            return UnitFacing.East;
        }
        else if (maxDot == dotSouth)
        {
            return UnitFacing.South;
        }
        else // maxDot == dotWest
        {
            return UnitFacing.West;
        }
    }

    /// <summary>
    /// Normalizes a direction vector to approximate unit length with integer components.
    /// Returns the input vector if it is already a unit vector, otherwise scales it
    /// to have components in the range [-1, 0, 1].
    /// </summary>
    /// <param name="vector">The direction vector to normalize.</param>
    /// <returns>Normalized direction vector with integer components.</returns>
    private static Vector2Int NormalizeDirection(Vector2Int vector)
    {
        // Handle zero vector
        if (vector.x == 0 && vector.y == 0)
        {
            return new Vector2Int(1, 0); // Default: East
        }

        // Calculate magnitude
        float magnitude = Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y);

        // Avoid division by zero
        if (magnitude < 0.001f)
        {
            return new Vector2Int(1, 0); // Default: East
        }

        // Normalize and round to integer components
        int normalizedX = Mathf.RoundToInt(vector.x / magnitude);
        int normalizedY = Mathf.RoundToInt(vector.y / magnitude);

        return new Vector2Int(normalizedX, normalizedY);
    }
}

