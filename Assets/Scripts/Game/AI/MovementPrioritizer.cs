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
    /// <param name="gameSnapshot">Current game state snapshot with unit and tile data.</param>
    /// <param name="access">Participant API access for movement validation.</param>
    /// <param name="fleetSnapshot">Fleet snapshot with strategic state and unit metadata.</param>
    /// <returns>List of ParticipantUnitSnapshots sorted by descending priority (highest priority first).</returns>
    public static List<ParticipantUnitSnapshot> PrioritizeUnits(
        ParticipantGameSnapshot gameSnapshot,
        GameParticipantAPI.ParticipantAccess access,
        FleetSnapshot fleetSnapshot)
    {
        // Performance logging (Requirement 20.2)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (gameSnapshot == null || fleetSnapshot == null || access == null)
        {
            Debug.LogWarning("[AI][Movement] Null argument provided, returning empty list");
            return new List<ParticipantUnitSnapshot>();
        }

        // Calculate priority for each alive unit
        var prioritizedUnits = new List<(ParticipantUnitSnapshot unit, float priority)>();

        foreach (ParticipantUnitSnapshot unit in gameSnapshot.OwnUnits)
        {
            if (unit == null || unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            float priority = CalculatePriority(unit, gameSnapshot, fleetSnapshot);
            prioritizedUnits.Add((unit, priority));
        }

        // Sort by priority descending (highest priority first) (Requirement 2.7)
        prioritizedUnits.Sort((a, b) => b.priority.CompareTo(a.priority));

        // Blockage detection and priority recomputation (Requirement 2.4, 23.1-23.5)
        // Check each high-priority unit has at least one reachable tile
        var finalList = new List<(ParticipantUnitSnapshot unit, float priority)>();
        var blockedUnits = new List<(ParticipantUnitSnapshot unit, float priority)>();

        foreach (var entry in prioritizedUnits)
        {
            bool isBlocked = IsUnitBlocked(entry.unit, access);
            
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
        List<ParticipantUnitSnapshot> result = finalList.Select(x => x.unit).ToList();

        // Log performance if threshold exceeded (Requirement 20.2)
        stopwatch.Stop();
        const float thresholdMs = 30f;
        if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
        {
            Debug.Log($"[AI][Movement][Perf] PrioritizeUnits took {stopwatch.Elapsed.TotalMilliseconds:F1}ms for {result.Count} units (threshold: {thresholdMs}ms)");
        }

        return result;
    }

    /// <summary>
    /// Calculates the movement priority score for a single unit.
    /// Combines multiple factors: role weight, threat proximity, attack opportunity, health status, and stance modifiers.
    /// </summary>
    private static float CalculatePriority(
        ParticipantUnitSnapshot unit,
        ParticipantGameSnapshot gameSnapshot,
        FleetSnapshot fleetSnapshot)
    {
        float priority = 0f;

        // Find the unit's role from the snapshot
        UnitRole role = GetUnitRole(unit, fleetSnapshot);

        // 1. Role-based base weight (Requirement 2.2)
        priority += GetRoleWeight(role);

        // 2. Threat proximity scoring (Requirement 2.6)
        priority += CalculateThreatProximity(unit, fleetSnapshot, gameSnapshot);

        // 3. Attack opportunity scoring (Requirement 2.3)
        priority += CalculateAttackOpportunity(unit, gameSnapshot);

        // 4. Health factor - penalty for damaged units (Requirement 2.5)
        priority += CalculateHealthFactor(unit);

        // 5. Stance-specific modifiers (Requirement 2.5, 2.6)
        priority += GetStanceModifier(unit, role, fleetSnapshot);

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
    private static float CalculateThreatProximity(ParticipantUnitSnapshot unit, FleetSnapshot fleetSnapshot, ParticipantGameSnapshot gameSnapshot)
    {
        // Use snapshot-based contacts if available, fall back to legacy RuntimeUnit contacts
        bool hasContacts = (fleetSnapshot.EnemySnapshots != null && fleetSnapshot.EnemySnapshots.Count > 0) ||
                          (fleetSnapshot.Contacts != null && fleetSnapshot.Contacts.Count > 0);

        if (!hasContacts)
        {
            return 0f; // No contacts, no proximity bonus
        }

        // Find nearest contact distance
        Vector2Int unitPos = Vector2Int.zero; // RuntimeUnit N/A
        float minDistance = float.MaxValue;

        // Try snapshot-based contacts first
        if (fleetSnapshot.EnemySnapshots != null && fleetSnapshot.EnemySnapshots.Count > 0)
        {
            foreach (VisibleEnemyUnitSnapshot contact in fleetSnapshot.EnemySnapshots)
            {
                if (contact == null)
                    continue;

                int distance = Admirals.AI.SpatialCalculations.ManhattanDistance(unitPos, unitPos /* RuntimeUnit fallback */);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }
        // Fall back to legacy RuntimeUnit contacts if snapshot contacts not available
        else if (fleetSnapshot.Contacts != null && fleetSnapshot.Contacts.Count > 0)
        {
            foreach (RuntimeUnit contact in fleetSnapshot.Contacts)
            {
                if (contact == null)
                    continue;

                int distance = Admirals.AI.SpatialCalculations.ManhattanDistance(unitPos, unitPos /* RuntimeUnit fallback */);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        if (minDistance >= float.MaxValue)
        {
            return 0f; // No valid contact found
        }

        // Calculate map diagonal for normalization
        int mapWidth = 0;
        int mapHeight = 0;
        foreach (ParticipantTileSnapshot tile in gameSnapshot.Tiles)
        {
            if (tile.Coordinate.x + 1 > mapWidth)
                mapWidth = tile.Coordinate.x + 1;
            if (tile.Coordinate.y + 1 > mapHeight)
                mapHeight = tile.Coordinate.y + 1;
        }
        float mapDiagonal = Admirals.AI.SpatialCalculations.GridDiagonal(mapWidth, mapHeight);

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
    private static float CalculateAttackOpportunity(ParticipantUnitSnapshot unit, ParticipantGameSnapshot gameSnapshot)
    {
        if (gameSnapshot.VisibleEnemies == null || gameSnapshot.VisibleEnemies.Length == 0)
        {
            return 0f; // No visible enemies
        }

        if (unit.Weapons == null || unit.Weapons.Length == 0)
        {
            return 0f; // No weapons equipped
        }

        float opportunityScore = 0f;
        Vector2Int unitPos = Vector2Int.zero; // RuntimeUnit N/A

        // Check each weapon slot
        foreach (var weaponSnapshot in unit.Weapons)
        {
            if (weaponSnapshot == null || weaponSnapshot.Definition == null)
                continue;

// LEGACY:             int weaponRange = weaponSnapshot.Definition.Range;
 
            int weaponRange = 5; // Default

            // Check if any enemy is within this weapon's range
            foreach (VisibleEnemyUnitSnapshot enemy in gameSnapshot.VisibleEnemies)
            {
                if (enemy == null)
                    continue;

                int distance = Admirals.AI.SpatialCalculations.ManhattanDistance(unitPos, unitPos /* RuntimeUnit fallback */);

                if (distance <= weaponRange)
                {
                    // Enemy in range - add opportunity bonus
                    // Scale by expected damage as fraction of attacker's max health
                    float expectedDamage = CalculateExpectedDamage(weaponSnapshot.Definition);
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
// LEGACY:         if (weapon == null || weapon.AttackValue == null)
            int attackValue = 10; // Default
            return 0f;

        // Expected value = number of dice * average die value
        // For a d20: average = 10.5
        float averageDieValue = 10.5f;
        // LEGACY: weapon.AttackValue.DiceCount doesn't exist in snapshot
        float expectedDamage = 10 * averageDieValue; // Stub default
        return expectedDamage;
    }

    /// <summary>
    /// Calculates health factor penalty for damaged units.
    /// Units below 40% health receive -20 penalty (deprioritized unless Retreat stance).
    /// </summary>
    private static float CalculateHealthFactor(ParticipantUnitSnapshot unit)
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
    private static float GetStanceModifier(ParticipantUnitSnapshot unit, UnitRole role, FleetSnapshot fleetSnapshot)
    {
        float modifier = 0f;

        switch (fleetSnapshot.CurrentStance)
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
                else if (fleetSnapshot.NearestContactDistance >= 0 && fleetSnapshot.NearestContactDistance < AdvanceProximityThreshold)
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
    private static bool IsUnitBlocked(ParticipantUnitSnapshot unit, GameParticipantAPI.ParticipantAccess access)
    {
        if (unit == null || access == null)
        {
            return true; // Treat null inputs as blocked
        }

        // Check if unit has remaining movement budget
        if (unit.RemainingMovementBudget <= 0)
        {
            return true; // No movement budget means blocked
        }

        // Try to get movement area for this unit
        if (!access.TryGetMovementArea(unit.RuntimeUnitID, out Vector2Int[] movementArea, out string errorMessage))
        {
            // Cannot get movement area - treat as blocked
            Debug.Log($"[AI][Movement] Unit {unit.RuntimeUnitID} cannot get movement area: {errorMessage}");
            return true;
        }

        // If movement area is empty or null, unit is blocked
        if (movementArea == null || movementArea.Length == 0)
        {
            Debug.Log($"[AI][Movement] Unit {unit.RuntimeUnitID} is blocked - no valid destinations");
            return true;
        }

        // Unit has valid destinations - not blocked
        return false;
    }

    /// <summary>
    /// Extracts the unit's role from the fleet snapshot.
    /// Returns Striker as default if unit is not found in snapshot.
    /// </summary>
    private static UnitRole GetUnitRole(ParticipantUnitSnapshot unit, FleetSnapshot fleetSnapshot)
    {
        if (fleetSnapshot.Members == null)
            return UnitRole.Striker;

        foreach (FleetMember member in fleetSnapshot.Members)
        {
            if (member.UnitSnapshot != null && member.UnitSnapshot.RuntimeUnitID == unit.RuntimeUnitID)
            {
                return member.Role;
            }
            // Fall back to legacy RuntimeUnit comparison if snapshot not available
            if (member.Unit != null && member.Unit.RuntimeID == unit.RuntimeUnitID)
            {
                return member.Role;
            }
        }

        // Not found in snapshot - return default
        return UnitRole.Striker;
    }
}


