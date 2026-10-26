using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PreBattleDecider
{
    public static GameParticipantAPI.ParticipantCommand DecidePreBattle(
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        FleetStance currentStance)
    {
        if (snapshot?.PreBattle == null || !snapshot.PreBattle.CanAct)
            return null;

        if (snapshot.PreBattle.ActionsCommitted > 0)
            return GameParticipantAPI.ParticipantCommand.EndPreBattle(snapshot);

        List<PreBattleAction> actions = DiscoverPreBattleActions(snapshot, access, currentStance);

        if (actions.Count == 0)
            return GameParticipantAPI.ParticipantCommand.EndPreBattle(snapshot);

        return SelectAndValidateAction(actions, snapshot, access);
    }

    private static List<PreBattleAction> DiscoverPreBattleActions(
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        FleetStance currentStance)
    {
        List<PreBattleAction> actions = new List<PreBattleAction>();

        foreach (ParticipantUnitSnapshot unit in snapshot.OwnUnits)
        {
            if (unit == null || !unit.IsOnMap || unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            actions.AddRange(DiscoverMineDeployments(unit, snapshot, access, currentStance));
            actions.AddRange(DiscoverCarriedUnitDeployments(unit, snapshot, access, currentStance));
        }

        return actions;
    }

    private static List<PreBattleAction> DiscoverMineDeployments(
        ParticipantUnitSnapshot unit,
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        FleetStance currentStance)
    {
        List<PreBattleAction> actions = new List<PreBattleAction>();

        for (int slotIdx = 0; slotIdx < unit.Systems.Length; slotIdx++)
        {
            ParticipantSystemSnapshot system = unit.Systems[slotIdx];
            if (system?.Definition == null ||
                (system.Definition.OperationalPhases & GameplayPhase.PreBattle) == 0 ||
                (system.Definition.UsesCharges && system.CurrentCharges <= 0))
                continue;

            if (!access.TryGetMineDeploymentArea(unit.RuntimeUnitID, slotIdx, out Vector2Int[] area, out _) || area.Length == 0)
                continue;

            foreach (Vector2Int coord in area.Take(5))
            {
                float score = PreBattleScorer.ScoreMineDeployment(coord, unit, snapshot, currentStance);
                actions.Add(new PreBattleAction
                {
                    ActionType = PreBattleActionType.DeployMine,
                    RuntimeUnitID = unit.RuntimeUnitID,
                    SystemSlotIndex = slotIdx,
                    TargetCoordinate = coord,
                    StrategyScore = score
                });
            }
        }

        return actions;
    }

    private static List<PreBattleAction> DiscoverCarriedUnitDeployments(
        ParticipantUnitSnapshot unit,
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        FleetStance currentStance)
    {
        List<PreBattleAction> actions = new List<PreBattleAction>();

        for (int slotIdx = 0; slotIdx < unit.Systems.Length; slotIdx++)
        {
            ParticipantSystemSnapshot system = unit.Systems[slotIdx];
            if (system?.CarriedUnits == null ||
                (system.Definition.OperationalPhases & GameplayPhase.PreBattle) == 0)
                continue;

            for (int carriedIdx = 0; carriedIdx < system.CarriedUnits.Length; carriedIdx++)
            {
                ParticipantCarriedUnitSnapshot carried = system.CarriedUnits[carriedIdx];
                if (carried?.State != RuntimeCarriedUnitState.Carried)
                    continue;

                UnitFacing[] facings = { UnitFacing.North, UnitFacing.East, UnitFacing.South, UnitFacing.West };
                foreach (UnitFacing facing in facings.Take(2))
                {
                    if (!access.TryGetUnitDeploymentArea(unit.RuntimeUnitID, slotIdx, carriedIdx, facing, out Vector2Int[] area, out _) || area.Length == 0)
                        continue;

                    foreach (Vector2Int coord in area.Take(3))
                    {
                        float score = PreBattleScorer.ScoreCarriedUnitDeployment(coord, facing, unit, carried, snapshot, currentStance);
                        actions.Add(new PreBattleAction
                        {
                            ActionType = PreBattleActionType.DeployCarriedUnit,
                            RuntimeUnitID = unit.RuntimeUnitID,
                            SystemSlotIndex = slotIdx,
                            CarriedUnitIndex = carriedIdx,
                            TargetCoordinate = coord,
                            TargetFacing = facing,
                            StrategyScore = score
                        });
                    }
                }
            }
        }

        return actions;
    }

    private static GameParticipantAPI.ParticipantCommand SelectAndValidateAction(
        List<PreBattleAction> actions,
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access)
    {
        actions.Sort((a, b) => b.StrategyScore.CompareTo(a.StrategyScore));

        foreach (PreBattleAction action in actions)
        {
            bool isValid = action.ActionType == PreBattleActionType.DeployMine
                ? access.ValidateMineDeployment(action.RuntimeUnitID, action.SystemSlotIndex, action.TargetCoordinate, out _)
                : access.ValidateUnitDeployment(action.RuntimeUnitID, action.SystemSlotIndex, action.CarriedUnitIndex, action.TargetCoordinate, action.TargetFacing, out _);

            if (!isValid) continue;

            return action.ActionType == PreBattleActionType.DeployMine
                ? GameParticipantAPI.ParticipantCommand.DeployMine(snapshot, action.RuntimeUnitID, action.SystemSlotIndex, action.TargetCoordinate)
                : GameParticipantAPI.ParticipantCommand.DeployCarriedUnit(snapshot, action.RuntimeUnitID, action.SystemSlotIndex, action.CarriedUnitIndex, action.TargetCoordinate, action.TargetFacing);
        }

        return GameParticipantAPI.ParticipantCommand.EndPreBattle(snapshot);
    }
}
