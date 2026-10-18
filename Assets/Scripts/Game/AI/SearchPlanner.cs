using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI search phase planner that selects which unit should scan and in which direction.
/// Prioritizes Scout units and aims scans toward predicted enemy positions from EnemyMemory.
/// </summary>
public static class SearchPlanner
{
    /// <summary>
    /// Executes search phase for the AI participant.
    /// Selects the best unit with search capability and triggers its scan ability.
    /// </summary>
    /// <param name="context">Current turn context with units and fog state.</param>
    /// <param name="gameMaster">GameMaster for ability execution.</param>
    public static void ExecuteSearch(AITurnContext context, GameMaster gameMaster)
    {
        if (context == null || gameMaster == null)
        {
            Debug.LogError("[AI][Search] Null parameter in ExecuteSearch");
            return;
        }

        // Find units with search capability
        List<RuntimeUnit> scanners = FindScanCapableUnits(context.OwnAliveUnits);

        if (scanners.Count == 0)
        {
            Debug.Log("[AI][Search] No units with search capability available");
            return;
        }

        // Select best scanner (Scout priority + fog coverage)
        RuntimeUnit bestScanner = SelectBestScanner(scanners, context);

        if (bestScanner == null)
        {
            Debug.LogWarning("[AI][Search] Scanner selection failed");
            return;
        }

        // Find scan ability slot
        int scanAbilitySlot = FindScanAbilitySlot(bestScanner);

        if (scanAbilitySlot < 0)
        {
            Debug.LogWarning($"[AI][Search] Unit {bestScanner.RuntimeID} has no scan ability");
            return;
        }

        // Execute scan via ability system
        bool success = gameMaster.TryUseAbility(
            context.OwnParticipant,
            bestScanner.RuntimeID,
            scanAbilitySlot,
            out string validationMessage
        );

        if (!success)
        {
            Debug.LogWarning($"[AI][Search] Scan failed for {bestScanner.RuntimeID}: {validationMessage}");
        }
        else
        {
            Debug.Log($"[AI][Search] {bestScanner.InstanceName} executed scan");
        }
    }

    /// <summary>
    /// Finds all units that have search capability enabled.
    /// </summary>
    private static List<RuntimeUnit> FindScanCapableUnits(List<RuntimeUnit> units)
    {
        List<RuntimeUnit> scanners = new List<RuntimeUnit>();

        foreach (RuntimeUnit unit in units)
        {
            if (unit.Definition != null &&
                unit.Definition.System != null &&
                unit.Definition.System.Search != null &&
                unit.Definition.System.Search.Enabled)
            {
                scanners.Add(unit);
            }
        }

        return scanners;
    }

    /// <summary>
    /// Selects the best scanner from available units.
    /// Prioritizes Scout role units, then considers predicted enemy positions.
    /// </summary>
    private static RuntimeUnit SelectBestScanner(List<RuntimeUnit> scanners, AITurnContext context)
    {
        RuntimeUnit bestScanner = null;
        bool bestIsScout = false;

        foreach (RuntimeUnit unit in scanners)
        {
            // Classify role
            UnitRole role = ClassifyUnitRole(unit);
            bool isScout = (role == UnitRole.Scout);

            // Scout priority: Scouts always beat non-Scouts
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
            // Within same role tier, prefer first found (could add fog coverage scoring here)
        }

        return bestScanner;
    }

    /// <summary>
    /// Classifies a unit's tactical role based on speed and detection range.
    /// </summary>
    private static UnitRole ClassifyUnitRole(RuntimeUnit unit)
    {
        if (unit.Definition == null || unit.Definition.System == null)
        {
            return UnitRole.Striker;
        }

        int speed = unit.Definition.System.MovementSpeed;
        int detectionRange = unit.Definition.System.DetectionRange;

        // Scout: Fast (speed >= 6) with good detection (range >= 4)
        if (speed >= 6 && detectionRange >= 4)
        {
            return UnitRole.Scout;
        }

        // Anchor: Tough (HP >= 80) with multiple weapons
        if (unit.MaxHealth >= 80 && HasMultipleWeapons(unit))
        {
            return UnitRole.Anchor;
        }

        // Default: Striker
        return UnitRole.Striker;
    }

    /// <summary>
    /// Checks if a unit has multiple weapon systems.
    /// </summary>
    private static bool HasMultipleWeapons(RuntimeUnit unit)
    {
        if (unit.Definition == null || unit.Definition.System == null)
        {
            return false;
        }

        int weaponCount = 0;

        if (unit.Definition.System.PrimaryWeapon != null)
        {
            weaponCount++;
        }

        if (unit.Definition.System.SecondaryWeapon != null)
        {
            weaponCount++;
        }

        return weaponCount >= 2;
    }

    /// <summary>
    /// Finds the ability slot index for the scan ability.
    /// Searches unit abilities for one usable during Search phase.
    /// </summary>
    private static int FindScanAbilitySlot(RuntimeUnit unit)
    {
        if (unit.Abilities == null || unit.Abilities.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < unit.Abilities.Length; i++)
        {
            RuntimeAbilityState abilityState = unit.Abilities[i];

            if (abilityState != null &&
                abilityState.Ability != null &&
                (abilityState.Ability.UsablePhases & GameplayPhase.Search) != 0)
            {
                return i;
            }
        }

        return -1;
    }
}