using System.Collections.Generic;
using UnityEngine;

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
    // TILE SCORING WEIGHTS
    // ========================================================================
    // All weights are expressed as fractions of unit maxHealth to ensure
    // they remain balanced across different unit scales (400 HP corvettes
    // vs 2000 HP carriers). This prevents raw damage values from dominating
    // tactical considerations.

    /// <summary>
    /// Score bonus when expected damage would kill the target.
    /// Dominates other factors - guaranteed kills are always pursued.
    /// </summary>
    private const float KillBonus = 50f;

    /// <summary>
    /// Maximum score contribution from non-lethal damage to a target.
    /// Scaled by (expectedDamage / maxHealth) * MaxDamageScore.
    /// </summary>
    private const float MaxDamageScore = 40f;

    /// <summary>
    /// Bonus for attacking low-health targets (easier kills).
    /// Scaled by target vulnerability.
    /// </summary>
    private const float TargetWeaknessWeight = 10f;

    /// <summary>
    /// Penalty weight for expected incoming damage at a tile.
    /// Scaled by (incomingDamage / maxHealth) * DangerWeight.
    /// Increased by 1.5x during Retreat or DefensiveFallback stances.
    /// </summary>
    private const float DangerWeight = 35f;

    /// <summary>
    /// Additional penalty for high-fragility units in dangerous positions.
    /// Applied when EffectiveFragility > FragilityThreshold.
    /// </summary>
    private const float SelfRiskWeight = 15f;

    /// <summary>
    /// Penalty applied when targeting an enemy already claimed by another moving unit.
    /// Encourages fleet to spread across multiple targets rather than converging on one.
    /// Smaller than KillBonus so lethal attacks override this penalty.
    /// </summary>
    private const float AlreadyClaimedPenalty = 20f;

    /// <summary>
    /// Base weight for formation cohesion (attraction to fleet centroid).
    /// Scaled by distance and multiplied by stance-specific factor:
    /// - Retreat/DefensiveFallback: 2.0x (pull fleet together)
    /// - Advance/Hunt: 0.5x (allow spread)
    /// - Scouts in Hunt: -1.0x (push forward, away from fleet)
    /// </summary>
    private const float CohesionBaseWeight = 8f;

    /// <summary>
    /// Penalty weight for high-fragility units moving toward nearest contact.
    /// Applied when EffectiveFragility > FragilityThreshold.
    /// Keeps vulnerable units away from direct engagement.
    /// </summary>
    private const float FragilityPushbackWeight = 12f;

    /// <summary>
    /// Bonus weight for Scout units moving toward contacts during Hunt/Advance.
    /// Encourages forward reconnaissance behavior.
    /// </summary>
    private const float ScoutForwardWeight = 6f;

    /// <summary>
    /// Effective fragility threshold above which pushback penalties apply.
    /// Range: 0.0 (undamaged Anchor) to 1.0+ (critically damaged Scout).
    /// </summary>
    private const float FragilityThreshold = 0.6f;

    /// <summary>
    /// Directional bias weight for Retreat stance (toward home centroid).
    /// </summary>
    private const float RetreatDirectionalWeight = 18f;

    /// <summary>
    /// Directional bias weight for DefensiveFallback stance (toward home centroid).
    /// </summary>
    private const float FallbackDirectionalWeight = 10f;

    /// <summary>
    /// Directional bias weight for Advance stance (toward nearest contact).
    /// </summary>
    private const float AdvanceDirectionalWeight = 10f;

    /// <summary>
    /// Health fraction threshold for flee-to-safety override.
    /// Units below this threshold will prioritize safety over attack unless
    /// they can secure a guaranteed kill.
    /// </summary>
    private const float FleeHealthFraction = 0.3f;

    // ========================================================================
    // MAIN API
    // ========================================================================

    /// <summary>
    /// Selects the best destination for a unit by scoring all reachable tiles.
    /// Balances attack opportunity, danger assessment, formation cohesion, and
    /// stance-driven directional bias.
    /// 
    /// Critically damaged units (HP &lt;= 30%) may flee to safety unless a
    /// guaranteed kill is available.
    /// </summary>
    /// <param name="unit">The unit to move.</param>
    /// <param name="context">Turn context with visible enemies and game state.</param>
    /// <param name="snapshot">Fleet snapshot with stance, centroid, and member metadata.</param>
    /// <param name="gameMovement">GameMovement subsystem for pathfinding validation.</param>
    /// <param name="gridManager">Grid manager for spatial calculations.</param>
    /// <param name="claimedTargets">Set of enemies already targeted by other moving units (may be null).</param>
    /// <returns>Movement decision with destination, likely target, and flee status.</returns>
    public static Decision ChooseDestination(
        RuntimeUnit unit,
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager,
        HashSet<RuntimeUnit> claimedTargets)
    {
        // Get all reachable tiles within movement budget
        List<Vector2Int> candidates = GetPlaceableReachableTiles(unit, gameMovement, gridManager);

        if (candidates.Count == 0)
        {
            // No valid destinations - stay in place
            return new Decision
            {
                destination = GetUnitPosition(unit, gridManager),
                likelyTarget = null,
                isFleeing = false
            };
        }

        // Pre-compute danger at each tile (reused across all target evaluations)
        Dictionary<Vector2Int, float> dangerByTile = new Dictionary<Vector2Int, float>();
        foreach (Vector2Int tile in candidates)
        {
            dangerByTile[tile] = DangerAtTile(tile, unit, context, gridManager);
        }

        // Find this unit's FleetMember entry for role and fragility metadata
        FleetMember member = FindMember(snapshot, unit);

        // Score each (tile, target) combination
        float bestScore = float.NegativeInfinity;
        Vector2Int bestTile = GetUnitPosition(unit, gridManager);
        RuntimeUnit bestTarget = null;

        foreach (Vector2Int tile in candidates)
        {
            foreach (RuntimeUnit enemy in context.EnemyVisibleUnits)
            {
                // Check if this unit can attack this enemy from this tile
                bool canAttack = CanAttackTargetFromTile(unit, tile, enemy, gridManager);
                if (!canAttack)
                {
                    continue;
                }

                // Score the attack opportunity
                float score = ScoreTile(tile, unit, enemy, context, snapshot, gridManager, claimedTargets, dangerByTile[tile]);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                    bestTarget = enemy;
                }
            }

            // Also score tiles as pure repositioning (no target)
            float repoScore = ScoreTile(tile, unit, null, context, snapshot, gridManager, claimedTargets, dangerByTile[tile]);
            if (repoScore > bestScore)
            {
                bestScore = repoScore;
                bestTile = tile;
                bestTarget = null;
            }
        }

        // Flee-to-safety override for critically damaged units
        bool isCriticallyLow = unit.CurrentHealth <= unit.MaxHealth * FleeHealthFraction;
        bool bestIsLethal = bestTarget != null && WouldKillTarget(unit, bestTarget);

        if (isCriticallyLow && !bestIsLethal)
        {
            // Override: find safest tile regardless of attack opportunity
            Vector2Int safeTile = FindSafestTile(candidates, unit, context, gridManager, dangerByTile);
            return new Decision
            {
                destination = safeTile,
                likelyTarget = null,
                isFleeing = true
            };
        }

        return new Decision
        {
            destination = bestTile,
            likelyTarget = bestTarget,
            isFleeing = false
        };
    }

    // ========================================================================
    // TILE SCORING
    // ========================================================================

    /// <summary>
    /// Scores a candidate tile based on weighted sum of multiple factors:
    /// - Attack opportunity (if enemy is not null)
    /// - Danger at the tile
    /// - Formation cohesion
    /// - Role-based bias
    /// - Stance-driven directional bias
    /// </summary>
    private static float ScoreTile(
        Vector2Int tile,
        RuntimeUnit unit,
        RuntimeUnit targetEnemy,
        AITurnContext context,
        FleetSnapshot snapshot,
        GridManager gridManager,
        HashSet<RuntimeUnit> claimedTargets,
        float dangerAtTile)
    {
        float score = 0f;

        // Attack opportunity score (only if targeting an enemy)
        if (targetEnemy != null)
        {
            score += ScoreAttackFromTile(tile, unit, targetEnemy, gridManager, claimedTargets, snapshot);
        }

        // Danger penalty
        score -= dangerAtTile * GetDangerMultiplier(snapshot.CurrentStance);

        // Formation cohesion
        score += ScoreFormationTerms(tile, unit, snapshot, gridManager);

        // Stance-driven directional bias
        score += ScoreDirectionalBias(tile, unit, snapshot, gridManager, context);

        return score;
    }

    /// <summary>
    /// Scores attack opportunity from a tile against a specific enemy.
    /// Returns positive score based on expected damage, kill potential,
    /// target weakness, and whether the target is already claimed.
    /// 
    /// Implementation follows Requirements 22.1-22.7:
    /// - Calculate distance to each visible enemy from candidate tile
    /// - Check if enemies are within weapon range
    /// - Add kill bonus (+50) if expected damage >= enemy currentHealth
    /// - Add damage score (expectedDamage / maxHP * 40) otherwise
    /// - Add target weakness weight (+10) for low-HP enemies
    /// - Reduce score (-20) if target is already claimed
    /// - Scale by FleetStance: Advance * 1.5, Retreat * 0.3
    /// </summary>
    private static float ScoreAttackFromTile(
        Vector2Int tile,
        RuntimeUnit unit,
        RuntimeUnit enemy,
        GridManager gridManager,
        HashSet<RuntimeUnit> claimedTargets,
        FleetSnapshot snapshot)
    {
        float score = 0f;
        Vector2Int enemyPos = GetUnitPosition(enemy, gridManager);
        float distance = Vector2Int.Distance(tile, enemyPos);

        // Iterate through all weapon slots to find best weapon for this enemy
        // Requirements 22.1, 22.2
        float bestExpectedDamage = 0f;
        bool hasWeaponInRange = false;

        if (unit.WeaponSlots != null)
        {
            foreach (var weaponSlot in unit.WeaponSlots)
            {
                if (weaponSlot == null || !weaponSlot.IsEquipped || !weaponSlot.HasCharges())
                {
                    continue;
                }

                Definition_Weapon weapon = weaponSlot.Weapon;
                if (weapon == null)
                {
                    continue;
                }

                // Get weapon range
                // TODO: Once Definition_Weapon.Range property is added, this helper can be removed
                int weaponRange = GetWeaponRange(weapon);

                // Check if enemy is within weapon range from candidate tile
                // Requirement 22.2
                if (distance <= weaponRange)
                {
                    hasWeaponInRange = true;

                    // Calculate expected damage from this weapon
                    float expectedDamage = CalculateExpectedDamage(weapon);

                    if (expectedDamage > bestExpectedDamage)
                    {
                        bestExpectedDamage = expectedDamage;
                    }
                }
            }
        }

        // If no weapon can reach the enemy from this tile, return 0
        if (!hasWeaponInRange || bestExpectedDamage <= 0f)
        {
            return 0f;
        }

        // Requirement 22.3: Add kill bonus if expected damage >= enemy currentHealth
        if (bestExpectedDamage >= enemy.CurrentHealth)
        {
            score += KillBonus;
        }
        else
        {
            // Requirement 22.4: Add damage score (expectedDamage / maxHP * 40)
            float damageRatio = bestExpectedDamage / unit.MaxHealth;
            score += damageRatio * MaxDamageScore;
        }

        // Requirement 22.5: Add target weakness weight for low-HP enemies
        float enemyHpFraction = (float)enemy.CurrentHealth / enemy.MaxHealth;
        if (enemyHpFraction < 0.5f)
        {
            // More vulnerable enemies get higher weight
            float weaknessBonus = (1f - enemyHpFraction) * TargetWeaknessWeight;
            score += weaknessBonus;
        }

        // Requirement 22.6: Reduce score if target is already claimed
        if (claimedTargets != null && claimedTargets.Contains(enemy))
        {
            score -= AlreadyClaimedPenalty;
        }

        // Requirement 22.7: Scale by FleetStance: Advance * 1.5, Retreat * 0.3
        score *= GetAttackMultiplier(snapshot.CurrentStance);

        return score;
    }

    /// <summary>
    /// Gets the effective range of a weapon.
    /// 
    /// TODO: Once Definition_Weapon.Range property is added, replace this method
    /// with direct property access. For now, this provides reasonable defaults
    /// based on weapon type characteristics.
    /// </summary>
    private static int GetWeaponRange(Definition_Weapon weapon)
    {
        if (weapon == null)
        {
            return 0;
        }

        // Temporary heuristic until Definition_Weapon.Range is added:
        // - High-damage weapons tend to be longer range
        // - Use weapon name or type as hints if available
        
        // Calculate average damage as a proxy for weapon classification
        float avgDamage = CalculateExpectedDamage(weapon);
        
        // Rough classification:
        // - High damage (>40): Long range weapons (torpedoes, missiles) - range 8-10
        // - Medium damage (20-40): Medium range (guns, cannons) - range 5-7
        // - Low damage (<20): Short range (close combat) - range 3-4
        
        if (avgDamage > 40f)
        {
            return 9; // Long range
        }
        else if (avgDamage > 20f)
        {
            return 6; // Medium range
        }
        else if (avgDamage > 0f)
        {
            return 4; // Short range
        }
        
        // Default fallback
        return 5;
    }

    /// <summary>
    /// Calculates danger at a tile from all visible enemy weapon ranges and predicted enemy positions.
    /// Returns positive value representing expected incoming damage scaled by unit's max health.
    /// 
    /// For each visible enemy unit:
    /// - Enumerates weapon slots
    /// - Checks if candidate tile is within enemy weapon range
    /// - Adds danger penalty scaled by expected incoming damage
    /// - Aggregates danger from multiple threatening enemies
    /// 
    /// Also includes predicted enemy positions from EnemyMemory in danger calculations.
    /// Danger weight is multiplied by 1.5 when stance is Retreat or DefensiveFallback.
    /// 
    /// Implements Requirements 21.1-21.7
    /// </summary>
    private static float DangerAtTile(
        Vector2Int tile,
        RuntimeUnit unit,
        AITurnContext context,
        GridManager gridManager)
    {
        float totalDanger = 0f;

        // Process danger from visible enemy units (Requirements 21.1-21.5)
        foreach (RuntimeUnit enemy in context.EnemyVisibleUnits)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector2Int enemyPosition = enemy.CurrentTileCoordinate;
            totalDanger += CalculateDangerFromPosition(tile, unit, enemy, enemyPosition, gridManager);
        }

        // Process danger from predicted enemy positions from EnemyMemory (Requirement 21.7)
        List<Vector2Int> predictedPositions = EnemyMemory.GetPredictedPositions(
            context.OwnParticipant,
            context.TurnNumber);

        foreach (Vector2Int predictedPos in predictedPositions)
        {
            // For predicted positions, we use a conservative estimate:
            // Assume the hidden enemy has average threat capability
            // Scale danger from predictions by 0.5 to reflect uncertainty
            float predictedDanger = CalculatePredictedThreatDanger(tile, unit, predictedPos, gridManager);
            totalDanger += predictedDanger * 0.5f;
        }

        return totalDanger;
    }

    /// <summary>
    /// Calculates danger from a specific enemy at a known position.
    /// Enumerates enemy weapon slots and aggregates threat from weapons that can reach the tile.
    /// </summary>
    private static float CalculateDangerFromPosition(
        Vector2Int tile,
        RuntimeUnit unit,
        RuntimeUnit enemy,
        Vector2Int enemyPosition,
        GridManager gridManager)
    {
        float danger = 0f;

        // Requirement 21.1: Enumerate weapon slots for each visible enemy unit
        if (enemy.WeaponSlots == null || enemy.WeaponSlots.Length == 0)
        {
            return 0f; // Enemy has no weapons
        }

        foreach (RuntimeWeaponSlotState weaponSlot in enemy.WeaponSlots)
        {
            // Check if weapon is equipped
            if (weaponSlot == null || weaponSlot.Weapon == null)
            {
                continue;
            }

            // Requirement 21.2: Extract weapon's maximum range
            int weaponRange = weaponSlot.Weapon.Range;

            // Calculate distance from enemy position to candidate tile
            int distance = gridManager.DistanceBetween(enemyPosition, tile);

            // Requirement 21.3: Check if candidate tile is within enemy weapon range
            if (distance <= weaponRange)
            {
                // Requirement 21.4: Calculate expected incoming damage
                float expectedDamage = CalculateExpectedDamage(weaponSlot.Weapon);

                // Scale danger penalty by expected damage relative to unit's max health
                // This normalizes danger across units of different sizes
                float damageRatio = unit.MaxHealth > 0 ? expectedDamage / unit.MaxHealth : 0f;

                // Add danger penalty scaled by damage
                danger += damageRatio * DangerWeight;
            }
        }

        return danger;
    }

    /// <summary>
    /// Calculates danger from a predicted enemy position (from EnemyMemory).
    /// Uses a conservative threat estimate since we don't have visibility on the actual unit.
    /// </summary>
    private static float CalculatePredictedThreatDanger(
        Vector2Int tile,
        RuntimeUnit unit,
        Vector2Int predictedEnemyPos,
        GridManager gridManager)
    {
        // Assume average weapon range of 6 tiles for predicted threats
        const int assumedWeaponRange = 6;

        int distance = gridManager.DistanceBetween(predictedEnemyPos, tile);

        if (distance <= assumedWeaponRange)
        {
            // Assume average damage of 30% of unit's max health
            float assumedDamageRatio = 0.3f;

            // Calculate danger with falloff based on distance
            float rangeFraction = 1f - (distance / (float)assumedWeaponRange);
            float danger = assumedDamageRatio * rangeFraction * DangerWeight;

            return danger;
        }

        return 0f;
    }

    /// <summary>
    /// Calculates expected damage value for a weapon.
    /// 
    /// For weapons using D20 rolls, calculates weighted average across all possible outcomes.
    /// For weapons with fixed outcomes, returns the configured damage value.
    /// 
    /// Note: This is a duplicate of the method in MovementPrioritizer.
    /// Consider extracting to a shared utility class in future refactoring.
    /// </summary>
    private static float CalculateExpectedDamage(Definition_Weapon weapon)
    {
        if (weapon == null)
        {
            return 0f;
        }

        // Handle D20-based weapons
        if (weapon.UsesD20Roll && weapon.D20Outcomes != null && weapon.D20Outcomes.Length > 0)
        {
            float totalDamage = 0f;
            int totalRolls = 0;

            foreach (var d20Outcome in weapon.D20Outcomes)
            {
                if (d20Outcome == null || d20Outcome.Outcome == null)
                {
                    continue;
                }

                // Each d20 outcome covers a range of rolls
                // Calculate how many rolls this outcome represents
                int rollsInRange = (d20Outcome.MaximumRoll - d20Outcome.MinimumRoll) + 1;
                totalRolls += rollsInRange;
                totalDamage += d20Outcome.Outcome.Damage * rollsInRange;
            }

            if (totalRolls > 0)
            {
                return totalDamage / totalRolls;
            }

            return 0f;
        }

        // Handle fixed outcome weapons
        if (weapon.AttackOutcome != null)
        {
            return weapon.AttackOutcome.Damage;
        }

        return 0f;
    }

    /// <summary>
    /// Scores formation cohesion and role-based positioning.
    /// Includes cohesion pull toward fleet centroid, fragility pushback
    /// from threats, and scout forward bonus.
    /// </summary>
    private static float ScoreFormationTerms(
        Vector2Int tile,
        RuntimeUnit unit,
        FleetSnapshot snapshot,
        GridManager gridManager)
    {
        float score = 0f;

        // Cohesion: negative distance to fleet centroid
        float distToFleet = Vector2Int.Distance(tile, snapshot.FleetCentroid);
        float mapDiagonal = GetMapDiagonal(gridManager);
        float cohesion = -(distToFleet / mapDiagonal) * CohesionBaseWeight;

        // Apply stance multiplier
        cohesion *= GetCohesionMultiplier(snapshot, unit);

        score += cohesion;

        // Fragility pushback (for vulnerable units near contacts)
        FleetMember member = FindMember(snapshot, unit);
        if (member != null && member.EffectiveFragility > FragilityThreshold && snapshot.Contacts.Count > 0)
        {
            Vector2Int nearestContact = GetUnitPosition(snapshot.Contacts[0], gridManager);
            float distToContact = Vector2Int.Distance(tile, nearestContact);
            float pushback = -(1f - distToContact / mapDiagonal) * FragilityPushbackWeight;
            score += pushback;
        }

        // Scout forward bonus
        if (member != null && member.Role == UnitRole.Scout &&
            (snapshot.CurrentStance == FleetStance.Hunt || snapshot.CurrentStance == FleetStance.Advance) &&
            snapshot.Contacts.Count > 0)
        {
            Vector2Int nearestContact = GetUnitPosition(snapshot.Contacts[0], gridManager);
            float distToContact = Vector2Int.Distance(tile, nearestContact);
            float forwardBonus = (1f - distToContact / mapDiagonal) * ScoutForwardWeight;
            score += forwardBonus;
        }

        return score;
    }

    /// <summary>
    /// Scores stance-driven directional bias.
    /// Retreat/Fallback bias toward home centroid.
    /// Advance bias toward nearest contact.
    /// </summary>
    private static float ScoreDirectionalBias(
        Vector2Int tile,
        RuntimeUnit unit,
        FleetSnapshot snapshot,
        GridManager gridManager,
        AITurnContext context)
    {
        float score = 0f;

        if (snapshot.CurrentStance == FleetStance.Retreat || snapshot.CurrentStance == FleetStance.DefensiveFallback)
        {
            // Bias toward home centroid
            // Calculates how much closer (or farther) the tile is compared to current position
            // Positive improvement = moving toward home (desired for Retreat/Fallback)
            Vector2Int currentPos = GetUnitPosition(unit, gridManager);
            float currentDistToHome = Vector2Int.Distance(currentPos, snapshot.HomeCentroid);
            float tileDistToHome = Vector2Int.Distance(tile, snapshot.HomeCentroid);
            
            float improvement = currentDistToHome - tileDistToHome;
            float weight = snapshot.CurrentStance == FleetStance.Retreat ? RetreatDirectionalWeight : FallbackDirectionalWeight;
            score += improvement * weight;
        }
        else if (snapshot.CurrentStance == FleetStance.Advance && snapshot.Contacts.Count > 0)
        {
            // Bias toward nearest contact
            // Calculates how much closer the tile is to the enemy compared to current position
            // Positive improvement = moving toward enemy (desired for Advance)
            Vector2Int nearestContact = GetUnitPosition(snapshot.Contacts[0], gridManager);
            Vector2Int currentPos = GetUnitPosition(unit, gridManager);
            
            float currentDistToEnemy = Vector2Int.Distance(currentPos, nearestContact);
            float tileDistToEnemy = Vector2Int.Distance(tile, nearestContact);
            
            float improvement = currentDistToEnemy - tileDistToEnemy;
            score += improvement * AdvanceDirectionalWeight;
        }

        return score;
    }

    // ========================================================================
    // STANCE MULTIPLIERS
    // ========================================================================

    /// <summary>
    /// Returns attack opportunity multiplier for the given stance.
    /// Advance: 1.5x, Retreat: 0.3x, Others: 1.0x
    /// </summary>
    private static float GetAttackMultiplier(FleetStance stance)
    {
        switch (stance)
        {
            case FleetStance.Advance:
                return 1.5f;
            case FleetStance.Retreat:
                return 0.3f;
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Returns danger weight multiplier for the given stance.
    /// Retreat/DefensiveFallback: 1.5x, Others: 1.0x
    /// </summary>
    private static float GetDangerMultiplier(FleetStance stance)
    {
        if (stance == FleetStance.Retreat || stance == FleetStance.DefensiveFallback)
        {
            return 1.5f;
        }
        return 1.0f;
    }

    /// <summary>
    /// Returns cohesion weight multiplier based on stance and unit role.
    /// Retreat/DefensiveFallback: 2.0x (pull together)
    /// Advance/Hunt: 0.5x (allow spread)
    /// Scouts in Hunt: -1.0x (push forward)
    /// Damaged units: 1.5x additional multiplier
    /// </summary>
    private static float GetCohesionMultiplier(FleetSnapshot snapshot, RuntimeUnit unit)
    {
        float multiplier = 1.0f;

        // Stance-based multiplier
        if (snapshot.CurrentStance == FleetStance.Retreat || snapshot.CurrentStance == FleetStance.DefensiveFallback)
        {
            multiplier = 2.0f;
        }
        else if (snapshot.CurrentStance == FleetStance.Advance || snapshot.CurrentStance == FleetStance.Hunt)
        {
            multiplier = 0.5f;
        }

        // Scout in Hunt: push forward (negative cohesion)
        FleetMember member = FindMember(snapshot, unit);
        if (member != null && member.Role == UnitRole.Scout && snapshot.CurrentStance == FleetStance.Hunt)
        {
            multiplier = -1.0f;
        }

        // Damaged units: increase cohesion pull
        if (unit.CurrentHealth < unit.MaxHealth * 0.4f)
        {
            multiplier *= 1.5f;
        }

        return multiplier;
    }

    // ========================================================================
    // UTILITY METHODS
    // ========================================================================

    /// <summary>
    /// Generates candidate tiles within the unit's movement budget that are
    /// placeable (not blocked, within map bounds).
    /// TODO: Integrate with GameMovement.ValidateMovement for accurate pathfinding.
    /// </summary>
    public static List<Vector2Int> GetPlaceableReachableTiles(
        RuntimeUnit unit,
        GameMovement gameMovement,
        GridManager gridManager)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int currentPos = GetUnitPosition(unit, gridManager);

        // TODO: Get actual movement budget from unit definition
        int movementBudget = 5; // Placeholder

        // Generate tiles within Manhattan distance
        for (int dx = -movementBudget; dx <= movementBudget; dx++)
        {
            for (int dy = -movementBudget; dy <= movementBudget; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > movementBudget)
                {
                    continue;
                }

                Vector2Int candidate = new Vector2Int(currentPos.x + dx, currentPos.y + dy);

                // TODO: Validate with GameMovement.ValidateMovement
                // For now, basic bounds check
                if (IsInBounds(candidate, gridManager))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Finds the safest tile from candidates (minimum danger).
    /// Used for flee-to-safety override when unit is critically damaged.
    /// </summary>
    private static Vector2Int FindSafestTile(
        List<Vector2Int> candidates,
        RuntimeUnit unit,
        AITurnContext context,
        GridManager gridManager,
        Dictionary<Vector2Int, float> dangerByTile)
    {
        float minDanger = float.PositiveInfinity;
        Vector2Int safest = GetUnitPosition(unit, gridManager);

        foreach (Vector2Int tile in candidates)
        {
            float danger = dangerByTile.ContainsKey(tile) ? dangerByTile[tile] : 0f;
            if (danger < minDanger)
            {
                minDanger = danger;
                safest = tile;
            }
        }

        return safest;
    }

    /// <summary>
    /// Checks if a unit can attack a target from a given tile.
    /// TODO: Implement actual weapon range checking.
    /// </summary>
    private static bool CanAttackTargetFromTile(
        RuntimeUnit unit,
        Vector2Int tile,
        RuntimeUnit target,
        GridManager gridManager)
    {
        // TODO: Check weapon ranges
        // For now, placeholder: within 8 tiles
        float distance = Vector2Int.Distance(tile, GetUnitPosition(target, gridManager));
        return distance <= 8f;
    }

    /// <summary>
    /// Checks if attacking a target would result in a kill.
    /// TODO: Implement actual damage calculation.
    /// </summary>
    private static bool WouldKillTarget(RuntimeUnit unit, RuntimeUnit target)
    {
        // TODO: Calculate expected damage vs target health
        // For now, placeholder
        return false;
    }

    /// <summary>
    /// Gets the current map position of a unit.
    /// TODO: Query from GameMap once integrated.
    /// </summary>
    private static Vector2Int GetUnitPosition(RuntimeUnit unit, GridManager gridManager)
    {
        // TODO: Get actual position from GameMap
        // For now, return zero as placeholder
        return Vector2Int.zero;
    }

    /// <summary>
    /// Finds the FleetMember entry for a given RuntimeUnit.
    /// Returns null if not found.
    /// </summary>
    private static FleetMember FindMember(FleetSnapshot snapshot, RuntimeUnit unit)
    {
        foreach (FleetMember member in snapshot.Members)
        {
            if (ReferenceEquals(member.Unit, unit))
            {
                return member;
            }
        }
        return null;
    }

    /// <summary>
    /// Calculates map diagonal distance for normalization.
    /// </summary>
    private static float GetMapDiagonal(GridManager gridManager)
    {
        // TODO: Get actual map dimensions
        // For now, placeholder
        return 50f;
    }

    /// <summary>
    /// Checks if a tile is within map bounds.
    /// </summary>
    private static bool IsInBounds(Vector2Int tile, GridManager gridManager)
    {
        // TODO: Use actual GridManager bounds check
        // For now, placeholder
        return tile.x >= 0 && tile.y >= 0 && tile.x < 50 && tile.y < 50;
    }
}
