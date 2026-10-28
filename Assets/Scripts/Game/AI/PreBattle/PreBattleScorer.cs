using System.Linq;
using UnityEngine;

public static class PreBattleScorer
{
    public static float ScoreMineDeployment(
        Vector2Int coord,
        ParticipantUnitSnapshot deployingUnit,
        ParticipantGameSnapshot snapshot,
        FleetStance currentStance)
    {
        float score = 0f;

        // Calculate strategic positions
        Vector2Int mapCenter = CalculateMapCenter(snapshot);
        Vector2Int homeBase = CalculateHomeBase(snapshot);

        // PRIMARY SCORING: Reward forward placement toward map center
        float distFromHome = Vector2Int.Distance(coord, homeBase);
        float distToCenter = Vector2Int.Distance(coord, mapCenter);
        float maxMapDist = Vector2Int.Distance(homeBase, mapCenter);

        // Strong bonus for placing mines forward (toward center, away from home)
        float forwardProgress = distFromHome / (maxMapDist > 0 ? maxMapDist : 1f);
        score += forwardProgress * 60f; // 0-60 points based on how far forward

        // Bonus for being closer to center than to home
        if (distToCenter < distFromHome)
        {
            score += 30f; // Extra bonus for crossing midfield
        }

        // PENALTY: Heavily penalize placing mines near own base
        if (distFromHome < 5f)
        {
            score -= 100f; // Massive penalty for defensive mine placement
        }

        // SECONDARY SCORING: Choke points and enemy proximity
        Vector2Int[] chokes = IdentifyChokePoints(snapshot);
        bool isChokePoint = chokes.Any(c => Vector2Int.Distance(c, coord) < 3f);
        if (isChokePoint) score += 25f;

        // Moderate bonus for mines near visible enemies
        float minEnemyDist = 999f;
        if (snapshot.VisibleEnemies != null && snapshot.VisibleEnemies.Length > 0)
        {
            foreach (VisibleEnemyUnitSnapshot enemy in snapshot.VisibleEnemies)
            {
                if (enemy == null) continue;
                // Use first occupied coordinate as reference point
                if (enemy.OccupiedCoordinates != null && enemy.OccupiedCoordinates.Length > 0)
                {
                    float dist = Vector2Int.Distance(coord, enemy.OccupiedCoordinates[0]);
                    if (dist < minEnemyDist) minEnemyDist = dist;
                }
            }
        }

        if (minEnemyDist < 6f) score += 35f; // Close to visible enemy
        else if (minEnemyDist < 10f) score += 20f; // Moderate distance

        // Stance-specific adjustments
        if (currentStance == FleetStance.Retreat)
        {
            // In retreat, allow some defensive mines but still prefer forward
            if (distFromHome < 8f)
                score += 15f; // Small defensive bonus
        }
        else if (currentStance == FleetStance.Advance || currentStance == FleetStance.Hunt)
        {
            // Aggressive stances: push mines even further forward
            score += forwardProgress * 20f; // Extra 0-20 points for aggression
        }

        return score + Random.Range(-3f, 3f); // Small randomization
    }

    public static float ScoreCarriedUnitDeployment(
        Vector2Int coord,
        UnitFacing facing,
        ParticipantUnitSnapshot carrier,
        ParticipantCarriedUnitSnapshot carried,
        ParticipantGameSnapshot snapshot,
        FleetStance currentStance)
    {
        float score = 0f;

        // Calculate strategic positions
        Vector2Int mapCenter = CalculateMapCenter(snapshot);
        Vector2Int homeBase = CalculateHomeBase(snapshot);

        // Favor forward deployment
        float distFromHome = Vector2Int.Distance(coord, homeBase);
        float distToCenter = Vector2Int.Distance(coord, mapCenter);
        float maxMapDist = Vector2Int.Distance(homeBase, mapCenter);
        float forwardProgress = distFromHome / (maxMapDist > 0 ? maxMapDist : 1f);

        score += forwardProgress * 40f; // Forward deployment bonus

        // Find nearest enemy
        float minEnemyDist = 999f;
        Vector2Int nearestEnemyPos = Vector2Int.zero;
        bool hasNearestEnemy = false;

        if (snapshot.VisibleEnemies != null && snapshot.VisibleEnemies.Length > 0)
        {
            foreach (VisibleEnemyUnitSnapshot enemy in snapshot.VisibleEnemies)
            {
                if (enemy == null) continue;
                if (enemy.OccupiedCoordinates != null && enemy.OccupiedCoordinates.Length > 0)
                {
                    float dist = Vector2Int.Distance(coord, enemy.OccupiedCoordinates[0]);
                    if (dist < minEnemyDist)
                    {
                        minEnemyDist = dist;
                        nearestEnemyPos = enemy.OccupiedCoordinates[0];
                        hasNearestEnemy = true;
                    }
                }
            }
        }

        // Aggressive deployment near enemies
        if (currentStance == FleetStance.Advance && minEnemyDist < 10f)
        {
            score += 40f;
            if (hasNearestEnemy && IsFacingToward(coord, facing, nearestEnemyPos))
                score += 20f; // Bonus for facing enemy
        }

        // Check if carried unit has weapon systems
bool hasWeaponSystems = false;
if (carried.Definition != null && carried.Definition.WeaponSlots != null)
{
    hasWeaponSystems = carried.Definition.WeaponSlots.Length > 0;
}

        if (hasWeaponSystems)
        {
            score += forwardProgress * 30f; // Extra forward bonus for combat units
            if (minEnemyDist < 8f) score += 25f;
        }

        // Defensive stance allows closer deployment
        if (currentStance == FleetStance.Hold || currentStance == FleetStance.DefensiveFallback)
        {
            var activeUnits = snapshot.OwnUnits.Where(u => u != null && u.IsOnMap && u.LifeState == RuntimeUnitLifeState.Active).ToList();
            if (activeUnits.Count > 0)
            {
                float avgOwnDist = activeUnits.Average(u => Vector2Int.Distance(coord, u.MovementPivotCoordinate));
                if (avgOwnDist < 6f) score += 20f; // Stay near fleet
            }
        }

        return score + Random.Range(-3f, 3f);
    }

    /// <summary>
    /// Calculate map center from snapshot tiles.
    /// </summary>
    private static Vector2Int CalculateMapCenter(ParticipantGameSnapshot snapshot)
    {
        if (snapshot.Tiles == null || snapshot.Tiles.Length == 0)
            return Vector2Int.zero;

        int maxX = 0, maxY = 0;
        foreach (var tile in snapshot.Tiles)
        {
            if (tile.Coordinate.x > maxX) maxX = tile.Coordinate.x;
            if (tile.Coordinate.y > maxY) maxY = tile.Coordinate.y;
        }
        return new Vector2Int(maxX / 2, maxY / 2);
    }

    /// <summary>
    /// Calculate home base as average of own unit starting positions.
    /// </summary>
    private static Vector2Int CalculateHomeBase(ParticipantGameSnapshot snapshot)
    {
        if (snapshot.OwnUnits == null || snapshot.OwnUnits.Length == 0)
            return Vector2Int.zero;

        int sumX = 0, sumY = 0, count = 0;
        foreach (var unit in snapshot.OwnUnits)
        {
            if (unit != null && unit.IsOnMap && unit.LifeState == RuntimeUnitLifeState.Active)
            {
                sumX += unit.MovementPivotCoordinate.x;
                sumY += unit.MovementPivotCoordinate.y;
                count++;
            }
        }

        return count > 0 ? new Vector2Int(sumX / count, sumY / count) : Vector2Int.zero;
    }

    private static Vector2Int[] IdentifyChokePoints(ParticipantGameSnapshot snapshot)
    {
        // TODO: Implement proper choke point detection
        // For now, return empty array
        return new Vector2Int[0];
    }

    private static bool IsFacingToward(Vector2Int from, UnitFacing facing, Vector2Int target)
    {
        Vector2Int dir = target - from;
        return facing switch
        {
            UnitFacing.North => dir.y > 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y),
            UnitFacing.South => dir.y < 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y),
            UnitFacing.East => dir.x > 0 && Mathf.Abs(dir.y) < Mathf.Abs(dir.x),
            UnitFacing.West => dir.x < 0 && Mathf.Abs(dir.y) < Mathf.Abs(dir.x),
            _ => false
        };
    }
}
