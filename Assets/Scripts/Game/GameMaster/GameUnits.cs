using System;
using System.Collections.Generic;
using UnityEngine;


// Four cardinal Unit orientations.
public enum UnitFacing
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}


public enum RuntimeUnitLifeState
{
    Active = 0,
    Destroyed = 1
}


public enum RuntimeCarriedUnitState
{
    Carried = 0,
    Deployed = 1,
    Lost = 2
}


// Runtime state for one specific authored Weapon Slot.
//
// Two slots containing the same Definition_Weapon are still
// completely independent runtime slots.
public sealed class RuntimeWeaponSlotState
{
    public int SlotIndex
    {
        get;
        private set;
    }


    public UnitWeaponSlot AuthoredSlot
    {
        get;
        private set;
    }


    public Definition_Weapon Weapon
    {
        get
        {
            return
                AuthoredSlot != null
                    ? AuthoredSlot.Weapon
                    : null;
        }
    }


    public int MaxCharges
    {
        get;
        private set;
    }


    public int CurrentCharges
    {
        get;
        private set;
    }


    // Number of completed turns accumulated toward the
    // Weapon's next replenishment interval.
    public int ReplenishmentProgress
    {
        get;
        private set;
    }


    public bool IsEquipped
    {
        get
        {
            return
                Weapon != null;
        }
    }


    internal RuntimeWeaponSlotState(
        int SlotIndex,
        UnitWeaponSlot AuthoredSlot
    )
    {
        this.SlotIndex =
            SlotIndex;

        this.AuthoredSlot =
            AuthoredSlot;


        MaxCharges =
            AuthoredSlot != null
                ? Mathf.Max(
                    0,
                    AuthoredSlot.MaxCharges
                )
                : 0;


        CurrentCharges =
            Weapon != null
                ? MaxCharges
                : 0;


        ReplenishmentProgress =
            0;
    }


    public bool HasCharges(
        int Amount = 1
    )
    {
        return
            Amount > 0 &&
            CurrentCharges >= Amount;
    }


    internal bool TrySpendCharges(
        int Amount
    )
    {
        if (!IsEquipped ||
            Amount < 1 ||
            CurrentCharges < Amount)
        {
            return false;
        }


        CurrentCharges -=
            Amount;


        return true;
    }


    internal int ReplenishCharges(
        int Amount
    )
    {
        if (!IsEquipped ||
            Amount < 1)
        {
            return 0;
        }


        int PreviousCharges =
            CurrentCharges;


        CurrentCharges =
            Mathf.Min(
                MaxCharges,
                CurrentCharges + Amount
            );


        return
            CurrentCharges -
            PreviousCharges;
    }


    internal void AdvanceReplenishmentProgress()
    {
        ReplenishmentProgress++;
    }


    internal void ResetReplenishmentProgress()
    {
        ReplenishmentProgress =
            0;
    }
}


// One carried Unit that physically exists aboard a carrier
// but is not yet a RuntimeUnit on the board.
//
// A Count of 3 in authored data becomes three separate records here.
public sealed class RuntimeCarriedUnit
{
    public RuntimeUnit Carrier
    {
        get;
        private set;
    }


    public Definition_Unit Unit
    {
        get;
        private set;
    }


    public int SourceSystemSlotIndex
    {
        get;
        private set;
    }


    public int SourceLoadoutIndex
    {
        get;
        private set;
    }


    public int SourceLoadoutSequence
    {
        get;
        private set;
    }


    public RuntimeCarriedUnitState State
    {
        get;
        private set;
    }


    public RuntimeUnit DeployedUnit
    {
        get;
        private set;
    }


    internal RuntimeCarriedUnit(
        RuntimeUnit Carrier,
        Definition_Unit Unit,
        int SourceSystemSlotIndex,
        int SourceLoadoutIndex,
        int SourceLoadoutSequence
    )
    {
        this.Carrier =
            Carrier;

        this.Unit =
            Unit;

        this.SourceSystemSlotIndex =
            SourceSystemSlotIndex;

        this.SourceLoadoutIndex =
            SourceLoadoutIndex;

        this.SourceLoadoutSequence =
            SourceLoadoutSequence;


        State =
            RuntimeCarriedUnitState.Carried;
    }


    internal bool MarkDeployed(
        RuntimeUnit RuntimeUnit
    )
    {
        if (State !=
                RuntimeCarriedUnitState.Carried ||
            RuntimeUnit == null)
        {
            return false;
        }


        DeployedUnit =
            RuntimeUnit;

        State =
            RuntimeCarriedUnitState.Deployed;


        return true;
    }


    internal bool MarkLost()
    {
        if (State !=
            RuntimeCarriedUnitState.Carried)
        {
            return false;
        }


        State =
            RuntimeCarriedUnitState.Lost;


        return true;
    }
}


// Runtime state for one specific authored System Slot.
public sealed class RuntimeSystemSlotState
{
    private readonly List<RuntimeCarriedUnit>
        _CarriedUnits =
            new List<RuntimeCarriedUnit>();


    public int SlotIndex
    {
        get;
        private set;
    }


    public UnitSystemSlot AuthoredSlot
    {
        get;
        private set;
    }


    public SystemSlotType SlotType
    {
        get
        {
            return
                AuthoredSlot != null
                    ? AuthoredSlot.SlotType
                    : SystemSlotType.None;
        }
    }


    public Definition_System System
    {
        get
        {
            return
                AuthoredSlot != null
                    ? AuthoredSlot.System
                    : null;
        }
    }


    public int MaxCharges
    {
        get;
        private set;
    }


    public int CurrentCharges
    {
        get;
        private set;
    }


    public int ReplenishmentProgress
    {
        get;
        private set;
    }


    public bool IsEquipped
    {
        get
        {
            return
                System != null;
        }
    }


    public bool UsesCharges
    {
        get
        {
            return
                System != null &&
                System.UsesCharges;
        }
    }


    public IReadOnlyList<RuntimeCarriedUnit> CarriedUnits
    {
        get
        {
            return
                _CarriedUnits;
        }
    }


    internal RuntimeSystemSlotState(
        RuntimeUnit OwnerUnit,
        int SlotIndex,
        UnitSystemSlot AuthoredSlot
    )
    {
        this.SlotIndex =
            SlotIndex;

        this.AuthoredSlot =
            AuthoredSlot;


        MaxCharges =
            AuthoredSlot != null
                ? Mathf.Max(
                    0,
                    AuthoredSlot.MaxCharges
                )
                : 0;


        CurrentCharges =
            UsesCharges
                ? MaxCharges
                : 0;


        ReplenishmentProgress =
            0;


        InitializeCarriedUnits(
            OwnerUnit
        );
    }


    private void InitializeCarriedUnits(
        RuntimeUnit OwnerUnit
    )
    {
        if (AuthoredSlot == null ||
            AuthoredSlot.CarriedUnits == null)
        {
            return;
        }


        CarriedUnitLoadout[] Loadouts =
            AuthoredSlot.CarriedUnits;


        for (
            int LoadoutIndex = 0;
            LoadoutIndex < Loadouts.Length;
            LoadoutIndex++
        )
        {
            CarriedUnitLoadout Loadout =
                Loadouts[LoadoutIndex];


            if (Loadout == null ||
                Loadout.Unit == null ||
                Loadout.Count < 1)
            {
                continue;
            }


            for (
                int Sequence = 0;
                Sequence < Loadout.Count;
                Sequence++
            )
            {
                _CarriedUnits.Add(
                    new RuntimeCarriedUnit(
                        OwnerUnit,
                        Loadout.Unit,
                        SlotIndex,
                        LoadoutIndex,
                        Sequence
                    )
                );
            }
        }
    }


    public bool HasCharges(
        int Amount = 1
    )
    {
        if (!UsesCharges)
        {
            return true;
        }


        return
            Amount > 0 &&
            CurrentCharges >= Amount;
    }


    internal bool TrySpendCharges(
        int Amount
    )
    {
        if (!UsesCharges ||
            Amount < 1 ||
            CurrentCharges < Amount)
        {
            return false;
        }


        CurrentCharges -=
            Amount;


        return true;
    }


    internal int ReplenishCharges(
        int Amount
    )
    {
        if (!UsesCharges ||
            Amount < 1)
        {
            return 0;
        }


        int PreviousCharges =
            CurrentCharges;


        CurrentCharges =
            Mathf.Min(
                MaxCharges,
                CurrentCharges + Amount
            );


        return
            CurrentCharges -
            PreviousCharges;
    }


    internal void AdvanceReplenishmentProgress()
    {
        ReplenishmentProgress++;
    }


    internal void ResetReplenishmentProgress()
    {
        ReplenishmentProgress =
            0;
    }
}


// Runtime identity for an Ability Slot.
//
// Definition_Ability currently has no authored charge capacity,
// so this class deliberately does not invent one.
public sealed class RuntimeAbilityState
{
    public int SlotIndex
    {
        get;
        private set;
    }


    public Definition_Ability Ability
    {
        get;
        private set;
    }


    internal RuntimeAbilityState(
        int SlotIndex,
        Definition_Ability Ability
    )
    {
        this.SlotIndex =
            SlotIndex;

        this.Ability =
            Ability;
    }
}


// Applied Status runtime state.
//
// Status application/ticking is not implemented in this pass;
// this simply gives RuntimeUnit the correct place to own it later.
public sealed class RuntimeStatusState
{
    public Definition_Status Status
    {
        get;
        private set;
    }


    public int RemainingTurns
    {
        get;
        internal set;
    }


    public int IntervalProgress
    {
        get;
        internal set;
    }


    internal RuntimeStatusState(
        Definition_Status Status,
        int RemainingTurns
    )
    {
        this.Status =
            Status;

        this.RemainingTurns =
            RemainingTurns;

        IntervalProgress =
            0;
    }
}


// Temporary Modifier runtime state.
//
// The authored GameplayModifier remains immutable;
// duration is per runtime application.
public sealed class RuntimeModifierState
{
    public GameplayModifier Modifier
    {
        get;
        private set;
    }


    public DurationMode DurationMode
    {
        get;
        private set;
    }


    public int RemainingTurns
    {
        get;
        internal set;
    }


    internal RuntimeModifierState(
        GameplayModifier Modifier,
        DurationMode DurationMode,
        int RemainingTurns
    )
    {
        this.Modifier =
            Modifier;

        this.DurationMode =
            DurationMode;

        this.RemainingTurns =
            RemainingTurns;
    }
}


// One actual Unit instance in the current Match.
//
// This object owns mutable gameplay state.
// It deliberately does NOT own an independent Map coordinate.
// Physical position is authoritative in GameMap.
public sealed class RuntimeUnit
{
    private readonly List<RuntimeStatusState>
        _ActiveStatuses =
            new List<RuntimeStatusState>();

    private readonly List<RuntimeModifierState>
        _ActiveModifiers =
            new List<RuntimeModifierState>();

    private readonly HashSet<string>
        _Tags =
            new HashSet<string>(
                StringComparer.Ordinal
            );


    public string SourceFleetEntryID
    {
        get;
        private set;
    }


    // Side-local identity and display instance name.
    // Example: "Rover Class 2".
    public string InstanceName
    {
        get;
        private set;
    }


    public Definition_Unit Definition
    {
        get;
        private set;
    }


    public RuntimeParticipant Owner
    {
        get;
        private set;
    }


    public RuntimeUnitLifeState LifeState
    {
        get;
        private set;
    }


    public int CurrentHealth
    {
        get;
        private set;
    }


    public int MaxHealth
    {
        get
        {
            return
                Definition != null
                    ? Definition.Health
                    : 0;
        }
    }


    public Domain CurrentDomain
    {
        get;
        private set;
    }


    public UnitFacing Facing
    {
        get;
        private set;
    }


    public RuntimeWeaponSlotState[] WeaponSlots
    {
        get;
        private set;
    }


    public RuntimeSystemSlotState[] SystemSlots
    {
        get;
        private set;
    }


    public RuntimeAbilityState[] Abilities
    {
        get;
        private set;
    }


    public IReadOnlyList<RuntimeStatusState> ActiveStatuses
    {
        get
        {
            return
                _ActiveStatuses;
        }
    }


    public IReadOnlyList<RuntimeModifierState> ActiveModifiers
    {
        get
        {
            return
                _ActiveModifiers;
        }
    }


    public bool IsActive
    {
        get
        {
            return
                LifeState ==
                RuntimeUnitLifeState.Active;
        }
    }


    internal RuntimeUnit(
        string SourceFleetEntryID,
        string InstanceName,
        Definition_Unit Definition,
        RuntimeParticipant Owner,
        UnitFacing Facing
    )
    {
        this.SourceFleetEntryID =
            SourceFleetEntryID;

        this.InstanceName =
            InstanceName;

        this.Definition =
            Definition;

        this.Owner =
            Owner;

        this.Facing =
            Facing;


        LifeState =
            RuntimeUnitLifeState.Active;


        CurrentHealth =
            Definition != null
                ? Definition.Health
                : 0;


        CurrentDomain =
            Definition != null
                ? Definition.BaseDomain
                : Domain.None;


        InitializeWeaponSlots();
        InitializeSystemSlots();
        InitializeAbilities();
    }


    private void InitializeWeaponSlots()
    {
        UnitWeaponSlot[] AuthoredSlots =
            Definition != null
                ? Definition.WeaponSlots
                : null;


        if (AuthoredSlots == null)
        {
            WeaponSlots =
                Array.Empty<RuntimeWeaponSlotState>();

            return;
        }


        WeaponSlots =
            new RuntimeWeaponSlotState[
                AuthoredSlots.Length
            ];


        for (
            int i = 0;
            i < AuthoredSlots.Length;
            i++
        )
        {
            WeaponSlots[i] =
                new RuntimeWeaponSlotState(
                    i,
                    AuthoredSlots[i]
                );
        }
    }


    private void InitializeSystemSlots()
    {
        UnitSystemSlot[] AuthoredSlots =
            Definition != null
                ? Definition.SystemSlots
                : null;


        if (AuthoredSlots == null)
        {
            SystemSlots =
                Array.Empty<RuntimeSystemSlotState>();

            return;
        }


        SystemSlots =
            new RuntimeSystemSlotState[
                AuthoredSlots.Length
            ];


        for (
            int i = 0;
            i < AuthoredSlots.Length;
            i++
        )
        {
            SystemSlots[i] =
                new RuntimeSystemSlotState(
                    this,
                    i,
                    AuthoredSlots[i]
                );
        }
    }


    private void InitializeAbilities()
    {
        Definition_Ability[] AuthoredAbilities =
            Definition != null
                ? Definition.Abilities
                : null;


        if (AuthoredAbilities == null)
        {
            Abilities =
                Array.Empty<RuntimeAbilityState>();

            return;
        }


        Abilities =
            new RuntimeAbilityState[
                AuthoredAbilities.Length
            ];


        for (
            int i = 0;
            i < AuthoredAbilities.Length;
            i++
        )
        {
            Abilities[i] =
                new RuntimeAbilityState(
                    i,
                    AuthoredAbilities[i]
                );
        }
    }


    public RuntimeCarriedUnit[] GetCarriedUnits()
    {
        List<RuntimeCarriedUnit> CarriedUnits =
            new List<RuntimeCarriedUnit>();


        if (SystemSlots == null)
        {
            return
                CarriedUnits.ToArray();
        }


        for (
            int i = 0;
            i < SystemSlots.Length;
            i++
        )
        {
            RuntimeSystemSlotState Slot =
                SystemSlots[i];


            if (Slot == null)
            {
                continue;
            }


            for (
                int CarriedIndex = 0;
                CarriedIndex < Slot.CarriedUnits.Count;
                CarriedIndex++
            )
            {
                CarriedUnits.Add(
                    Slot.CarriedUnits[
                        CarriedIndex
                    ]
                );
            }
        }


        return
            CarriedUnits.ToArray();
    }


    public bool HasTag(
        string TagID
    )
    {
        if (string.IsNullOrWhiteSpace(
                TagID))
        {
            return false;
        }


        return
            _Tags.Contains(
                TagID
            );
    }


    public string[] GetTags()
    {
        string[] Tags =
            new string[
                _Tags.Count
            ];


        _Tags.CopyTo(
            Tags
        );


        return
            Tags;
    }


    internal bool AddTag(
        string TagID
    )
    {
        if (string.IsNullOrWhiteSpace(
                TagID))
        {
            return false;
        }


        return
            _Tags.Add(
                TagID
            );
    }


    internal bool RemoveTag(
        string TagID
    )
    {
        if (string.IsNullOrWhiteSpace(
                TagID))
        {
            return false;
        }


        return
            _Tags.Remove(
                TagID
            );
    }


    internal void SetDomain(
        Domain Domain
    )
    {
        CurrentDomain =
            Domain;
    }


    internal void SetFacing(
        UnitFacing Facing
    )
    {
        this.Facing =
            Facing;
    }


    internal void SetHealth(
        int Health
    )
    {
        CurrentHealth =
            Mathf.Clamp(
                Health,
                0,
                MaxHealth
            );
    }


    internal RuntimeCarriedUnit[] MarkDestroyed()
    {
        if (LifeState ==
            RuntimeUnitLifeState.Destroyed)
        {
            return
                Array.Empty<RuntimeCarriedUnit>();
        }


        LifeState =
            RuntimeUnitLifeState.Destroyed;

        CurrentHealth =
            0;


        List<RuntimeCarriedUnit> LostUnits =
            new List<RuntimeCarriedUnit>();


        RuntimeCarriedUnit[] CarriedUnits =
            GetCarriedUnits();


        for (
            int i = 0;
            i < CarriedUnits.Length;
            i++
        )
        {
            RuntimeCarriedUnit Carried =
                CarriedUnits[i];


            // Deployed carried Units are already independent RuntimeUnits.
            // Only Units still aboard are lost with the carrier.
            if (Carried != null &&
                Carried.MarkLost())
            {
                LostUnits.Add(
                    Carried
                );
            }
        }


        return
            LostUnits.ToArray();
    }
}


// Runtime tracking for one top-level authored Fleet Entry.
//
// No RuntimeUnit exists until Deployment commits this Fleet Entry.
public sealed class RuntimeFleetEntryState
{
    public FleetEntry FleetEntry
    {
        get;
        private set;
    }


    public RuntimeUnit RuntimeUnit
    {
        get;
        private set;
    }


    public string ID
    {
        get
        {
            return
                FleetEntry != null
                    ? FleetEntry.ID
                    : string.Empty;
        }
    }


    public Definition_Unit Unit
    {
        get
        {
            return
                FleetEntry != null
                    ? FleetEntry.Unit
                    : null;
        }
    }


    public bool IsCommitted
    {
        get
        {
            return
                RuntimeUnit != null;
        }
    }


    internal RuntimeFleetEntryState(
        FleetEntry FleetEntry
    )
    {
        this.FleetEntry =
            FleetEntry;
    }


    internal bool Commit(
        RuntimeUnit RuntimeUnit
    )
    {
        if (IsCommitted ||
            RuntimeUnit == null)
        {
            return false;
        }


        this.RuntimeUnit =
            RuntimeUnit;


        return true;
    }
}


// One participant's completely separate Unit roster.
//
// There is deliberately no shared/global Unit pool between participants.
public sealed class ParticipantUnitRoster
{
    private readonly List<RuntimeFleetEntryState>
        _FleetEntries =
            new List<RuntimeFleetEntryState>();

    private readonly List<RuntimeUnit>
        _RuntimeUnits =
            new List<RuntimeUnit>();

    private readonly Dictionary<string, int>
        _InstanceNameCounters =
            new Dictionary<string, int>(
                StringComparer.Ordinal
            );


    public RuntimeParticipant Participant
    {
        get;
        private set;
    }


    public Definition_Fleet Fleet
    {
        get;
        private set;
    }


    public IReadOnlyList<RuntimeFleetEntryState> FleetEntries
    {
        get
        {
            return
                _FleetEntries;
        }
    }


    public IReadOnlyList<RuntimeUnit> RuntimeUnits
    {
        get
        {
            return
                _RuntimeUnits;
        }
    }


    public int CommittedRuntimeUnitCount
    {
        get
        {
            return
                _RuntimeUnits.Count;
        }
    }


    // Includes:
    // - undeployed top-level Fleet Entries
    // - active RuntimeUnits
    // - Units still carried aboard active Units
    //
    // Destroyed Units and carried Units lost with a carrier are excluded.
    public int RemainingFleetSize
    {
        get
        {
            int Count =
                0;


            for (
                int i = 0;
                i < _FleetEntries.Count;
                i++
            )
            {
                RuntimeFleetEntryState Entry =
                    _FleetEntries[i];


                if (Entry == null)
                {
                    continue;
                }


                if (!Entry.IsCommitted)
                {
                    Count++;


                    Count +=
                        CountAuthoredCarriedUnits(
                            Entry.Unit,
                            new HashSet<Definition_Unit>()
                        );
                }
            }


            for (
                int i = 0;
                i < _RuntimeUnits.Count;
                i++
            )
            {
                RuntimeUnit Unit =
                    _RuntimeUnits[i];


                if (Unit == null ||
                    !Unit.IsActive)
                {
                    continue;
                }


                Count++;


                RuntimeCarriedUnit[] CarriedUnits =
                    Unit.GetCarriedUnits();


                for (
                    int CarriedIndex = 0;
                    CarriedIndex < CarriedUnits.Length;
                    CarriedIndex++
                )
                {
                    RuntimeCarriedUnit Carried =
                        CarriedUnits[CarriedIndex];


                    if (Carried == null ||
                        Carried.State !=
                            RuntimeCarriedUnitState.Carried)
                    {
                        continue;
                    }


                    Count++;


                    Count +=
                        CountAuthoredCarriedUnits(
                            Carried.Unit,
                            new HashSet<Definition_Unit>()
                        );
                }
            }


            return
                Count;
        }
    }


    internal bool Initialize(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (Participant == null)
        {
            ValidationMessage =
                "Participant is missing.";

            return false;
        }


        if (Participant.Fleet == null)
        {
            ValidationMessage =
                "Participant has no Fleet.";

            return false;
        }


        this.Participant =
            Participant;

        Fleet =
            Participant.Fleet;


        _FleetEntries.Clear();
        _RuntimeUnits.Clear();
        _InstanceNameCounters.Clear();


        FleetEntry[] Entries =
            Fleet.Entries;


        if (Entries == null ||
            Entries.Length == 0)
        {
            ValidationMessage =
                $"Fleet '{Fleet.Name}' contains no Fleet Entries.";

            return false;
        }


        HashSet<string> UsedEntryIDs =
            new HashSet<string>(
                StringComparer.Ordinal
            );


        for (
            int i = 0;
            i < Entries.Length;
            i++
        )
        {
            FleetEntry Entry =
                Entries[i];


            if (Entry == null)
            {
                ValidationMessage =
                    $"Fleet '{Fleet.Name}' Entry {i} is missing.";

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    Entry.ID))
            {
                ValidationMessage =
                    $"Fleet '{Fleet.Name}' Entry {i} has no ID.";

                return false;
            }


            if (!UsedEntryIDs.Add(
                    Entry.ID))
            {
                ValidationMessage =
                    $"Fleet '{Fleet.Name}' contains duplicate Entry ID '{Entry.ID}'.";

                return false;
            }


            if (Entry.Unit == null)
            {
                ValidationMessage =
                    $"Fleet Entry '{Entry.ID}' has no Unit.";

                return false;
            }


            if (!Entry.Unit.Validate(
                    out string UnitValidationMessage))
            {
                ValidationMessage =
                    $"Fleet Entry '{Entry.ID}' uses invalid Unit '{Entry.Unit.name}': " +
                    UnitValidationMessage;

                return false;
            }


            _FleetEntries.Add(
                new RuntimeFleetEntryState(
                    Entry
                )
            );
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryGetFleetEntry(
        string FleetEntryID,
        out RuntimeFleetEntryState FleetEntry
    )
    {
        FleetEntry =
            null;


        if (string.IsNullOrWhiteSpace(
                FleetEntryID))
        {
            return false;
        }


        for (
            int i = 0;
            i < _FleetEntries.Count;
            i++
        )
        {
            RuntimeFleetEntryState Entry =
                _FleetEntries[i];


            if (Entry != null &&
                Entry.ID ==
                    FleetEntryID)
            {
                FleetEntry =
                    Entry;

                return true;
            }
        }


        return false;
    }


    // Called later by Deployment only after the complete placement
    // plan has passed spatial validation.
    internal bool TryCommitFleetEntry(
        string FleetEntryID,
        UnitFacing Facing,
        out RuntimeUnit RuntimeUnit,
        out string ValidationMessage
    )
    {
        RuntimeUnit =
            null;


        if (!TryGetFleetEntry(
                FleetEntryID,
                out RuntimeFleetEntryState Entry))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' does not exist.";

            return false;
        }


        if (Entry.IsCommitted)
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' has already been deployed.";

            return false;
        }


        string InstanceName =
            CreateNextInstanceName(
                Entry.Unit
            );


        RuntimeUnit =
            new RuntimeUnit(
                Entry.ID,
                InstanceName,
                Entry.Unit,
                Participant,
                Facing
            );


        if (!Entry.Commit(
                RuntimeUnit))
        {
            RuntimeUnit =
                null;


            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' could not be committed.";

            return false;
        }


        _RuntimeUnits.Add(
            RuntimeUnit
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Converts one carried record into an actual RuntimeUnit.
    // Once deployed, it can never become carried again.
    internal bool TryDeployCarriedUnit(
        RuntimeCarriedUnit CarriedUnit,
        UnitFacing Facing,
        out RuntimeUnit RuntimeUnit,
        out string ValidationMessage
    )
    {
        RuntimeUnit =
            null;


        if (CarriedUnit == null)
        {
            ValidationMessage =
                "Carried Unit record is missing.";

            return false;
        }


        if (CarriedUnit.Carrier == null ||
            CarriedUnit.Carrier.Owner !=
                Participant)
        {
            ValidationMessage =
                "Carried Unit does not belong to this Participant.";

            return false;
        }


        if (!CarriedUnit.Carrier.IsActive)
        {
            ValidationMessage =
                $"Carrier '{CarriedUnit.Carrier.InstanceName}' is destroyed.";

            return false;
        }


        if (CarriedUnit.State !=
            RuntimeCarriedUnitState.Carried)
        {
            ValidationMessage =
                "Carried Unit is no longer available for deployment.";

            return false;
        }


        if (CarriedUnit.Unit == null)
        {
            ValidationMessage =
                "Carried Unit has no Unit Definition.";

            return false;
        }


        string InstanceName =
            CreateNextInstanceName(
                CarriedUnit.Unit
            );


        RuntimeUnit =
            new RuntimeUnit(
                string.Empty,
                InstanceName,
                CarriedUnit.Unit,
                Participant,
                Facing
            );


        if (!CarriedUnit.MarkDeployed(
                RuntimeUnit))
        {
            RuntimeUnit =
                null;


            ValidationMessage =
                "Carried Unit could not be marked as deployed.";

            return false;
        }


        _RuntimeUnits.Add(
            RuntimeUnit
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryMarkDestroyed(
        RuntimeUnit RuntimeUnit,
        out RuntimeCarriedUnit[] LostCarriedUnits,
        out string ValidationMessage
    )
    {
        LostCarriedUnits =
            Array.Empty<RuntimeCarriedUnit>();


        if (RuntimeUnit == null ||
            !_RuntimeUnits.Contains(
                RuntimeUnit))
        {
            ValidationMessage =
                "Runtime Unit does not belong to this Participant.";

            return false;
        }


        if (!RuntimeUnit.IsActive)
        {
            ValidationMessage =
                $"Unit '{RuntimeUnit.InstanceName}' is already destroyed.";

            return false;
        }


        LostCarriedUnits =
            RuntimeUnit.MarkDestroyed();


        ValidationMessage =
            string.Empty;

        return true;
    }


    private string CreateNextInstanceName(
        Definition_Unit Unit
    )
    {
        string BaseName =
            Unit != null &&
            !string.IsNullOrWhiteSpace(
                Unit.Name)
                ? Unit.Name
                : "Unit";


        if (!_InstanceNameCounters.TryGetValue(
                BaseName,
                out int CurrentNumber))
        {
            CurrentNumber =
                0;
        }


        CurrentNumber++;


        _InstanceNameCounters[BaseName] =
            CurrentNumber;


        return
            $"{BaseName} {CurrentNumber}";
    }


    private static int CountAuthoredCarriedUnits(
        Definition_Unit Unit,
        HashSet<Definition_Unit> RecursionPath
    )
    {
        if (Unit == null ||
            RecursionPath.Contains(
                Unit))
        {
            return 0;
        }


        RecursionPath.Add(
            Unit
        );


        int Count =
            0;


        UnitSystemSlot[] SystemSlots =
            Unit.SystemSlots;


        if (SystemSlots != null)
        {
            for (
                int SlotIndex = 0;
                SlotIndex < SystemSlots.Length;
                SlotIndex++
            )
            {
                UnitSystemSlot Slot =
                    SystemSlots[SlotIndex];


                if (Slot == null ||
                    Slot.CarriedUnits == null)
                {
                    continue;
                }


                CarriedUnitLoadout[] Loadouts =
                    Slot.CarriedUnits;


                for (
                    int LoadoutIndex = 0;
                    LoadoutIndex < Loadouts.Length;
                    LoadoutIndex++
                )
                {
                    CarriedUnitLoadout Loadout =
                        Loadouts[LoadoutIndex];


                    if (Loadout == null ||
                        Loadout.Unit == null ||
                        Loadout.Count < 1)
                    {
                        continue;
                    }


                    Count +=
                        Loadout.Count;


                    int NestedCount =
                        CountAuthoredCarriedUnits(
                            Loadout.Unit,
                            new HashSet<Definition_Unit>(
                                RecursionPath
                            )
                        );


                    Count +=
                        NestedCount *
                        Loadout.Count;
                }
            }
        }


        return
            Count;
    }
}


// GameMaster's dedicated Unit-state arm.
//
// Participant A and Participant B are intentionally maintained
// as completely separate rosters.
public sealed class GameUnits
{
    public ParticipantUnitRoster ParticipantA
    {
        get;
        private set;
    }


    public ParticipantUnitRoster ParticipantB
    {
        get;
        private set;
    }


    public bool Initialized
    {
        get;
        private set;
    }


    public bool Initialize(
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB,
        out string ValidationMessage
    )
    {
        if (ParticipantA == null ||
            ParticipantB == null)
        {
            ValidationMessage =
                "GameUnits requires both Match Participants.";

            return false;
        }


        ParticipantUnitRoster RosterA =
            new ParticipantUnitRoster();


        if (!RosterA.Initialize(
                ParticipantA,
                out string ParticipantAMessage))
        {
            ValidationMessage =
                $"Participant A Unit State is invalid: {ParticipantAMessage}";

            return false;
        }


        ParticipantUnitRoster RosterB =
            new ParticipantUnitRoster();


        if (!RosterB.Initialize(
                ParticipantB,
                out string ParticipantBMessage))
        {
            ValidationMessage =
                $"Participant B Unit State is invalid: {ParticipantBMessage}";

            return false;
        }


        this.ParticipantA =
            RosterA;

        this.ParticipantB =
            RosterB;


        Initialized =
            true;


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryGetRoster(
        RuntimeParticipant Participant,
        out ParticipantUnitRoster Roster
    )
    {
        Roster =
            null;


        if (!Initialized ||
            Participant == null)
        {
            return false;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantA.Participant))
        {
            Roster =
                ParticipantA;

            return true;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantB.Participant))
        {
            Roster =
                ParticipantB;

            return true;
        }


        return false;
    }
}