using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Admirals.AI;
/// <summary>
/// AI search phase planner that selects which unit should scan and in which direction.
/// Refactored to use ParticipantGameSnapshot and ParticipantAccess (Tasks 11.1-11.4).
/// </summary>
public static class SearchPlanner
{
    // Track previously searched coordinates to avoid redundant scans
    private static HashSet<Vector2Int> searchHistory = new HashSet<Vector2Int>();
    /// <summary>
    /// Plans search action using ParticipantGameSnapshot and ParticipantAccess.
    /// Task 11.1: Refactored signature to use snapshot and ParticipantAccess
    /// Task 11.2: Queries searchable units and patterns using ParticipantAccess
    /// Task 11.3: Scores patterns based on fog coverage and contact overlap
    /// Task 11.4: Validates searches and constructs ParticipantCommand
    /// </summary>
    public static GameParticipantAPI.ParticipantCommand PlanSearch(
        GameParticipantAPI.ParticipantAccess access,
        ParticipantGameSnapshot snapshot,
        FleetSnapshot fleetSnapshot)
    {
        if (access == null || snapshot == null || fleetSnapshot == null)
        {
            Debug.LogWarning("[AI][Search] Null parameter in PlanSearch");
            return null;
        }
        // Task 11.2: Check if we have searchable units
        if (snapshot.Search == null || snapshot.Search.Units == null || snapshot.Search.Units.Length == 0)
        {
            Debug.Log("[AI][Search] No searchable units in snapshot");
            return GameParticipantAPI.ParticipantCommand.EndSearch(snapshot);
        }
        // Task 11.3: Score all possible searches
        List<ScoredSearch> scoredSearches = new List<ScoredSearch>();
        // Iterate through search view units (GameSearch.UnitView)
        foreach (var searchUnit in snapshot.Search.Units)
        {
            if (searchUnit == null || searchUnit.HasSearched)
                continue;
            // Find corresponding unit snapshot from OwnUnits
            ParticipantUnitSnapshot unitSnap = snapshot.OwnUnits.FirstOrDefault(u => u.RuntimeUnitID == searchUnit.RuntimeUnitID);
            if (unitSnap == null || unitSnap.Systems == null)
                continue;
            // Check each system slot for sensors
            for (int sensorIndex = 0; sensorIndex < unitSnap.Systems.Length; sensorIndex++)
            {
                ParticipantSystemSnapshot system = unitSnap.Systems[sensorIndex];
                if (system == null || system.SlotType != SystemSlotType.Sensor)
                    continue;
                // Task 11.2: Query search pattern for this sensor
                if (!access.TryGetSearchPattern(searchUnit.RuntimeUnitID, sensorIndex, UnitFacing.North, out GameSearch.SearchTile[] searchTiles, out string errorMessage))
                {
                    Debug.Log($"[AI][Search] Cannot get pattern for unit {searchUnit.RuntimeUnitID} sensor {sensorIndex}: {errorMessage}");
                    continue;
                }
                // Convert SearchTile[] to Vector2Int[] for scoring
                Vector2Int[] pattern = searchTiles?.Select(t => t.Coordinate).ToArray();
                if (pattern == null || pattern.Length == 0)
                    continue;
                // Score this search for each facing
                foreach (UnitFacing facing in System.Enum.GetValues(typeof(UnitFacing)))
                {
                    float score = ScoreSearchPattern(pattern, facing, unitSnap, fleetSnapshot, snapshot);
                    scoredSearches.Add(new ScoredSearch
                    {
                        UnitID = searchUnit.RuntimeUnitID,
                        SensorIndex = sensorIndex,
                        Facing = facing,
                        Score = score
                    });
                }
            }
        }
        if (scoredSearches.Count == 0)
        {
            Debug.Log("[AI][Search] No valid search patterns found");
            return GameParticipantAPI.ParticipantCommand.EndSearch(snapshot);
        }
        // Task 11.4: Sort by score descending and validate
        scoredSearches.Sort((a, b) => b.Score.CompareTo(a.Score));
        foreach (var search in scoredSearches)
        {
            if (access.TryCommitSearch(
                search.UnitID,
                search.SensorIndex,
                search.Facing,
                out var commitResult, out string validationError))
            {
                // Valid search found - construct and return command
                Debug.Log($"[AI][Search] Committing search: unit={search.UnitID} sensor={search.SensorIndex} facing={search.Facing} score={search.Score:F1}");
                return GameParticipantAPI.ParticipantCommand.CommitSearch(snapshot, search.UnitID, search.SensorIndex,
                    search.Facing);
            }
            else
            {
                // Task 11.4: Log validation failure at Info level
                Debug.Log($"[AI][Search] Validation failed for unit {search.UnitID}: {validationError}");
            }
        }
        // Task 11.4: No valid searches found - end search
        Debug.Log("[AI][Search] No valid searches after validation, ending search phase");
        return GameParticipantAPI.ParticipantCommand.EndSearch(snapshot);
    }
    /// <summary>
    /// Helper struct for tracking scored search candidates.
    /// </summary>
    private struct ScoredSearch
    {
        public string UnitID;
        public int SensorIndex;
        public UnitFacing Facing;
        public float Score;
    }
    /// <summary>
    /// Task 11.3: Scores a search pattern based on fog coverage and contact overlap.
    /// </summary>
    private static float ScoreSearchPattern(
        Vector2Int[] pattern,
        UnitFacing facing,
        ParticipantUnitSnapshot unitSnapshot,
        FleetSnapshot fleetSnapshot,
        ParticipantGameSnapshot snapshot)
    {
        float score = 0f;
        // Task 11.3: Rotate pattern based on facing
        Vector2Int[] rotatedPattern = RotatePattern(pattern, facing, unitSnapshot.MovementPivotCoordinate);
        // Task 11.3: Count fog tiles covered by pattern
        int fogTilesCovered = 0;
        foreach (var coord in rotatedPattern)
        {
            // Check if coordinate is in snapshot tiles and is fog
            foreach (var tile in snapshot.Tiles)
            {
                if (tile.Coordinate == coord && tile.Visibility == ParticipantTileVisibility.Fog)
                {
                    fogTilesCovered++;
                    break;
                }
            }
        }
        // Task 11.3: Count contact overlap
        int contactOverlap = 0;
        bool hasContacts = (fleetSnapshot.EnemySnapshots != null && fleetSnapshot.EnemySnapshots.Count > 0) ||
                          (fleetSnapshot.Contacts != null && fleetSnapshot.Contacts.Count > 0);
        if (hasContacts)
        {
            HashSet<Vector2Int> patternSet = new HashSet<Vector2Int>(rotatedPattern);
            // Check snapshot-based contacts
            if (fleetSnapshot.EnemySnapshots != null)
            {
                foreach (var contact in fleetSnapshot.EnemySnapshots)
                {
                    if (contact != null && patternSet.Contains(contact.MovementPivotCoordinate))
                    {
                        contactOverlap++;
                    }
                }
            }
        }
        // Task 11.3: Apply weights based on whether contacts are visible
        if (hasContacts && (fleetSnapshot.EnemySnapshots?.Count > 0 || fleetSnapshot.Contacts?.Count > 0))
        {
            // Prioritize contact overlap (weight 50) over fog coverage (weight 10)
            score += contactOverlap * 50.0f;
            score += fogTilesCovered * 10.0f;
        }
        else
        {
            // No contacts visible - prioritize fog coverage (weight 30)
            score += fogTilesCovered * 30.0f;
        }
        // Task 11.3: Penalize previously searched coordinates (weight -5 per coord)
        int previouslySearched = 0;
        foreach (var coord in rotatedPattern)
        {
            if (searchHistory.Contains(coord))
            {
                previouslySearched++;
            }
        }
        score += previouslySearched * -5.0f;
        // Update search history
        foreach (var coord in rotatedPattern)
        {
            searchHistory.Add(coord);
        }
        return score;
    }
    /// <summary>
    /// Task 11.3: Rotates pattern coordinates based on facing direction.
    /// Patterns are defined relative to North facing by default.
    /// </summary>
    private static Vector2Int[] RotatePattern(Vector2Int[] pattern, UnitFacing facing, Vector2Int unitPosition)
    {
        Vector2Int[] rotated = new Vector2Int[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
        {
            Vector2Int coord = pattern[i];
            // Rotate based on facing (North = 0°, East = 90°, South = 180°, West = 270°)
            Vector2Int rotatedCoord;
            switch (facing)
            {
                case UnitFacing.North:
                    rotatedCoord = coord; // No rotation
                    break;
                case UnitFacing.East:
                    rotatedCoord = new Vector2Int(-coord.y, coord.x); // 90° clockwise
                    break;
                case UnitFacing.South:
                    rotatedCoord = new Vector2Int(-coord.x, -coord.y); // 180°
                    break;
                case UnitFacing.West:
                    rotatedCoord = new Vector2Int(coord.y, -coord.x); // 270° clockwise
                    break;
                default:
                    rotatedCoord = coord;
                    break;
            }
            // Translate to world coordinates
            rotated[i] = unitPosition + rotatedCoord;
        }
        return rotated;
    }
    /// <summary>
    /// Clears the search history (useful for testing or when starting a new match).
    /// </summary>
    public static void ClearSearchHistory()
    {
        searchHistory.Clear();
    }
    // ========================================================================
    // LEGACY API (preserved for backward compatibility)
    // ========================================================================
    /// <summary>
    /// Legacy ExecuteSearch method - preserved for backward compatibility.
    /// New implementations should use PlanSearch instead.
    /// </summary>
    [System.Obsolete("Use PlanSearch with ParticipantAccess instead")]
    public static void ExecuteSearch(AITurnContext context, GameMaster gameMaster)
    {
        if (context == null || gameMaster == null || gameMaster.GameSearch == null)
        {
            Debug.LogError("[AI][Search] Null parameter in ExecuteSearch");
            return;
        }
        // Get search view to check quota
        if (!gameMaster.GameSearch.TryGetView(context.OwnParticipant, out GameSearch.ParticipantView searchView))
        {
            Debug.LogError("[AI][Search] Failed to get search view");
            return;
        }
        if (!searchView.CanAct)
        {
            Debug.Log("[AI][Search] Cannot act in search phase");
            return;
        }
        if (searchView.SearchesCommitted >= searchView.SearchLimit)
        {
            Debug.Log($"[AI][Search] Search limit reached ({searchView.SearchesCommitted}/{searchView.SearchLimit})");
            return;
        }
        // Find units with sensor systems
        List<RuntimeUnit> scanners = FindScanCapableUnits(context.OwnAliveUnits);
        if (scanners.Count == 0)
        {
            Debug.Log("[AI][Search] No units with sensor systems available");
            return;
        }
        // Select best scanner
        RuntimeUnit bestScanner = SelectBestScanner(scanners, context);
        if (bestScanner == null)
        {
            Debug.LogWarning("[AI][Search] Scanner selection failed");
            return;
        }
        // Find sensor system slot
        int sensorSlot = FindSensorSlot(bestScanner);
        if (sensorSlot < 0)
        {
            Debug.LogWarning($"[AI][Search] Unit {bestScanner.RuntimeID} has no sensor system");
            return;
        }
        // Choose best facing direction (default to North for obsolete code path)
        UnitFacing facing = UnitFacing.North;
        // Execute search via GameSearch
        bool success = gameMaster.GameSearch.TryCommitSearch(
            context.OwnParticipant,
            bestScanner.RuntimeID,
            sensorSlot,
            facing,
            out GameSearch.CommitResult result,
            out string validationMessage
        );
        if (!success)
        {
            Debug.LogWarning($"[AI][Search] Search failed for {bestScanner.RuntimeID}: {validationMessage}");
        }
        else
        {
            Debug.Log($"[AI][Search] {bestScanner.InstanceName} scanned facing {facing}, revealed {result.Contacts.Length} contacts");
        }
    }
    private static List<RuntimeUnit> FindScanCapableUnits(List<ParticipantUnitSnapshot> units)
    {
        // Legacy method - cannot work with snapshots, return empty
        return new List<RuntimeUnit>();
    }
    private static RuntimeUnit SelectBestScanner(List<RuntimeUnit> scanners, AITurnContext context)
    {
        RuntimeUnit bestScanner = null;
        bool bestIsScout = false;
        foreach (RuntimeUnit unit in scanners)
        {
            UnitRole role = ClassifyUnitRole(unit);
            bool isScout = (role == UnitRole.Scout);
            if (bestScanner == null)
            {
                bestScanner = unit;
                bestIsScout = isScout;
            }
            else if (isScout && !bestIsScout)
            {
                bestScanner = unit;
                bestIsScout = true;
            }
        }
        return bestScanner;
    }
    private static UnitFacing DirectionToFacing(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return UnitFacing.North;
        }
        int absX = Mathf.Abs(direction.x);
        int absY = Mathf.Abs(direction.y);
        if (absX > absY)
        {
            return direction.x > 0 ? UnitFacing.East : UnitFacing.West;
        }
        else
        {
            return direction.y > 0 ? UnitFacing.North : UnitFacing.South;
        }
    }
        // Stub methods for legacy code compatibility
    private static int FindSensorSlot(RuntimeUnit unit) { return -1; }
    private static UnitRole ClassifyUnitRole(RuntimeUnit unit) { return UnitRole.Striker; }    /// <summary>
    /// Calculates map center from tile snapshots.
    /// </summary>
    private static Vector2Int CalculateMapCenter(ParticipantGameSnapshot snapshot)
    {
        if (snapshot?.Tiles == null || snapshot.Tiles.Length == 0)
            return Vector2Int.zero;
        int maxX = snapshot.Tiles.Max(t => t.Coordinate.x);
        int maxY = snapshot.Tiles.Max(t => t.Coordinate.y);
        return new Vector2Int(maxX / 2, maxY / 2);
    }
}
