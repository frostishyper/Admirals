using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit tests for DeploymentPlanner static class.
/// Tests zone analysis methods including centroid calculation, enemy direction estimation,
/// and facing calculation.
/// </summary>
public class DeploymentPlannerTest : MonoBehaviour
{
    /// <summary>
    /// Runs all DeploymentPlanner tests and logs results.
    /// </summary>
    public static void RunAllTests()
    {
        Debug.Log("[TEST][DeploymentPlanner] Starting test suite...");

        TestCalculateDeploymentCentroid_SingleTile();
        TestCalculateDeploymentCentroid_MultipleTiles();
        TestCalculateDeploymentCentroid_EmptyZone();
        TestEstimateEnemyDirection_NorthDeployment();
        TestEstimateEnemyDirection_SouthDeployment();
        TestEstimateEnemyDirection_EastDeployment();
        TestEstimateEnemyDirection_WestDeployment();
        TestCalculateFacingToward_North();
        TestCalculateFacingToward_East();
        TestCalculateFacingToward_South();
        TestCalculateFacingToward_West();
        TestCalculateFacingToward_Diagonal();
        TestCalculateFacingToward_ZeroVector();
        TestDivideIntoSectors_HorizontalZone();
        TestDivideIntoSectors_VerticalZone();
        TestDivideIntoSectors_EmptyZone();
        TestAssignUnitsToSectors_AnchorsCenter();
        TestAssignUnitsToSectors_ScoutsLeftRight();
        TestAssignUnitsToSectors_StrikersFillGaps();

        Debug.Log("[TEST][DeploymentPlanner] All tests completed!");
    }

    #region Centroid Calculation Tests

    private static void TestCalculateDeploymentCentroid_SingleTile()
    {
        List<Vector2Int> zone = new List<Vector2Int> { new Vector2Int(5, 7) };
        Vector2Int centroid = DeploymentPlanner.CalculateDeploymentCentroid(zone);

        AssertEqual(new Vector2Int(5, 7), centroid, "Single tile centroid");
    }

    private static void TestCalculateDeploymentCentroid_MultipleTiles()
    {
        List<Vector2Int> zone = new List<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(10, 0),
            new Vector2Int(0, 10),
            new Vector2Int(10, 10)
        };
        Vector2Int centroid = DeploymentPlanner.CalculateDeploymentCentroid(zone);

        // Average: (0+10+0+10)/4 = 5, (0+0+10+10)/4 = 5
        AssertEqual(new Vector2Int(5, 5), centroid, "Four corner tiles centroid");
    }

    private static void TestCalculateDeploymentCentroid_EmptyZone()
    {
        List<Vector2Int> zone = new List<Vector2Int>();
        Vector2Int centroid = DeploymentPlanner.CalculateDeploymentCentroid(zone);

        AssertEqual(Vector2Int.zero, centroid, "Empty zone centroid");
    }

    #endregion

    #region Enemy Direction Estimation Tests

    private static void TestEstimateEnemyDirection_NorthDeployment()
    {
        // Mock GridManager with 20x20 map
        MockGridManager gridManager = new MockGridManager(20, 20);

        // Deployment in North (y near max)
        Vector2Int centroid = new Vector2Int(10, 18);
        Vector2Int direction = DeploymentPlanner.EstimateEnemyDirection(centroid, gridManager);

        // Should point South (0, -1) as opposite of North
        AssertEqual(new Vector2Int(0, -1), direction, "North deployment points South");
    }

    private static void TestEstimateEnemyDirection_SouthDeployment()
    {
        MockGridManager gridManager = new MockGridManager(20, 20);

        // Deployment in South (y near 0)
        Vector2Int centroid = new Vector2Int(10, 2);
        Vector2Int direction = DeploymentPlanner.EstimateEnemyDirection(centroid, gridManager);

        // Should point North (0, 1) as opposite of South
        AssertEqual(new Vector2Int(0, 1), direction, "South deployment points North");
    }

    private static void TestEstimateEnemyDirection_EastDeployment()
    {
        MockGridManager gridManager = new MockGridManager(20, 20);

        // Deployment in East (x near max)
        Vector2Int centroid = new Vector2Int(18, 10);
        Vector2Int direction = DeploymentPlanner.EstimateEnemyDirection(centroid, gridManager);

        // Should point West (-1, 0) as opposite of East
        AssertEqual(new Vector2Int(-1, 0), direction, "East deployment points West");
    }

    private static void TestEstimateEnemyDirection_WestDeployment()
    {
        MockGridManager gridManager = new MockGridManager(20, 20);

        // Deployment in West (x near 0)
        Vector2Int centroid = new Vector2Int(2, 10);
        Vector2Int direction = DeploymentPlanner.EstimateEnemyDirection(centroid, gridManager);

        // Should point East (1, 0) as opposite of West
        AssertEqual(new Vector2Int(1, 0), direction, "West deployment points East");
    }

    #endregion

    #region Facing Calculation Tests

    private static void TestCalculateFacingToward_North()
    {
        Vector2Int direction = new Vector2Int(0, 1);
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        AssertEqual(UnitFacing.North, facing, "North direction");
    }

    private static void TestCalculateFacingToward_East()
    {
        Vector2Int direction = new Vector2Int(1, 0);
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        AssertEqual(UnitFacing.East, facing, "East direction");
    }

    private static void TestCalculateFacingToward_South()
    {
        Vector2Int direction = new Vector2Int(0, -1);
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        AssertEqual(UnitFacing.South, facing, "South direction");
    }

    private static void TestCalculateFacingToward_West()
    {
        Vector2Int direction = new Vector2Int(-1, 0);
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        AssertEqual(UnitFacing.West, facing, "West direction");
    }

    private static void TestCalculateFacingToward_Diagonal()
    {
        // Northeast diagonal should choose North or East based on dot product
        // (1, 1) has equal dot product with North (0, 1) and East (1, 0)
        Vector2Int direction = new Vector2Int(1, 1);
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        // Should be either North or East (both have dot product = 1)
        // Implementation picks the first max found, which is North
        AssertEqual(UnitFacing.North, facing, "Northeast diagonal");
    }

    private static void TestCalculateFacingToward_ZeroVector()
    {
        Vector2Int direction = Vector2Int.zero;
        UnitFacing facing = DeploymentPlanner.CalculateFacingToward(direction);

        // Should default to North
        AssertEqual(UnitFacing.North, facing, "Zero vector defaults to North");
    }

    #endregion

    #region Sector Division Tests

    private static void TestDivideIntoSectors_HorizontalZone()
    {
        // Create horizontal deployment zone (5 tiles wide, 3 tiles tall)
        List<Vector2Int> zone = new List<Vector2Int>();
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                zone.Add(new Vector2Int(x, y));
            }
        }

        Vector2Int centroid = new Vector2Int(2, 1); // Center of 5x3 grid
        Vector2Int enemyDirection = new Vector2Int(0, 1); // Enemy to North

        var sectors = DeploymentPlanner.DivideIntoSectors(zone, centroid, enemyDirection);

        // With enemy to North, perpendicular is East (1, 0)
        // Left sector should have negative x offsets
        // Right sector should have positive x offsets
        // Center should have near-zero x offsets

        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Left), "Has Left sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Center), "Has Center sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Right), "Has Right sector");
        AssertTrue(sectors[DeploymentPlanner.Sector.Left].Count > 0, "Left sector not empty");
        AssertTrue(sectors[DeploymentPlanner.Sector.Right].Count > 0, "Right sector not empty");

        Debug.Log($"[TEST] HorizontalZone sectors - Left: {sectors[DeploymentPlanner.Sector.Left].Count}, " +
                  $"Center: {sectors[DeploymentPlanner.Sector.Center].Count}, " +
                  $"Right: {sectors[DeploymentPlanner.Sector.Right].Count}");
    }

    private static void TestDivideIntoSectors_VerticalZone()
    {
        // Create vertical deployment zone (3 tiles wide, 5 tiles tall)
        List<Vector2Int> zone = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                zone.Add(new Vector2Int(x, y));
            }
        }

        Vector2Int centroid = new Vector2Int(1, 2); // Center of 3x5 grid
        Vector2Int enemyDirection = new Vector2Int(1, 0); // Enemy to East

        var sectors = DeploymentPlanner.DivideIntoSectors(zone, centroid, enemyDirection);

        // With enemy to East, perpendicular is South (0, -1)
        // Sectors should be divided along vertical axis

        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Left), "Has Left sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Center), "Has Center sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Right), "Has Right sector");

        Debug.Log($"[TEST] VerticalZone sectors - Left: {sectors[DeploymentPlanner.Sector.Left].Count}, " +
                  $"Center: {sectors[DeploymentPlanner.Sector.Center].Count}, " +
                  $"Right: {sectors[DeploymentPlanner.Sector.Right].Count}");
    }

    private static void TestDivideIntoSectors_EmptyZone()
    {
        List<Vector2Int> zone = new List<Vector2Int>();
        Vector2Int centroid = Vector2Int.zero;
        Vector2Int enemyDirection = new Vector2Int(0, 1);

        var sectors = DeploymentPlanner.DivideIntoSectors(zone, centroid, enemyDirection);

        // Should return empty sectors without crashing
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Left), "Has Left sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Center), "Has Center sector");
        AssertTrue(sectors.ContainsKey(DeploymentPlanner.Sector.Right), "Has Right sector");
        AssertEqual(0, sectors[DeploymentPlanner.Sector.Left].Count, "Left sector empty");
        AssertEqual(0, sectors[DeploymentPlanner.Sector.Center].Count, "Center sector empty");
        AssertEqual(0, sectors[DeploymentPlanner.Sector.Right].Count, "Right sector empty");
    }

    #endregion

    #region Unit Assignment Tests

    private static void TestAssignUnitsToSectors_AnchorsCenter()
    {
        // Create mock units and snapshot with Anchor roles
        var units = CreateMockUnits(3);
        var snapshot = CreateMockSnapshot(units, UnitRole.Anchor, UnitRole.Anchor, UnitRole.Anchor);
        var sectorTiles = CreateMockSectorTiles();

        var assignments = DeploymentPlanner.AssignUnitsToSectors(units, snapshot, sectorTiles);

        // All Anchors should be assigned to Center
        int centerCount = 0;
        foreach (var assignment in assignments.Values)
        {
            if (assignment == DeploymentPlanner.Sector.Center)
            {
                centerCount++;
            }
        }

        AssertEqual(3, centerCount, "All Anchors assigned to Center");
    }

    private static void TestAssignUnitsToSectors_ScoutsLeftRight()
    {
        // Create mock units with Scout roles
        var units = CreateMockUnits(4);
        var snapshot = CreateMockSnapshot(units, UnitRole.Scout, UnitRole.Scout, UnitRole.Scout, UnitRole.Scout);
        var sectorTiles = CreateMockSectorTiles();

        var assignments = DeploymentPlanner.AssignUnitsToSectors(units, snapshot, sectorTiles);

        // Scouts should alternate between Left and Right
        int leftCount = 0;
        int rightCount = 0;

        foreach (var assignment in assignments.Values)
        {
            if (assignment == DeploymentPlanner.Sector.Left) leftCount++;
            if (assignment == DeploymentPlanner.Sector.Right) rightCount++;
        }

        // With 4 scouts, should be 2 left and 2 right
        AssertEqual(2, leftCount, "2 Scouts to Left");
        AssertEqual(2, rightCount, "2 Scouts to Right");
    }

    private static void TestAssignUnitsToSectors_StrikersFillGaps()
    {
        // Create mixed fleet: 2 Anchors (Center), 2 Scouts (Left/Right), 3 Strikers (fill)
        var units = CreateMockUnits(7);
        var snapshot = CreateMockSnapshot(units,
            UnitRole.Anchor, UnitRole.Anchor,
            UnitRole.Scout, UnitRole.Scout,
            UnitRole.Striker, UnitRole.Striker, UnitRole.Striker);
        var sectorTiles = CreateMockSectorTiles();

        var assignments = DeploymentPlanner.AssignUnitsToSectors(units, snapshot, sectorTiles);

        // Count assignments per sector
        int leftCount = 0, centerCount = 0, rightCount = 0;
        foreach (var assignment in assignments.Values)
        {
            if (assignment == DeploymentPlanner.Sector.Left) leftCount++;
            if (assignment == DeploymentPlanner.Sector.Center) centerCount++;
            if (assignment == DeploymentPlanner.Sector.Right) rightCount++;
        }

        // Expected: 2 Anchors in Center, 1 Scout in Left, 1 Scout in Right
        // 3 Strikers should balance the sectors
        Debug.Log($"[TEST] StrikersFillGaps distribution - Left: {leftCount}, Center: {centerCount}, Right: {rightCount}");

        // Strikers should balance, so no sector should be empty
        AssertTrue(leftCount > 0, "Left sector has units");
        AssertTrue(centerCount >= 2, "Center has at least Anchors");
        AssertTrue(rightCount > 0, "Right sector has units");
    }

    #endregion

    #region Test Utilities

    private static void AssertEqual(Vector2Int expected, Vector2Int actual, string testName)
    {
        if (expected == actual)
        {
            Debug.Log($"[TEST][✓] {testName}: PASS (expected {expected}, got {actual})");
        }
        else
        {
            Debug.LogError($"[TEST][✗] {testName}: FAIL (expected {expected}, got {actual})");
        }
    }

    private static void AssertEqual(UnitFacing expected, UnitFacing actual, string testName)
    {
        if (expected == actual)
        {
            Debug.Log($"[TEST][✓] {testName}: PASS (expected {expected}, got {actual})");
        }
        else
        {
            Debug.LogError($"[TEST][✗] {testName}: FAIL (expected {expected}, got {actual})");
        }
    }

    private static void AssertEqual(int expected, int actual, string testName)
    {
        if (expected == actual)
        {
            Debug.Log($"[TEST][✓] {testName}: PASS (expected {expected}, got {actual})");
        }
        else
        {
            Debug.LogError($"[TEST][✗] {testName}: FAIL (expected {expected}, got {actual})");
        }
    }

    private static void AssertTrue(bool condition, string testName)
    {
        if (condition)
        {
            Debug.Log($"[TEST][✓] {testName}: PASS");
        }
        else
        {
            Debug.LogError($"[TEST][✗] {testName}: FAIL");
        }
    }

    private static List<RuntimeUnit> CreateMockUnits(int count)
    {
        var units = new List<RuntimeUnit>();
        for (int i = 0; i < count; i++)
        {
            var unit = ScriptableObject.CreateInstance<RuntimeUnit>();
            unit.RuntimeID = $"Unit_{i}";
            units.Add(unit);
        }
        return units;
    }

    private static FleetSnapshot CreateMockSnapshot(List<RuntimeUnit> units, params UnitRole[] roles)
    {
        var snapshot = new FleetSnapshot();
        snapshot.Members = new List<FleetMember>();

        for (int i = 0; i < units.Count && i < roles.Length; i++)
        {
            snapshot.Members.Add(new FleetMember
            {
                Unit = units[i],
                Role = roles[i],
                Position = Vector2Int.zero,
                Facing = UnitFacing.North,
                HpFraction = 1.0f,
                EffectiveFragility = 0.0f
            });
        }

        return snapshot;
    }

    private static Dictionary<DeploymentPlanner.Sector, List<Vector2Int>> CreateMockSectorTiles()
    {
        // Create mock sector tiles (3x3 grid per sector)
        var sectors = new Dictionary<DeploymentPlanner.Sector, List<Vector2Int>>();

        sectors[DeploymentPlanner.Sector.Left] = new List<Vector2Int>();
        for (int i = 0; i < 9; i++)
        {
            sectors[DeploymentPlanner.Sector.Left].Add(new Vector2Int(i, 0));
        }

        sectors[DeploymentPlanner.Sector.Center] = new List<Vector2Int>();
        for (int i = 0; i < 9; i++)
        {
            sectors[DeploymentPlanner.Sector.Center].Add(new Vector2Int(i, 1));
        }

        sectors[DeploymentPlanner.Sector.Right] = new List<Vector2Int>();
        for (int i = 0; i < 9; i++)
        {
            sectors[DeploymentPlanner.Sector.Right].Add(new Vector2Int(i, 2));
        }

        return sectors;
    }

    #endregion

    #region Mock GridManager

    /// <summary>
    /// Mock GridManager for testing without Unity dependencies.
    /// </summary>
    private class MockGridManager : GridManager
    {
        public MockGridManager(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }

    #endregion
}
