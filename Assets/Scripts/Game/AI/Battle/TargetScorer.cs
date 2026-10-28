using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides target prioritization scoring for AI Battle phase decision-making.
/// Evaluates potential enemy targets based on threat level, fragility, distance,
/// and fleet coordination penalties to determine optimal engagement targets.
/// </summary>
public static class TargetScorer
{
    // Scoring weight constants for target evaluation
    private const float ThreatWeight = 0.35f;
    private const float FragilityWeight = 0.35f;
    private const float DistanceWeight = 0.15f;
    private const float VisibilityBonus = 0.15f;

    /// <summary>
    /// Scores a potential target coordinate for engagement priority.
    /// </summary>
    /// <param name="coordinate">Map coordinate of the potential target.</param>
    /// <param name="attacker">Snapshot of the attacking unit.</param>
    /// <param name="context">Battle context containing game state and coordination tracking.</param>
    /// <returns>
    /// TargetEvaluation containing component scores and total weighted score.
    /// Higher total scores indicate higher priority targets.
    /// </returns>
    /// <remarks>
    /// Scoring Algorithm:
    /// - ThreatScore (0-100): Enemy weapon capabilities, unit type, and domain advantage
    /// - FragilityScore (0-100): Remaining health percentage (low health = high fragility)
    /// - DistanceScore (0-50): Manhattan distance from attacker (closer = higher score)
    /// - CoordinationPenalty (0 to -40): Penalty for already-engaged targets this phase
    /// - VisibilityBonus: +15% to total if absolutely visible (not just contact-based)
    /// 
    /// TotalScore = (Threat × 0.35) + (Fragility × 0.35) + (Distance × 0.15) + CoordinationPenalty
    /// TotalScore receives +15% bonus if IsAbsolutelyVisible is true.
    /// </remarks>
    public static TargetEvaluation ScoreTarget(Vector2Int coordinate, ParticipantUnitSnapshot attacker, BattleContext context)
    {
        TargetEvaluation evaluation = new TargetEvaluation
        {
            Coordinate = coordinate,
            RuntimeUnitID = "",
            TargetDomain = Domain.Surface, // Will be set by helper methods in later tasks
            ThreatScore = 0f,
            FragilityScore = 0f,
            DistanceScore = 0f,
            CoordinationPenalty = 0f,
            IsAbsolutelyVisible = false
        };

        // Try to find visible enemy unit at the target coordinate
        // Find visible enemy at coordinate (inline to avoid compilation issue)
        VisibleEnemyUnitSnapshot enemyVisible = null;
        if (context.Snapshot != null && context.Snapshot.VisibleEnemies != null)
        {
            foreach (var enemy in context.Snapshot.VisibleEnemies)
            {
                if (enemy != null && enemy.OccupiedCoordinates != null)
                {
                    foreach (var coord in enemy.OccupiedCoordinates)
                    {
                        if (coord == coordinate)
                        {
                            enemyVisible = enemy;
                            break;
                        }
                    }
                    if (enemyVisible != null) break;
                }
            }
        }
        
        // Calculate component scores
        // Note: EstimateEnemyThreat currently expects ParticipantUnitSnapshot but we only have
        // VisibleEnemyUnitSnapshot during target selection. For now, pass null and use heuristics.
        // Future enhancement: modify EstimateEnemyThreat to work with VisibleEnemyUnitSnapshot
        evaluation.ThreatScore = EstimateEnemyThreatFromVisible(enemyVisible);
        
        // Fragility cannot be calculated without health data (not exposed to enemy visibility)
        // Use baseline score for now
        evaluation.FragilityScore = 50f;
        
        if (enemyVisible != null)
        {
            evaluation.TargetDomain = enemyVisible.CurrentDomain;
            evaluation.IsAbsolutelyVisible = true;
            // Note: RuntimeUnitID is not exposed for enemies, leave empty
        }
        
        evaluation.DistanceScore = GetDistanceScore(attacker.MovementPivotCoordinate, coordinate);
        evaluation.CoordinationPenalty = GetCoordinationPenalty(coordinate, context.EngagedTargetsThisPhase);

        // Calculate weighted total score
        float baseScore = (evaluation.ThreatScore * ThreatWeight)
                        + (evaluation.FragilityScore * FragilityWeight)
                        + (evaluation.DistanceScore * DistanceWeight)
                        + evaluation.CoordinationPenalty;

        // Apply visibility bonus
        if (evaluation.IsAbsolutelyVisible)
        {
            evaluation.TotalScore = baseScore * (1f + VisibilityBonus);
        }
        else
        {
            evaluation.TotalScore = baseScore;
        }

        return evaluation;
    }

    // Helper method stubs - to be implemented in later tasks

    /// <summary>
    /// Estimates the threat level of an enemy unit based on weapon capabilities and unit type.
    /// </summary>
    /// <param name="enemy">Snapshot of the enemy unit to evaluate.</param>
    /// <returns>Threat score from 0-100. Returns 50 if enemy is null or invalid.</returns>
    /// <remarks>
    /// Scoring Logic:
    /// - Counts weapon slots with charges > 0
    /// - Heavy weapons (3+ slots): +40
    /// - Medium weapons (2 slots): +25
    /// - Light weapons (1 slot): +10
    /// - Carrier/Support units: +20 bonus (detected via unit name)
    /// - Domain advantage: +15 (reserved for future implementation when attacker context available)
    /// - Baseline: 50 for null/invalid units
    /// 
    /// NOTE: This method requires full ParticipantUnitSnapshot with weapon data.
    /// For visible enemies, use EstimateEnemyThreatFromVisible instead.
    /// </remarks>
    private static float EstimateEnemyThreat(ParticipantUnitSnapshot enemy)
    {
        // Return baseline for null or invalid enemy
        if (enemy == null || enemy.Weapons == null)
        {
            return 50f;
        }

        float threatScore = 0f;

        // Count weapon slots with charges > 0
        int activeWeaponCount = 0;
        foreach (var weapon in enemy.Weapons)
        {
            if (weapon != null && weapon.CurrentCharges > 0)
            {
                activeWeaponCount++;
            }
        }

        // Weapon capability scoring based on active weapon count
        if (activeWeaponCount >= 3)
        {
            threatScore += 40f; // Heavy weapons
        }
        else if (activeWeaponCount == 2)
        {
            threatScore += 25f; // Medium weapons
        }
        else if (activeWeaponCount == 1)
        {
            threatScore += 10f; // Light weapons
        }

        // Check for carrier or support unit bonus
        if (!string.IsNullOrEmpty(enemy.DefinitionName))
        {
            string unitName = enemy.DefinitionName.ToLower();
            if (unitName.Contains("carrier") || unitName.Contains("support"))
            {
                threatScore += 20f;
            }
        }

        // Domain advantage bonus: +15
        // Note: This requires attacker context to determine if attacker has weapons
        // effective against the target's domain. Reserved for future enhancement
        // when attacker parameter is added to this method or passed through ScoreTarget.

        // Clamp to 0-100 range
        return Mathf.Clamp(threatScore, 0f, 100f);
    }

    /// <summary>
    /// Estimates the threat level of a visible enemy unit using heuristics.
    /// Since weapon data is not exposed for visible enemies, uses unit definition and type.
    /// </summary>
    /// <param name="enemy">Visible enemy unit snapshot.</param>
    /// <returns>Threat score from 0-100. Returns 50 if enemy is null.</returns>
    /// <remarks>
    /// Heuristic Scoring Logic:
    /// - Baseline threat: 30 (assumes moderate weapons)
    /// - Carrier/Support units: +20 bonus (detected via unit name or definition)
    /// - Large units (2x2 or bigger footprint): +10 (typically more weapons)
    /// - Domain consideration: reserved for future enhancement
    /// </remarks>
    private static float EstimateEnemyThreatFromVisible(VisibleEnemyUnitSnapshot enemy)
    {
        // Return baseline for null enemy
        if (enemy == null)
        {
            return 50f;
        }

        float threatScore = 30f; // Baseline assumes moderate weapons

        // Check for carrier or support unit bonus
        if (!string.IsNullOrEmpty(enemy.DefinitionName))
        {
            string unitName = enemy.DefinitionName.ToLower();
            if (unitName.Contains("carrier") || unitName.Contains("support"))
            {
                threatScore += 20f;
            }
        }

        // Larger units tend to have more weapons
        if (enemy.OccupiedCoordinates != null && enemy.OccupiedCoordinates.Length >= 4)
        {
            threatScore += 10f; // 2x2 or larger footprint
        }

        // Clamp to 0-100 range
        return Mathf.Clamp(threatScore, 0f, 100f);
    }

    
    /// <summary>
    /// Calculates fragility score based on remaining health percentage.
    /// Lower health percentages result in higher fragility scores, making
    /// damaged units more attractive targets for elimination.
    /// </summary>
    /// <param name="currentHealth">Current health of the unit.</param>
    /// <param name="maxHealth">Maximum health of the unit.</param>
    /// <returns>
    /// Fragility score from 0-100:
    /// - 0-25% health: 100 points (critical/destroyed)
    /// - 26-50% health: 75 points (heavily damaged)
    /// - 51-75% health: 50 points (moderately damaged)
    /// - 76-100% health: 25 points (healthy)
    /// 
    /// Edge cases:
    /// - maxHealth <= 0: returns 50 (baseline, unknown health)
    /// - currentHealth <= 0: returns 100 (destroyed/critical)
    /// - currentHealth >= maxHealth: returns 25 (full health)
    /// </returns>
    private static float CalculateFragility(int currentHealth, int maxHealth)
    {
        // Handle edge case: invalid max health
        if (maxHealth <= 0)
        {
            return 50f; // Baseline score when health data is unknown
        }

        // Handle edge case: destroyed or critical health
        if (currentHealth <= 0)
        {
            return 100f;
        }

        // Handle edge case: at or above max health
        if (currentHealth >= maxHealth)
        {
            return 25f;
        }

        // Calculate health percentage
        float healthPercent = (currentHealth / (float)maxHealth) * 100f;

        // Score based on health percentage ranges
        if (healthPercent <= 25f)
        {
            return 100f; // Critical: 0-25% health
        }
        else if (healthPercent <= 50f)
        {
            return 75f; // Heavily damaged: 26-50% health
        }
        else if (healthPercent <= 75f)
        {
            return 50f; // Moderately damaged: 51-75% health
        }
        else
        {
            return 25f; // Healthy: 76-100% health
        }
    }
    
    /// <summary>
    /// Calculates a distance-based score favoring closer targets.
    /// </summary>
    /// <param name="from">Starting coordinate (attacker position).</param>
    /// <param name="to">Target coordinate.</param>
    /// <returns>Score from 0-50 based on Manhattan distance (closer = higher score).</returns>
    private static float GetDistanceScore(Vector2Int from, Vector2Int to)
    {
        int distance = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);

        if (distance <= 3)
        {
            return 50f; // Very close
        }
        else if (distance <= 6)
        {
            return 35f; // Close
        }
        else if (distance <= 10)
        {
            return 20f; // Moderate
        }
        else
        {
            return 10f; // Far
        }
    }
    
    /// <summary>
    /// Calculates a coordination penalty to discourage excessive focus fire on the same target.
    /// </summary>
    /// <param name="coord">Target coordinate to check.</param>
    /// <param name="engaged">Set of coordinates already engaged this Battle phase.</param>
    /// <returns>Penalty value from 0 to -20 (0 if not engaged, -20 if already engaged).</returns>
    /// <remarks>
    /// For MVP, implements simple binary tracking:
    /// - Not engaged yet: 0 (no penalty)
    /// - Already engaged: -20 (discourage but allow)
    /// 
    /// Note: Enhancement could use Dictionary&lt;Vector2Int, int&gt; to track
    /// engagement count and apply -40 penalty for twice-engaged targets.
    /// </remarks>
    private static float GetCoordinationPenalty(Vector2Int coord, HashSet<Vector2Int> engaged)
    {
        if (engaged.Contains(coord))
        {
            return -20f; // Already engaged this phase - discourage
        }
        else
        {
            return 0f; // Not yet engaged - no penalty
        }
    }
}
