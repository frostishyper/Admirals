using System.Collections.Generic;
using UnityEngine;


// Authoritative subordinate for RuntimeUnit effects that cross Unit and Map
// ownership boundaries. GameMaster decides when an action may be attempted;
// this class validates and commits the resulting Unit-state transaction.
internal sealed class GameUnitEffects
{
    internal sealed class EndPhaseReport
    {
        public int UnitsProcessed { get; internal set; }
        public int UnitsDestroyed { get; internal set; }
        public int HealthChanged { get; internal set; }
        public int WeaponChargesRestored { get; internal set; }
        public int SystemChargesRestored { get; internal set; }
        public int StatusesExpired { get; internal set; }
        public int ModifiersExpired { get; internal set; }
    }


    private sealed class EndUnitPlan
    {
        public RuntimeUnit Unit;
        public int HealthDelta;
        public int ReplenishmentModifier;
        public readonly List<RuntimeStatusState> ExpiredStatuses =
            new List<RuntimeStatusState>();
        public readonly List<RuntimeModifierState> ExpiredModifiers =
            new List<RuntimeModifierState>();
    }


    private GameMap _GameMap;

    private GameUnits _GameUnits;


    internal bool Initialized
    {
        get;
        private set;
    }


    internal bool Initialize(
        GameMap GameMap,
        GameUnits GameUnits,
        out string ValidationMessage
    )
    {
        Initialized =
            false;


        if (GameMap == null ||
            !GameMap.Initialized ||
            GameUnits == null ||
            !GameUnits.Initialized)
        {
            ValidationMessage =
                "GameUnitEffects requires initialized GameMap and GameUnits state.";

            return false;
        }


        _GameMap =
            GameMap;

        _GameUnits =
            GameUnits;


        Initialized =
            true;

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool ResolveTriggeredMines(
        RuntimeUnit Unit,
        IReadOnlyList<RuntimeMineOccupancy> Mines,
        out bool UnitDestroyed,
        out string ResolutionMessage
    )
    {
        UnitDestroyed = false;
        ResolutionMessage = string.Empty;


        if (!Initialized ||
            Unit == null ||
            Mines == null)
        {
            ResolutionMessage =
                "Mine resolution requires initialized effects, a Unit and Mine records.";
            return false;
        }


        int TotalDamage = 0;


        for (int i = 0; i < Mines.Count; i++)
        {
            RuntimeMineOccupancy Mine = Mines[i];
            SystemMineDeploymentCapability Capability =
                Mine?.SourceSystem?.MineDeployment;


            if (Capability == null ||
                !Capability.Enabled ||
                (Capability.TriggerDomains & Unit.CurrentDomain) == 0)
            {
                continue;
            }


            int Roll = Capability.UsesD20Roll
                ? Random.Range(1, 21)
                : 1;


            if (Capability.TryGetMineOutcome(
                    Roll,
                    out MineOutcome Outcome) &&
                Outcome != null)
            {
                TotalDamage += Mathf.Max(0, Outcome.Damage);
            }
        }


        if (TotalDamage > 0)
        {
            Unit.SetHealth(Unit.CurrentHealth - TotalDamage);
        }


        if (Unit.CurrentHealth <= 0 && Unit.IsActive)
        {
            if (!_GameUnits.TryGetRoster(
                    Unit.Owner,
                    out ParticipantUnitRoster Roster))
            {
                ResolutionMessage =
                    "The damaged Unit has no participant roster.";
                return false;
            }


            if (!_GameMap.TryRemoveUnit(
                    Unit,
                    out string MapMessage))
            {
                ResolutionMessage = MapMessage;
                return false;
            }


            if (!Roster.TryMarkDestroyed(
                    Unit,
                    out _,
                    out string UnitMessage))
            {
                ResolutionMessage = UnitMessage;
                return false;
            }


            UnitDestroyed = true;
        }


        ResolutionMessage =
            $"{Mines.Count} Mine(s) detonated for {TotalDamage} damage.";
        return true;
    }


    internal bool TryApplyTagActions(
        RuntimeUnit Unit,
        IReadOnlyList<GameplayAction> Actions,
        out string ValidationMessage
    )
    {
        if (!Initialized)
        {
            ValidationMessage =
                "GameUnitEffects is not initialized.";

            return false;
        }


        if (Unit == null ||
            !Unit.IsActive)
        {
            ValidationMessage =
                "An active Runtime Unit is required.";

            return false;
        }


        if (!_GameMap.IsUnitOnMap(
                Unit))
        {
            ValidationMessage =
                $"Unit '{Unit.InstanceName}' is not currently on the Map.";

            return false;
        }


        if (Actions == null ||
            Actions.Count == 0)
        {
            ValidationMessage =
                "No Unit tag Actions were supplied.";

            return false;
        }


        HashSet<Definition_UnitTag> CurrentTags =
            new HashSet<Definition_UnitTag>(
                Unit.GetTags()
            );

        HashSet<Definition_UnitTag> ResultingTags =
            new HashSet<Definition_UnitTag>(
                CurrentTags
            );


        for (int i = 0;
            i < Actions.Count;
            i++)
        {
            GameplayAction Action =
                Actions[i];


            if (Action == null ||
                (Action.Type != GameplayActionType.AddTag &&
                 Action.Type != GameplayActionType.RemoveTag))
            {
                ValidationMessage =
                    "GameUnitEffects received an Action that is not a tag Action.";

                return false;
            }


            if (Action.Tag == null)
            {
                ValidationMessage =
                    "GameUnitEffects received a tag Action with no Unit Tag.";

                return false;
            }


            if (!Action.Tag.Validate(
                    out string TagValidationMessage))
            {
                ValidationMessage =
                    $"GameUnitEffects received an invalid Unit Tag: {TagValidationMessage}";

                return false;
            }


            bool Changed =
                Action.Type == GameplayActionType.AddTag
                    ? ResultingTags.Add(
                        Action.Tag
                    )
                    : ResultingTags.Remove(
                        Action.Tag
                    );


            if (!Changed)
            {
                ValidationMessage =
                    Action.Type == GameplayActionType.AddTag
                        ? $"Unit '{Unit.InstanceName}' already has Tag '{Action.Tag.Name}'."
                        : $"Unit '{Unit.InstanceName}' does not have Tag '{Action.Tag.Name}'.";

                return false;
            }
        }


        if (!TryResolveDomainOverride(
                CurrentTags,
                out bool HadDomainOverride,
                out _,
                out ValidationMessage) ||
            !TryResolveDomainOverride(
                ResultingTags,
                out bool HasDomainOverride,
                out Domain OverrideDomain,
                out ValidationMessage))
        {
            return false;
        }


        Domain ResultingDomain =
            HasDomainOverride
                ? OverrideDomain
                : HadDomainOverride
                    ? Unit.Definition.BaseDomain
                    : Unit.CurrentDomain;


        if (!_GameMap.ValidateUnitDomainChange(
                Unit,
                ResultingDomain,
                out ValidationMessage))
        {
            return false;
        }


        // Every fallible check is complete. Tag and Domain state now commit
        // together as one authoritative transaction.
        for (int i = 0;
            i < Actions.Count;
            i++)
        {
            GameplayAction Action =
                Actions[i];


            if (Action.Type ==
                GameplayActionType.AddTag)
            {
                Unit.AddTag(
                    Action.Tag
                );
            }
            else
            {
                Unit.RemoveTag(
                    Action.Tag
                );
            }
        }


        Unit.SetDomain(
            ResultingDomain
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryApplyActions(RuntimeUnit Unit,
        IReadOnlyList<GameplayAction> Actions,
        out bool UnitDestroyed, out string ValidationMessage)
    {
        UnitDestroyed = false;
        ValidationMessage = string.Empty;
        if (!Initialized || Unit == null || !Unit.IsActive ||
            Actions == null)
        {
            ValidationMessage = "Gameplay actions require initialized effects and an active Unit.";
            return false;
        }

        List<GameplayAction> TagActions = new List<GameplayAction>();
        for (int i = 0; i < Actions.Count; i++)
        {
            GameplayAction Action = Actions[i];
            if (Action == null)
                continue;
            switch (Action.Type)
            {
                case GameplayActionType.Damage:
                    Unit.SetHealth(Unit.CurrentHealth - Mathf.Max(0, Action.Amount));
                    break;
                case GameplayActionType.Health:
                    Unit.SetHealth(Unit.CurrentHealth + Mathf.Max(0, Action.Amount));
                    break;
                case GameplayActionType.AddTag:
                case GameplayActionType.RemoveTag:
                    TagActions.Add(Action);
                    break;
                case GameplayActionType.ApplyStatus:
                    Unit.ApplyStatus(Action.Status);
                    break;
                case GameplayActionType.RemoveStatus:
                    Unit.RemoveStatus(Action.Status);
                    break;
                case GameplayActionType.ApplyModifier:
                    Unit.ApplyModifier(Action.Modifier,
                        Action.ModifierDurationMode, Action.ModifierDurationTurns);
                    break;
            }
        }

        if (TagActions.Count > 0 && Unit.IsActive &&
            !TryApplyTagActions(Unit, TagActions, out ValidationMessage))
            return false;

        if (Unit.CurrentHealth <= 0 && Unit.IsActive)
        {
            if (!_GameUnits.TryGetRoster(Unit.Owner,
                    out ParticipantUnitRoster Roster) ||
                !_GameMap.TryRemoveUnit(Unit, out ValidationMessage) ||
                !Roster.TryMarkDestroyed(Unit, out _, out ValidationMessage))
                return false;
            UnitDestroyed = true;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryApplyDamage(RuntimeUnit Unit, int Damage,
        out bool UnitDestroyed, out string ValidationMessage)
    {
        UnitDestroyed = false;
        ValidationMessage = string.Empty;
        if (!Initialized || Unit == null || !Unit.IsActive)
        {
            ValidationMessage = "Damage requires initialized effects and an active Unit.";
            return false;
        }
        Unit.SetHealth(Unit.CurrentHealth - Mathf.Max(0, Damage));
        if (Unit.CurrentHealth <= 0)
        {
            if (!_GameUnits.TryGetRoster(Unit.Owner,
                    out ParticipantUnitRoster Roster) ||
                !_GameMap.TryRemoveUnit(Unit, out ValidationMessage) ||
                !Roster.TryMarkDestroyed(Unit, out _, out ValidationMessage))
                return false;
            UnitDestroyed = true;
        }
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryResolveEndPhase(out EndPhaseReport Report,
        out string ValidationMessage)
    {
        Report = null;
        ValidationMessage = string.Empty;
        if (!Initialized || _GameUnits == null || _GameMap == null)
        {
            ValidationMessage =
                "End-phase effects require initialized Unit and Map state.";
            return false;
        }

        List<RuntimeUnit> ActiveUnits = GetActiveUnits();
        List<EndUnitPlan> Plans = new List<EndUnitPlan>(ActiveUnits.Count);
        for (int i = 0; i < ActiveUnits.Count; i++)
            Plans.Add(BuildEndUnitPlan(ActiveUnits[i], ActiveUnits));

        EndPhaseReport Result = new EndPhaseReport();
        for (int i = 0; i < Plans.Count; i++)
        {
            EndUnitPlan Plan = Plans[i];
            RuntimeUnit Unit = Plan.Unit;
            if (Unit == null || !Unit.IsActive)
                continue;

            Result.UnitsProcessed++;
            int PreviousHealth = Unit.CurrentHealth;
            Unit.SetHealth(Unit.CurrentHealth + Plan.HealthDelta);
            Result.HealthChanged += Unit.CurrentHealth - PreviousHealth;

            RemoveExpiredEffects(Unit, Plan, Result);

            if (Unit.CurrentHealth <= 0)
            {
                if (!_GameUnits.TryGetRoster(Unit.Owner,
                        out ParticipantUnitRoster Roster) ||
                    !_GameMap.TryRemoveUnit(Unit, out ValidationMessage) ||
                    !Roster.TryMarkDestroyed(Unit, out _,
                        out ValidationMessage))
                {
                    return false;
                }

                Result.UnitsDestroyed++;
                continue;
            }

            ReplenishUnit(Unit, Plan.ReplenishmentModifier, Result);
        }

        Report = Result;
        ValidationMessage = string.Empty;
        return true;
    }


    private EndUnitPlan BuildEndUnitPlan(RuntimeUnit Unit,
        IReadOnlyList<RuntimeUnit> ActiveUnits)
    {
        EndUnitPlan Plan = new EndUnitPlan
        {
            Unit = Unit,
            HealthDelta = Mathf.Max(0, GetModifierTotalForEnd(
                Unit, ActiveUnits, ModifierType.HealthPerEndPhase)),
            ReplenishmentModifier = GetModifierTotalForEnd(
                Unit, ActiveUnits, ModifierType.Replenishment)
        };

        for (int i = 0; i < Unit.ActiveStatuses.Count; i++)
        {
            RuntimeStatusState State = Unit.ActiveStatuses[i];
            Definition_Status Status = State?.Status;
            if (Status == null)
                continue;

            State.IntervalProgress++;
            if (State.IntervalProgress >= Mathf.Max(1, Status.IntervalTurns))
            {
                Plan.HealthDelta += Mathf.Max(0, Status.HealthPerInterval);
                Plan.HealthDelta -= Mathf.Max(0, Status.DamagePerInterval);
                State.IntervalProgress = 0;
            }

            if (Status.DurationMode == DurationMode.Turns)
            {
                State.RemainingTurns--;
                if (State.RemainingTurns <= 0)
                    Plan.ExpiredStatuses.Add(State);
            }
        }

        for (int i = 0; i < Unit.ActiveModifiers.Count; i++)
        {
            RuntimeModifierState State = Unit.ActiveModifiers[i];
            if (State != null && State.DurationMode == DurationMode.Turns)
            {
                State.RemainingTurns--;
                if (State.RemainingTurns <= 0)
                    Plan.ExpiredModifiers.Add(State);
            }
        }

        return Plan;
    }


    private static void RemoveExpiredEffects(RuntimeUnit Unit,
        EndUnitPlan Plan, EndPhaseReport Report)
    {
        for (int i = 0; i < Plan.ExpiredStatuses.Count; i++)
        {
            RuntimeStatusState State = Plan.ExpiredStatuses[i];
            if (State?.Status == null)
                continue;
            Unit.RemoveStatus(State.Status);
            Report.StatusesExpired++;
        }

        for (int i = 0; i < Plan.ExpiredModifiers.Count; i++)
        {
            Unit.RemoveModifier(Plan.ExpiredModifiers[i]);
            Report.ModifiersExpired++;
        }
    }


    private static void ReplenishUnit(RuntimeUnit Unit,
        int ReplenishmentModifier, EndPhaseReport Report)
    {
        for (int i = 0; i < Unit.WeaponSlots.Length; i++)
        {
            RuntimeWeaponSlotState Slot = Unit.WeaponSlots[i];
            Definition_Weapon Weapon = Slot?.Weapon;
            if (Slot == null || Weapon == null)
                continue;
            if (Slot.CurrentCharges >= Slot.MaxCharges ||
                Weapon.ReplenishRate < 1 || Weapon.ReplenishInterval < 1)
            {
                Slot.ResetReplenishmentProgress();
                continue;
            }

            Slot.AdvanceReplenishmentProgress();
            if (Slot.ReplenishmentProgress >= Weapon.ReplenishInterval)
            {
                Report.WeaponChargesRestored += Slot.ReplenishCharges(
                    Mathf.Max(0, Weapon.ReplenishRate + ReplenishmentModifier));
                Slot.ResetReplenishmentProgress();
            }
        }

        for (int i = 0; i < Unit.SystemSlots.Length; i++)
        {
            RuntimeSystemSlotState Slot = Unit.SystemSlots[i];
            Definition_System System = Slot?.System;
            if (Slot == null || System == null || !Slot.UsesCharges)
                continue;
            if (Slot.CurrentCharges >= Slot.MaxCharges ||
                System.ReplenishRate < 1 || System.ReplenishInterval < 1)
            {
                Slot.ResetReplenishmentProgress();
                continue;
            }

            Slot.AdvanceReplenishmentProgress();
            if (Slot.ReplenishmentProgress >= System.ReplenishInterval)
            {
                Report.SystemChargesRestored += Slot.ReplenishCharges(
                    Mathf.Max(0, System.ReplenishRate + ReplenishmentModifier));
                Slot.ResetReplenishmentProgress();
            }
        }
    }


    private List<RuntimeUnit> GetActiveUnits()
    {
        List<RuntimeUnit> Result = new List<RuntimeUnit>();
        AddActiveUnits(_GameUnits.ParticipantA, Result);
        AddActiveUnits(_GameUnits.ParticipantB, Result);
        return Result;
    }


    private static void AddActiveUnits(ParticipantUnitRoster Roster,
        List<RuntimeUnit> Result)
    {
        if (Roster == null)
            return;
        IReadOnlyList<RuntimeUnit> Units = Roster.RuntimeUnits;
        for (int i = 0; i < Units.Count; i++)
            if (Units[i] != null && Units[i].IsActive)
                Result.Add(Units[i]);
    }


    private int GetModifierTotalForEnd(RuntimeUnit Recipient,
        IReadOnlyList<RuntimeUnit> ActiveUnits, ModifierType Type)
    {
        int Total = 0;
        for (int i = 0; i < Recipient.ActiveModifiers.Count; i++)
            Total += GetEndModifierValue(
                Recipient.ActiveModifiers[i]?.Modifier, Type);

        for (int i = 0; i < Recipient.ActiveStatuses.Count; i++)
        {
            GameplayModifier[] Modifiers =
                Recipient.ActiveStatuses[i]?.Status?.Modifiers;
            if (Modifiers == null)
                continue;
            for (int m = 0; m < Modifiers.Length; m++)
                Total += GetEndModifierValue(Modifiers[m], Type);
        }

        for (int i = 0; i < ActiveUnits.Count; i++)
        {
            RuntimeUnit Source = ActiveUnits[i];
            RuntimeSystemSlotState[] Slots = Source.SystemSlots;
            for (int s = 0; s < Slots.Length; s++)
            {
                Definition_System System = Slots[s]?.System;
                SystemPassiveCapability Passive = System?.Passive;
                if (System == null || Passive == null || !Passive.Enabled ||
                    (System.OperationalPhases & GameplayPhase.End) == 0 ||
                    (System.Domains & Recipient.CurrentDomain) == 0 ||
                    !PassiveCanTarget(Source, Recipient, Passive.Targets) ||
                    (!ReferenceEquals(Source, Recipient) &&
                     !DoesPatternReach(Source, Recipient, System.Pattern)))
                    continue;

                GameplayModifier[] Modifiers = Passive.Modifiers;
                for (int m = 0; m < Modifiers.Length; m++)
                    Total += GetEndModifierValue(Modifiers[m], Type);
            }
        }

        return Total;
    }


    private static int GetEndModifierValue(GameplayModifier Modifier,
        ModifierType Type)
    {
        return Modifier != null && Modifier.Type == Type &&
            (Modifier.ApplicablePhases & GameplayPhase.End) != 0
                ? Modifier.Value
                : 0;
    }


    private static bool PassiveCanTarget(RuntimeUnit Source,
        RuntimeUnit Recipient, GameplayTarget Targets)
    {
        if (ReferenceEquals(Source, Recipient))
            return (Targets & GameplayTarget.Self) != 0;
        return ReferenceEquals(Source.Owner, Recipient.Owner)
            ? (Targets & GameplayTarget.Ally) != 0
            : (Targets & GameplayTarget.Enemy) != 0;
    }


    private bool DoesPatternReach(RuntimeUnit Source, RuntimeUnit Recipient,
        Definition_Pattern Pattern)
    {
        if (Pattern == null || Pattern.Mode != PatternMode.OriginPoint ||
            !_GameMap.TryGetUnitMovementPivotCoordinate(Source,
                out Vector2Int Pivot) ||
            !_GameMap.TryGetUnitCoordinates(Recipient,
                out Vector2Int[] Footprint))
            return false;

        Vector2Int OriginOffset = Source.Definition.OriginPoint -
            Source.Definition.MovementPivot;
        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                OriginOffset, Source.Facing, out Vector2Int RotatedOrigin))
            return false;

        PatternLayer[] Layers = Pattern.Layers;
        for (int l = 0; l < Layers.Length; l++)
        {
            Vector2Int[] Offsets = Layers[l]?.Offsets;
            if (Offsets == null)
                continue;
            for (int o = 0; o < Offsets.Length; o++)
            {
                if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Offsets[o], Source.Facing, out Vector2Int Rotated))
                    continue;
                Vector2Int Coordinate = Pivot + RotatedOrigin + Rotated;
                for (int f = 0; f < Footprint.Length; f++)
                    if (Footprint[f] == Coordinate)
                        return true;
            }
        }

        return false;
    }


    private bool TryResolveDomainOverride(
        HashSet<Definition_UnitTag> Tags,
        out bool HasOverride,
        out Domain OverrideDomain,
        out string ValidationMessage
    )
    {
        HasOverride =
            false;

        OverrideDomain =
            Domain.None;


        foreach (Definition_UnitTag Tag
            in Tags)
        {
            if (Tag == null ||
                !Tag.OverridesDomain)
            {
                continue;
            }


            Domain TagDomain =
                Tag.DomainOverride;


            if (HasOverride &&
                OverrideDomain !=
                    TagDomain)
            {
                ValidationMessage =
                    "The resulting Unit Tags contain conflicting Domain overrides.";

                return false;
            }


            OverrideDomain =
                TagDomain;

            HasOverride =
                true;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}
