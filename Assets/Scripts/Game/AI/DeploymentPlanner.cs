using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Static utility class for deployment phase zone analysis and initial placement strategy.
/// Analyzes deployment zone geometry, estimates enemy direction, and calculates appropriate
/// unit facing for deployment.
/// </summary>
public static class DeploymentPlanner
{
    /// <summary>
    /// Sector classification for deployment zone tiles.
    /// Used to distribute units across the deployment zone for balanced initial positioning.
    /// </summary>
    public enum Sector
    {
        Left,
        Center,
        Right
    }
    /// <summary>
    /// Main deployment orchestration method.
    /// Analyzes the deployment zone, estimates enemy direction, calculates unit facing,
    /// and coordinates placement of all units.
    /// </summary>
    /// <param name="context">Turn context with participant and game state information.</param>
    /// <param name="snapshot">Fleet snapshot with unit roles and tactical state.</param>
    /// <param name="gameDeployment">GameDeployment subsystem for validation and commit operations.</param>
    /// <param name="gridManager">Grid manager for spatial calculations and map bounds.</param>
    /// <returns>True if deployment completed successfully; false on failure.</returns>
    public static bool ExecuteDeployment(
        AITurnContext context,
        FleetSnapshot snapshot,
        GameDeployment gameDeployment,
        GridManager gridManager)
    {
        if (context == null || snapshot == null || gameDeployment == null || gridManager == null)
        {
            Debug.LogError("[AI][DeploymentPlanner] Null parameter in ExecuteDeployment");
            return false;
        }

        // Query deployment zone from GameDeployment
        if (!gameDeployment.TryGetView(context.OwnParticipant, out GameDeployment.ParticipantView deploymentView))
        {
            Debug.LogError("[AI][DeploymentPlanner] Failed to get deployment view for participant");
            return false;
        }

        List<Vector2Int> deploymentZone = deploymentView.DeploymentZone;
        if (deploymentZone == null || deploymentZone.Count == 0)
        {
            Debug.LogError("[AI][DeploymentPlanner] Deployment zone is empty");
            return false;
        }

        // Calculate deployment zone centroid (Requirement 11.2)
        Vector2Int centroid = CalculateDeploymentCentroid(deploymentZone);

        // Estimate enemy direction (Requirement 11.5)
        Vector2Int enemyDirection = EstimateEnemyDirection(centroid, gridManager);

        // Calculate facing toward enemy (Requirement 11.6)
        UnitFacing facing = CalculateFacingToward(enemyDirection);

        Debug.Log($"[AI][DeploymentPlanner] Zone centroid: {centroid}, Enemy direction: {enemyDirection}, Facing: {facing}");

        // Divide deployment zone into sectors (Requirement 11.3, 11.4)
        Dictionary<Sector, List<Vector2Int>> sectorTiles = DivideIntoSectors(deploymentZone, centroid, enemyDirection);

        // Assign units to sectors based on roles (Requirement 12.1-12.3)
        Dictionary<RuntimeUnit, Sector> unitSectorAssignments = AssignUnitsToSectors(context.OwnAliveUnits, snapshot, sectorTiles);

        Debug.Log($"[AI][DeploymentPlanner] Divided zone into {sectorTiles.Count} sectors, assigned {unitSectorAssignments.Count} units");

        // Place units with spacing enforcement
        int minSpacing = 2; // Minimum 2 tiles between units
        HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();
        int successfulPlacements = 0;
        int failedPlacements = 0;

        foreach (var kvp in unitSectorAssignments)
        {
            RuntimeUnit unit = kvp.Key;
            Sector sector = kvp.Value;

            if (!sectorTiles.ContainsKey(sector) || sectorTiles[sector].Count == 0)
            {
                Debug.LogWarning($"[AI][DeploymentPlanner] No tiles available in {sector} sector for unit {unit.RuntimeID}");
                failedPlacements++;
                continue;
            }

            bool placed = TryPlaceUnit(unit, sectorTiles[sector], facing, occupiedTiles, minSpacing, gameDeployment, context.OwnParticipant);
            if (placed)
            {
                successfulPlacements++;
            }
            else
            {
                failedPlacements++;
            }
        }

        Debug.Log($"[AI][DeploymentPlanner] Placement complete: {successfulPlacements} successful, {failedPlacements} failed");

        return failedPlacements == 0;
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
    /// Estimates the direction toward the enemy by finding the opposite edge of the map
    /// from the deployment zone centroid.
    /// </summary>
    /// <param name="deploymentCentroid">The centroid of the AI's deployment zone.</param>
    /// <param name="gridManager">Grid manager providing map dimensions.</param>
    /// <returns>Normalized direction vector pointing toward the estimated enemy location.</returns>
    /// <remarks>
    /// Requirement 11.5: Estimate enemy direction by finding opposite edge of map from deployment zone centroid.
    /// 
    /// Algorithm:
    /// 1. Determine which edge is closest to deployment centroid
    /// 2. Select the opposite edge as the likely enemy location
    /// 3. Calculate direction vector from centroid to opposite edge center
    /// 4. Normalize to unit vector
    /// </remarks>
    public static Vector2Int EstimateEnemyDirection(Vector2Int deploymentCentroid, GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] GridManager is null, returning default direction");
            return new Vector2Int(1, 0); // Default: East
        }

        int mapWidth = gridManager.width;
        int mapHeight = gridManager.height;

        // Calculate distances to each edge
        int distToNorth = mapHeight - deploymentCentroid.y;
        int distToSouth = deploymentCentroid.y;
        int distToEast = mapWidth - deploymentCentroid.x;
        int distToWest = deploymentCentroid.x;

        // Find closest edge
        int minDist = Mathf.Min(distToNorth, Mathf.Min(distToSouth, Mathf.Min(distToEast, distToWest)));

        // Determine opposite edge center point
        Vector2Int oppositeEdgeCenter;

        if (minDist == distToNorth)
        {
            // Closest to North, enemy likely at South
            oppositeEdgeCenter = new Vector2Int(mapWidth / 2, 0);
        }
        else if (minDist == distToSouth)
        {
            // Closest to South, enemy likely at North
            oppositeEdgeCenter = new Vector2Int(mapWidth / 2, mapHeight - 1);
        }
        else if (minDist == distToEast)
        {
            // Closest to East, enemy likely at West
            oppositeEdgeCenter = new Vector2Int(0, mapHeight / 2);
        }
        else // minDist == distToWest
        {
            // Closest to West, enemy likely at East
            oppositeEdgeCenter = new Vector2Int(mapWidth - 1, mapHeight / 2);
        }

        // Calculate direction vector from centroid to opposite edge
        Vector2Int direction = oppositeEdgeCenter - deploymentCentroid;

        // Normalize to unit direction
        return NormalizeDirection(direction);
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
        float dotNorth = Vector2Int.Dot(direction, north);
        float dotEast = Vector2Int.Dot(direction, east);
        float dotSouth = Vector2Int.Dot(direction, south);
        float dotWest = Vector2Int.Dot(direction, west);

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

    /// <summary>
    /// Divides the deployment zone into three sectors (Left, Center, Right) based on
    /// perpendicular axis to enemy direction. This creates a tactical spread of units
    /// across the deployment zone for balanced initial positioning.
    /// </summary>
    /// <param name="deploymentZone">List of all tiles in the deployment zone.</param>
    /// <param name="centroid">The geometric center of the deployment zone.</param>
    /// <param name="enemyDirection">Normalized direction vector pointing toward estimated enemy location.</param>
    /// <returns>Dictionary mapping each sector to its list of tiles.</returns>
    /// <remarks>
    /// Requirement 11.3: Divide deployment zone into 3 sectors based on perpendicular axis to enemy direction.
    /// Requirement 11.4: Assign each deployment tile to one of the 3 sectors.
    /// 
    /// Algorithm:
    /// 1. Calculate perpendicular axis to enemy direction (rotated 90 degrees)
    /// 2. For each tile, compute offset along perpendicular axis from centroid
    /// 3. Classify tiles based on offset:
    ///    - Negative offset (left of center) -> Left sector
    ///    - Positive offset (right of center) -> Right sector
    ///    - Near zero offset -> Center sector
    /// 4. Use threshold of 0.3 * zone width to determine center boundaries
    /// </remarks>
    public static Dictionary<Sector, List<Vector2Int>> DivideIntoSectors(
        List<Vector2Int> deploymentZone,
        Vector2Int centroid,
        Vector2Int enemyDirection)
    {
        var sectors = new Dictionary<Sector, List<Vector2Int>>
        {
            { Sector.Left, new List<Vector2Int>() },
            { Sector.Center, new List<Vector2Int>() },
            { Sector.Right, new List<Vector2Int>() }
        };

        if (deploymentZone == null || deploymentZone.Count == 0)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Empty deployment zone in DivideIntoSectors");
            return sectors;
        }

        // Calculate perpendicular axis (rotate enemy direction by 90 degrees)
        // If enemy is North (0, 1), perpendicular is East (1, 0)
        // If enemy is East (1, 0), perpendicular is South (0, -1)
        Vector2Int perpendicularAxis = new Vector2Int(enemyDirection.y, -enemyDirection.x);

        // Calculate zone bounds for threshold determination
        int minOffset = int.MaxValue;
        int maxOffset = int.MinValue;

        foreach (Vector2Int tile in deploymentZone)
        {
            Vector2Int offset = tile - centroid;
            int projectedOffset = Vector2Int.Dot(offset, perpendicularAxis);
            minOffset = Mathf.Min(minOffset, projectedOffset);
            maxOffset = Mathf.Max(maxOffset, projectedOffset);
        }

        // Calculate threshold for center sector (30% of zone width on each side of centroid)
        int zoneWidth = maxOffset - minOffset;
        int centerThreshold = Mathf.Max(1, Mathf.RoundToInt(zoneWidth * 0.3f));

        // Classify each tile into a sector
        foreach (Vector2Int tile in deploymentZone)
        {
            Vector2Int offset = tile - centroid;
            int projectedOffset = Vector2Int.Dot(offset, perpendicularAxis);

            if (projectedOffset < -centerThreshold)
            {
                sectors[Sector.Left].Add(tile);
            }
            else if (projectedOffset > centerThreshold)
            {
                sectors[Sector.Right].Add(tile);
            }
            else
            {
                sectors[Sector.Center].Add(tile);
            }
        }

        Debug.Log($"[AI][DeploymentPlanner] Sectors: Left={sectors[Sector.Left].Count}, Center={sectors[Sector.Center].Count}, Right={sectors[Sector.Right].Count}");

        return sectors;
    }

    /// <summary>
    /// Assigns each unit to a deployment sector based on its tactical role.
    /// Anchors go to center, Scouts spread to left/right flanks, Strikers fill gaps.
    /// </summary>
    /// <param name="units">List of units to assign to sectors.</param>
    /// <param name="snapshot">Fleet snapshot containing unit role classifications.</param>
    /// <param name="sectorTiles">Dictionary of sector tiles (used to verify sectors exist).</param>
    /// <returns>Dictionary mapping each unit to its assigned sector.</returns>
    /// <remarks>
    /// Requirement 12.1: Assign Anchor units to center sector.
    /// Requirement 12.2: Assign Scout units evenly between left and right sectors.
    /// Requirement 12.3: Assign Striker units to fill gaps in sector distribution.
    /// 
    /// Algorithm:
    /// 1. Assign all Anchors to Center sector
    /// 2. Assign Scouts alternating between Left and Right (for even distribution)
    /// 3. Assign Strikers to the least populated sector (for balance)
    /// </remarks>
    public static Dictionary<RuntimeUnit, Sector> AssignUnitsToSectors(
        List<RuntimeUnit> units,
        FleetSnapshot snapshot,
        Dictionary<Sector, List<Vector2Int>> sectorTiles)
    {
        var assignments = new Dictionary<RuntimeUnit, Sector>();

        if (units == null || units.Count == 0)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Empty unit list in AssignUnitsToSectors");
            return assignments;
        }

        if (snapshot == null || snapshot.Members == null)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Null snapshot or members in AssignUnitsToSectors");
            return assignments;
        }

        // Build a lookup map from RuntimeUnit to FleetMember for role classification
        var unitToMember = new Dictionary<RuntimeUnit, FleetMember>();
        foreach (var member in snapshot.Members)
        {
            if (member.Unit != null)
            {
                unitToMember[member.Unit] = member;
            }
        }

        // Track sector population for balancing
        var sectorPopulation = new Dictionary<Sector, int>
        {
            { Sector.Left, 0 },
            { Sector.Center, 0 },
            { Sector.Right, 0 }
        };

        // Scout counter for alternating left/right assignment
        int scoutCounter = 0;

        // Process units by role priority: Anchors first, then Scouts, then Strikers
        var orderedUnits = units.OrderBy(u =>
        {
            if (unitToMember.TryGetValue(u, out var member))
            {
                return member.Role switch
                {
                    UnitRole.Anchor => 0,
                    UnitRole.Scout => 1,
                    UnitRole.Striker => 2,
                    _ => 3
                };
            }
            return 3;
        }).ToList();

        foreach (var unit in orderedUnits)
        {
            if (!unitToMember.TryGetValue(unit, out var member))
            {
                Debug.LogWarning($"[AI][DeploymentPlanner] Unit {unit.RuntimeID} not found in snapshot, skipping");
                continue;
            }

            Sector assignedSector;

            switch (member.Role)
            {
                case UnitRole.Anchor:
                    // Requirement 12.1: Anchors go to center
                    assignedSector = Sector.Center;
                    break;

                case UnitRole.Scout:
                    // Requirement 12.2: Scouts alternate between left and right
                    assignedSector = (scoutCounter % 2 == 0) ? Sector.Left : Sector.Right;
                    scoutCounter++;
                    break;

                case UnitRole.Striker:
                default:
                    // Requirement 12.3: Strikers fill the least populated sector
                    assignedSector = GetLeastPopulatedSector(sectorPopulation, sectorTiles);
                    break;
            }

            assignments[unit] = assignedSector;
            sectorPopulation[assignedSector]++;

            Debug.Log($"[AI][DeploymentPlanner] Assigned {member.Role} unit {unit.RuntimeID} to {assignedSector} sector");
        }

        return assignments;
    }

    /// <summary>
    /// Finds the sector with the fewest assigned units, preferring sectors with available tiles.
    /// Used for balanced Striker placement.
    /// </summary>
    /// <param name="sectorPopulation">Current population count for each sector.</param>
    /// <param name="sectorTiles">Dictionary of available tiles per sector.</param>
    /// <returns>The sector with the lowest population that has available tiles.</returns>
    private static Sector GetLeastPopulatedSector(
        Dictionary<Sector, int> sectorPopulation,
        Dictionary<Sector, List<Vector2Int>> sectorTiles)
    {
        // Filter sectors that have available tiles
        var availableSectors = sectorPopulation
            .Where(kvp => sectorTiles.ContainsKey(kvp.Key) && sectorTiles[kvp.Key].Count > 0)
            .OrderBy(kvp => kvp.Value)
            .ToList();

        if (availableSectors.Count == 0)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] No sectors with available tiles, defaulting to Center");
            return Sector.Center;
        }

        return availableSectors.First().Key;
    }

    /// <summary>
    /// Attempts to place a unit on a tile in its assigned sector with spacing enforcement.
    /// Tries tiles in order of distance from sector centroid, respecting minimum spacing
    /// from already-placed units. Falls back to relaxed spacing if no valid tile found.
    /// Implements Requirements 12.4-12.10.
    /// </summary>
    /// <param name="unit">Unit to place.</param>
    /// <param name="sectorTiles">Available tiles in the unit's assigned sector.</param>
    /// <param name="facing">Facing direction for the unit.</param>
    /// <param name="occupiedTiles">Set of tiles already occupied by placed units (updated on success).</param>
    /// <param name="minSpacing">Minimum spacing in tiles between units.</param>
    /// <param name="gameDeployment">GameDeployment subsystem for validation and commit.</param>
    /// <param name="participant">Participant placing the unit.</param>
    /// <returns>True if placement succeeded; false if no valid tile found.</returns>
    private static bool TryPlaceUnit(
        RuntimeUnit unit,
        List<Vector2Int> sectorTiles,
        UnitFacing facing,
        HashSet<Vector2Int> occupiedTiles,
        int minSpacing,
        GameDeployment gameDeployment,
        RuntimeParticipant participant)
    {
        if (unit == null || sectorTiles == null || sectorTiles.Count == 0)
        {
            Debug.LogWarning("[AI][DeploymentPlanner] Invalid parameters in TryPlaceUnit");
            return false;
        }

        // Calculate sector centroid for tile ordering
        Vector2Int sectorCentroid = CalculateDeploymentCentroid(sectorTiles);

        // Sort tiles by distance from sector centroid (prefer central placement)
        List<Vector2Int> sortedTiles = new List<Vector2Int>(sectorTiles);
        sortedTiles.Sort((a, b) =>
        {
            float distA = Vector2Int.Distance(a, sectorCentroid);
            float distB = Vector2Int.Distance(b, sectorCentroid);
            return distA.CompareTo(distB);
        });

        // Try with full spacing first
        foreach (Vector2Int tile in sortedTiles)
        {
            if (occupiedTiles.Contains(tile))
            {
                continue; // Tile already occupied
            }

            // Check spacing constraint
            bool spacingViolated = false;
            foreach (Vector2Int occupiedTile in occupiedTiles)
            {
                if (Vector2Int.Distance(tile, occupiedTile) < minSpacing)
                {
                    spacingViolated = true;
                    break;
                }
            }

            if (spacingViolated)
            {
                continue;
            }

            // Validate and commit placement
            if (TryCommitPlacement(unit, tile, facing, gameDeployment, participant))
            {
                occupiedTiles.Add(tile);
                Debug.Log($"[AI][DeploymentPlanner] Placed {unit.RuntimeID} at {tile} with spacing {minSpacing}");
                return true;
            }
        }

        // Fallback: relax spacing and try again
        Debug.LogWarning($"[AI][DeploymentPlanner] No tile found for {unit.RuntimeID} with spacing {minSpacing}, relaxing constraints");

        foreach (Vector2Int tile in sortedTiles)
        {
            if (occupiedTiles.Contains(tile))
            {
                continue;
            }

            if (TryCommitPlacement(unit, tile, facing, gameDeployment, participant))
            {
                occupiedTiles.Add(tile);
                Debug.Log($"[AI][DeploymentPlanner] Placed {unit.RuntimeID} at {tile} with relaxed spacing");
                return true;
            }
        }

        Debug.LogError($"[AI][DeploymentPlanner] Failed to place {unit.RuntimeID} in sector");
        return false;
    }

    /// <summary>
    /// Validates and commits a single unit deployment to a tile.
    /// </summary>
    private static bool TryCommitPlacement(
        RuntimeUnit unit,
        Vector2Int tile,
        UnitFacing facing,
        GameDeployment gameDeployment,
        RuntimeParticipant participant)
    {
        // Try submit placement using GameDeployment internal API
        bool success = gameDeployment.TrySubmitPlacement(
            participant,
            unit.RuntimeID,
            tile,
            facing,
            out string validationMessage
        );

        if (!success)
        {
            Debug.LogWarning($"[AI][DeploymentPlanner] Placement failed for {unit.RuntimeID}: {validationMessage}");
        }

        return success;
    }}
