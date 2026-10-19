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
    /// Selects the best unit with search capability, chooses optimal facing, and executes scan.
    /// </summary>
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

        // Choose best facing direction
        UnitFacing facing = ChooseBestFacing(bestScanner, context);

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

    /// <summary>
    /// Finds all units that have sensor system slots.
    /// </summary>
    private static List<RuntimeUnit> FindScanCapableUnits(List<RuntimeUnit> units)
    {
        List<RuntimeUnit> scanners = new List<RuntimeUnit>();

        foreach (RuntimeUnit unit in units)
        {
            if (FindSensorSlot(unit) >= 0)
            {
                scanners.Add(unit);
            }
        }

        return scanners;
    }

    /// <summary>
    /// Selects the best scanner from available units.
    /// Prioritizes Scout role units.
    /// </summary>
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

    /// <summary>
    /// Chooses the best facing direction for a scan.
    /// Prioritizes directions toward predicted enemy positions from EnemyMemory.
    /// </summary>
    private static UnitFacing ChooseBestFacing(RuntimeUnit scanner, AITurnContext context)
    {
        // Get scanner position
        if (!context.GameMap.TryGetUnitCoordinates(scanner, out Vector2Int[] coords) || coords.Length == 0)
        {
            return UnitFacing.North; // Default
        }

        Vector2Int scannerPos = coords[0];

        // Get predicted enemy positions from EnemyMemory
        List<Vector2Int> predictions = EnemyMemory.GetPredictedPositions(
            context.OwnParticipant,
            context.TurnNumber
        );

        // If we have predictions, face toward nearest one
        if (predictions.Count > 0)
        {
            Vector2Int nearest = predictions[0];
            float minDist = Vector2Int.Distance(scannerPos, predictions[0]);

            for (int i = 1; i < predictions.Count; i++)
            {
                float dist = Vector2Int.Distance(scannerPos, predictions[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = predictions[i];
                }
            }

            // Convert direction to facing
            Vector2Int direction = nearest - scannerPos;
            return DirectionToFacing(direction);
        }

        // No predictions - face toward map center
        Vector2Int mapCenter = new Vector2Int(
            context.GridManager.width / 2,
            context.GridManager.height / 2
        );

        Vector2Int toCenter = mapCenter - scannerPos;
        return DirectionToFacing(toCenter);
    }

    /// <summary>
    /// Converts a direction vector to the nearest cardinal UnitFacing.
    /// </summary>
    private static UnitFacing DirectionToFacing(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return UnitFacing.North;
        }

        // Use absolute values to find dominant axis
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

        if (speed >= 6 && detectionRange >= 4)
        {
            return UnitRole.Scout;
        }

        if (unit.MaxHealth >= 80 && HasMultipleWeapons(unit))
        {
            return UnitRole.Anchor;
        }

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
    /// Finds the sensor system slot index on a unit.
    /// Searches for a Sensor-type system slot.
    /// </summary>
    private static int FindSensorSlot(RuntimeUnit unit)
    {
        if (unit.SystemSlots == null || unit.SystemSlots.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < unit.SystemSlots.Length; i++)
        {
            RuntimeSystemSlotState slot = unit.SystemSlots[i];

            if (slot != null &&
                slot.System != null &&
                slot.System.SlotType == SystemSlotType.Sensor &&
                slot.System.Pattern != null)
            {
                return i;
            }
        }

        return -1;
    }
}