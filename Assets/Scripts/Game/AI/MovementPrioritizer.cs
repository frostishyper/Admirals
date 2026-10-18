using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Ranks units by movement priority when only a subset can move per turn.
/// Combines role classification, tactical opportunity, threat assessment, and stance
/// to ensure the most valuable movements happen first within the movement limit.
/// 
/// **Validates: Requirements 2.1-2.8, 15.1, 30.5**
/// </summary>
public static class MovementPrioritizer
{
    // Role-based base priority weights (Requirement 2.2)
    private const float RoleWeightScout = 50f;      // Scouts move first for reconnaissance
    private const float RoleWeightStriker = 30f;    // Strikers have medium priority
    private const float RoleWeightAnchor = 20f;     // Anchors move last (slow, defensive)

    // Tactical scoring weights
    private const float ThreatProximityWeight = 40f;        // Bonus for units near enemies (Requirement 2.6)
    private const float AttackOpportunityWeight = 35f;      // Bonus for units that can engage
    private const float DamagedPenalty = -20f;              // Penalty for damaged units (Requirement 2.5)
    private const float DamagedHealthThreshold = 0.4f;      // HP fraction below which units are "damaged"
    private const float BlockedPenalty = -100f;             // Penalty for units with no valid destinations (Requirement 2.4)

    // Stance-specific modifiers
    private const float RetreatDamagedBonus = 60f;          // Damaged units prioritized in Retreat (Requirement 2.5)
    private const float AdvanceScoutBonus = 30f;            // Scouts lead advances (Requirement 2.6)
    private const float AdvanceCloseBonus = 20f;            // Close units prioritized in advances
    private const float AdvanceProximityThreshold = 8f;     // Distance threshold for "close" units

    /// <summary>
    /// Ranks all alive units by movement priority and returns them in descending priority order.
    /// Priority combines role classification, threat proximity, attack opportunities, health status,
    /// and stance-specific modifiers to select the most tactically valuable movements first.
    /// </summary>
    /// <param name="context">Turn context containing unit lists and game state.</param>
    /// <param name="snapshot">Fleet snapshot with strategic state and unit metadata.</param>
    /// <param name="gameMovement">Movement subsystem for validation queries.</param>
    /// <param name="gridManager">Grid manager for spatial calculations.</param>
    /// <returns>List of RuntimeUnits sorted by descending priority (highest priority first).</returns>
    public static List<RuntimeUnit> PrioritizeUnits(
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager)
    {
        if (context == null || snapshot == null || gridManager == null)
        {
            Debug.LogWarning("[AI][MovementPrioritizer] Null argument provided, returning empty list");
            return new List<RuntimeUnit>();
        }

        // Calculate priority for each alive unit
        var prioritizedUnits = new List<(RuntimeUnit unit, float priority)>();

        foreach (RuntimeUnit unit in context.OwnAliveUnits)
        {
            if (unit == null)
                continue;

            float priority = CalculatePriority(unit, context, snapshot, gameMovement, gridManager);
            prioritizedUnits.Add((unit, priority));
        }

        // Sort by priority descending (highest priority first) (Requirement 2.7)
        prioritizedUnits.Sort((a, b) => b.priority.CompareTo(a.priority));

        // Blockage detection and priority recomputation (Requirement 2.4, 23.1-23.5)
        // Check each high-priority unit has at least one reachable tile
        var finalList = new List<(RuntimeUnit unit, float priority)>();
        var blockedUnits = new List<(RuntimeUnit unit, float priority)>();

        foreach (var entry in prioritizedUnits)
        {
            bool isBlocked = IsUnitBlocked(entry.unit, gameMovement, gridManager);
            
            if (isBlocked)
            {
                // Apply blocked penalty and move to blocked list
                float blockedPriority = entry.priority + BlockedPenalty;
                blockedUnits.Add((entry.unit, blockedPriority));
            }
            else
            {
                // Unit has valid destinations, keep in main list
                finalList.Add(entry);
            }
        }

        // Append blocked units at the end of the priority list (Requirement 23.2, 23.3)
        finalList.AddRange(blockedUnits);

        // Extract sorted unit list (Requirement 2.8)
        List<RuntimeUnit> result = finalList.Select(x => x.unit).ToList();

        return result;
    }

    /// <summary>
    /// Calculates the movement priority score for a single unit.
    /// Combines multiple factors: role weight, threat proximity, attack opportunity, health status, and stance modifiers.
    /// </summary>
    private static float CalculatePriority(
        RuntimeUnit unit,
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager)
    {
        float priority = 0f;

        // Find the unit's role from the snapshot
        UnitRole role = GetUnitRole(unit, snapshot);

        // 1. Role-based base weight (Requirement 2.2)
        priority += GetRoleWeight(role);

        // 2. Threat proximity scoring (Requirement 2.6)
        priority += CalculateThreatProximity(unit, snapshot, gridManager);

        // 3. Attack opportunity scoring (Requirement 2.3)
        priority += CalculateAttackOpportunity(unit, context, gridManager);

        // 4. Health factor - penalty for damaged units (Requirement 2.5)
        priority += CalculateHealthFactor(unit);

        // 5. Stance-specific modifiers (Requirement 2.5, 2.6)
        priority += GetStanceModifier(unit, role, snapshot, gridManager);

        return priority;
    }

    /// <summary>
    /// Returns the base priority weight for a unit's tactical role.
    /// Scouts receive highest priority for reconnaissance, Anchors lowest for defensive positioning.
    /// </summary>
    private static float GetRoleWeight(UnitRole role)
    {
        switch (role)
        {
            case UnitRole.Scout:
                return RoleWeightScout;     // 50 - scouts move first
            case UnitRole.Striker:
                return RoleWeightStriker;   // 30 - medium priority
            case UnitRole.Anchor:
                return RoleWeightAnchor;    // 20 - anchors move last
            default:
                return RoleWeightStriker;   // default to medium priority
        }
    }

    /// <summary>
    /// Calculates threat proximity score based on distance to nearest visible enemy contact.
    /// Closer units to enemies receive higher priority for engagement.
    /// Formula: (1 - distance/diagonal) * 40, max 40 points
    /// </summary>
    private static float CalculateThreatProximity(RuntimeUnit unit, FleetSnapshot snapshot, GridManager gridManager)
    {
        if (snapshot.Contacts == null || snapshot.Contacts.Count == 0)
        {
            return 0f; // No contacts, no proximity bonus
        }

        // Find nearest contact distance
        Vector2Int unitPos = unit.CurrentTileCoordinate;
        float minDistance = float.MaxValue;

        foreach (RuntimeUnit contact in snapshot.Contacts)
        {
            if (contact == null)
                continue;

            int distance = gridManager.DistanceBetween(unitPos, contact.CurrentTileCoordinate);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        if (minDistance >= float.MaxValue)
        {
            return 0f; // No valid contact found
        }

        // Calculate map diagonal for normalization
        float mapDiagonal = Mathf.Sqrt(gridManager.width * gridManager.width + gridManager.height * gridManager.height);

        // Proximity score: 1.0 when adjacent, 0.0 when at opposite corner
        // Formula: (1 - distance/diagonal) * ThreatProximityWeight
        float proximity = 1.0f - Mathf.Clamp01(minDistance / mapDiagonal);
        float score = proximity * ThreatProximityWeight;

        return score;
    }

    /// <summary>
    /// Calculates attack opportunity score based on enemies within weapon range.
    /// Units that can engage visible enemies receive higher priority.
    /// </summary>
    private static float CalculateAttackOpportunity(RuntimeUnit unit, AITurnContext context, GridManager gridManager)
    {
        if (context.EnemyVisibleUnits == null || context.EnemyVisibleUnits.Count == 0)
        {
            return 0f; // No visible enemies
        }

        if (unit.WeaponSlots == null || unit.WeaponSlots.Length == 0)
        {
            return 0f; // No weapons equipped
        }

        float opportunityScore = 0f;
        Vector2Int unitPos = unit.CurrentTileCoordinate;

        // Check each weapon slot
        foreach (var weaponSlot in unit.WeaponSlots)
        {
            if (weaponSlot == null || weaponSlot.Weapon == null)
                continue;

            int weaponRange = weaponSlot.Weapon.Range;

            // Check if any enemy is within this weapon's range
            foreach (RuntimeUnit enemy in context.EnemyVisibleUnits)
            {
                if (enemy == null)
                    continue;

                int distance = gridManager.DistanceBetween(unitPos, enemy.CurrentTileCoordinate);

                if (distance <= weaponRange)
                {
                    // Enemy in range - add opportunity bonus
                    // Scale by expected damage as fraction of attacker's max health
                    float expectedDamage = CalculateExpectedDamage(weaponSlot.Weapon);
                    float damageRatio = unit.MaxHealth > 0 ? expectedDamage / unit.MaxHealth : 0f;
                    opportunityScore += damageRatio * AttackOpportunityWeight;
                    break; // Only count each weapon once (first enemy in range)
                }
            }
        }

        return opportunityScore;
    }

    /// <summary>
    /// Calculates expected damage value for a weapon based on its attack dice.
    /// Returns the mean expected value (dice count * average die value).
    /// </summary>
    private static float CalculateExpectedDamage(Definition_Weapon weapon)
    {
        if (weapon == null || weapon.AttackDice == null)
            return 0f;

        // Expected value = number of dice * average die value
        // For a d20: average = 10.5
        float averageDieValue = 10.5f;
        float expectedDamage = weapon.AttackDice.DiceCount * averageDieValue;

        return expectedDamage;
    }

    /// <summary>
    /// Calculates health factor penalty for damaged units.
    /// Units below 40% health receive -20 penalty (deprioritized unless Retreat stance).
    /// </summary>
    private static float CalculateHealthFactor(RuntimeUnit unit)
    {
        if (unit.MaxHealth <= 0)
            return 0f;

        float hpFraction = (float)unit.CurrentHealth / unit.MaxHealth;

        // Apply penalty if health is below threshold (Requirement 2.5)
        if (hpFraction < DamagedHealthThreshold)
        {
            return DamagedPenalty; // -20 points
        }

        return 0f; // No penalty for healthy units
    }

    /// <summary>
    /// Applies stance-specific priority modifiers based on strategic posture.
    /// Retreat prioritizes damaged units, Advance/Hunt prioritizes scouts and close units.
    /// </summary>
    private static float GetStanceModifier(RuntimeUnit unit, UnitRole role, FleetSnapshot snapshot, GridManager gridManager)
    {
        float modifier = 0f;

        switch (snapshot.CurrentStance)
        {
            case FleetStance.Retreat:
                // Damaged units move first in Retreat (Requirement 2.5)
                float hpFraction = unit.MaxHealth > 0 ? (float)unit.CurrentHealth / unit.MaxHealth : 1f;
                if (hpFraction < DamagedHealthThreshold)
                {
                    modifier += RetreatDamagedBonus; // +60 points - override the -20 penalty
                }
                break;

            case FleetStance.Advance:
            case FleetStance.Hunt:
                // Scouts lead the advance (Requirement 2.6)
                if (role == UnitRole.Scout)
                {
                    modifier += AdvanceScoutBonus; // +30 points
                }
                // Units close to enemies also prioritized
                else if (snapshot.NearestContactDistance >= 0 && snapshot.NearestContactDistance < AdvanceProximityThreshold)
                {
                    modifier += AdvanceCloseBonus; // +20 points
                }
                break;

            case FleetStance.DefensiveFallback:
            case FleetStance.Hold:
            default:
                // No special modifiers for defensive stances
                break;
        }

        return modifier;
    }

    /// <summary>
    /// Checks if a unit is blocked (has no valid reachable destinations).
    /// Tests movement to adjacent tiles in all directions to detect blockage.
    /// Returns true if the unit cannot move to any reachable position.
    /// </summary>
    private static bool IsUnitBlocked(RuntimeUnit unit, GameMovement gameMovement, GridManager gridManager)
    {
        if (unit == null || gameMovement == null || gridManager == null)
        {
            return true; // Treat null inputs as blocked
        }

        // Get unit's current position
        Vector2Int currentPos = unit.CurrentTileCoordinate;
        UnitFacing currentFacing = unit.CurrentFacing;

        // Check if unit has remaining movement budget
        if (unit.RemainingMovementBudget <= 0)
        {
            return true; // No movement budget means blocked
        }

        // Test a sample of adjacent tiles to see if any are reachable
        // We'll check the 4 cardinal directions plus staying in place with rotation
        Vector2Int[] testDirections = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        UnitFacing[] testFacings = new UnitFacing[]
        {
            UnitFacing.Up,
            UnitFacing.Right,
            UnitFacing.Down,
            UnitFacing.Left
        };

        // Try moving to adjacent tiles
        foreach (Vector2Int direction in testDirections)
        {
            Vector2Int testPos = currentPos + direction;

            // Check if position is within grid bounds
            if (testPos.x < 0 || testPos.x >= gridManager.width ||
                testPos.y < 0 || testPos.y >= gridManager.height)
            {
                continue;
            }

            // Try each facing at this position
            foreach (UnitFacing testFacing in testFacings)
            {
                // Skip if this is the current position and facing (not a valid move)
                if (testPos == currentPos && testFacing == currentFacing)
                {
                    continue;
                }

                // Validate movement using GameMovement API
                bool isValid = gameMovement.ValidateMovement(
                    unit.Participant,
                    unit.RuntimeID,
                    testPos,
                    testFacing,
                    out GameMovement.MovementPlan plan,
                    out string validationMessage
                );

                // If any destination is valid and has a route, unit is not blocked
                if (isValid && plan != null && plan.Route != null && plan.Route.Length > 0)
                {
                    return false; // Found at least one reachable destination
                }
            }
        }

        // Also test in-place rotations (staying at current position but changing facing)
        foreach (UnitFacing testFacing in testFacings)
        {
            if (testFacing == currentFacing)
            {
                continue; // Skip current facing
            }

            bool isValid = gameMovement.ValidateMovement(
                unit.Participant,
                unit.RuntimeID,
                currentPos,
                testFacing,
                out GameMovement.MovementPlan plan,
                out string validationMessage
            );

            // In-place rotation might not count as a "move" but still indicates unit is not blocked
            if (isValid && plan != null)
            {
                return false; // Can rotate, so not completely blocked
            }
        }

        // No valid destinations found - unit is blocked (Requirement 23.1)
        Debug.Log($"[AI][MovementPrioritizer] Unit {unit.RuntimeID} is blocked - no valid destinations");
        return true;
    }

    /// <summary>
    /// Extracts the unit's role from the fleet snapshot.
    /// Returns Striker as default if unit is not found in snapshot.
    /// </summary>
    private static UnitRole GetUnitRole(RuntimeUnit unit, FleetSnapshot snapshot)
    {
        if (snapshot.Members == null)
            return UnitRole.Striker;

        foreach (FleetMember member in snapshot.Members)
        {
            if (member.Unit == unit)
            {
                return member.Role;
            }
        }

        // Not found in snapshot - return default
        return UnitRole.Striker;
    }
}
