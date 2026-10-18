using System.Collections.Generic;
using UnityEngine;


// Authoritative subordinate for RuntimeUnit effects that cross Unit and Map
// ownership boundaries. GameMaster decides when an action may be attempted;
// this class validates and commits the resulting Unit-state transaction.
internal sealed class GameUnitEffects
{
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
