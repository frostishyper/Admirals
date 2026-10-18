using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative Battle-phase state. Presentation submits only participant-bound
// Unit IDs, domains and known target coordinates; hidden target identity is
// resolved internally through GameKnowledge.
public sealed class GameBattle
{
    public enum CombatActionKind
    {
        None = 0,
        Weapon = 1,
        Ability = 2,
        System = 3
    }


    public sealed class ActionOption
    {
        public CombatActionKind Kind { get; }
        public int SlotIndex { get; }
        public string Name { get; }
        public bool UsesD20 { get; }
        public int CurrentCharges { get; }
        public int MaxCharges { get; }

        internal ActionOption(CombatActionKind Kind, int SlotIndex,
            string Name, bool UsesD20, int CurrentCharges, int MaxCharges)
        {
            this.Kind = Kind;
            this.SlotIndex = SlotIndex;
            this.Name = Name ?? string.Empty;
            this.UsesD20 = UsesD20;
            this.CurrentCharges = CurrentCharges;
            this.MaxCharges = MaxCharges;
        }
    }


    public sealed class AllySupportGroup
    {
        public string RuntimeUnitID { get; }
        public string DefinitionID { get; }
        public string UnitName { get; }
        public ActionOption[] Options { get; }

        internal AllySupportGroup(RuntimeUnit Unit, ActionOption[] Options)
        {
            RuntimeUnitID = Unit?.RuntimeID ?? string.Empty;
            DefinitionID = Unit?.Definition?.ID ?? string.Empty;
            UnitName = Unit?.InstanceName ?? string.Empty;
            this.Options = Options ?? Array.Empty<ActionOption>();
        }
    }


    public sealed class ResolutionEvent
    {
        public string Label { get; }
        public string Text { get; }

        internal ResolutionEvent(string Label, string Text)
        {
            this.Label = Label ?? string.Empty;
            this.Text = Text ?? string.Empty;
        }
    }


    public sealed class ResolutionView
    {
        public int AttackerRoll { get; }
        public int AllyRoll { get; }
        public int DefenderRoll { get; }
        public int Damage { get; }
        public bool DefenderDestroyed { get; }
        public string ResultName { get; }
        public ResolutionEvent[] Events { get; }

        internal ResolutionView(int AttackerRoll, int AllyRoll,
            int DefenderRoll, int Damage, bool DefenderDestroyed,
            string ResultName, ResolutionEvent[] Events)
        {
            this.AttackerRoll = AttackerRoll;
            this.AllyRoll = AllyRoll;
            this.DefenderRoll = DefenderRoll;
            this.Damage = Damage;
            this.DefenderDestroyed = DefenderDestroyed;
            this.ResultName = ResultName ?? string.Empty;
            this.Events = Events ?? Array.Empty<ResolutionEvent>();
        }
    }

    public sealed class UnitView
    {
        public string RuntimeUnitID { get; }
        public bool HasAttacked { get; }

        internal UnitView(string RuntimeUnitID, bool HasAttacked)
        {
            this.RuntimeUnitID = RuntimeUnitID ?? string.Empty;
            this.HasAttacked = HasAttacked;
        }
    }


    public sealed class EngagementView
    {
        public bool IsActive { get; }
        public bool IsAttacker { get; }
        public string AttackerRuntimeUnitID { get; }
        public string DefenderRuntimeUnitID { get; }
        public string AttackerDefinitionID { get; }
        public string DefenderDefinitionID { get; }
        public string AttackerDefinitionName { get; }
        public string DefenderDefinitionName { get; }
        public string AttackerInstanceName { get; }
        public string DefenderInstanceName { get; }
        public int AttackerHealth { get; }
        public int AttackerMaxHealth { get; }
        public int DefenderHealth { get; }
        public int DefenderMaxHealth { get; }
        public Domain DefenderDomain { get; }
        public bool BeganFromContact { get; }
        public bool AttackerLocked { get; }
        public bool DefenderLocked { get; }
        public bool Resolved { get; }
        public string SelectedAttackName { get; }
        public ActionOption[] AttackerOptions { get; }
        public ActionOption[] DefenderOptions { get; }
        public AllySupportGroup[] AllySupport { get; }
        public ResolutionView Resolution { get; }

        internal EngagementView(RuntimeParticipant Viewer,
            RuntimeUnit Attacker, RuntimeUnit Defender, bool BeganFromContact,
            bool AttackerLocked, bool DefenderLocked,
            string SelectedAttackName, ActionOption[] AttackerOptions,
            ActionOption[] DefenderOptions, AllySupportGroup[] AllySupport,
            ResolutionView Resolution)
        {
            IsActive = Attacker != null && Defender != null;
            IsAttacker = Attacker != null && ReferenceEquals(Viewer, Attacker.Owner);
            AttackerRuntimeUnitID = Attacker?.RuntimeID ?? string.Empty;
            DefenderRuntimeUnitID = Defender?.RuntimeID ?? string.Empty;
            AttackerDefinitionID = Attacker?.Definition?.ID ?? string.Empty;
            DefenderDefinitionID = Defender?.Definition?.ID ?? string.Empty;
            AttackerDefinitionName = Attacker?.Definition?.Name ?? string.Empty;
            DefenderDefinitionName = Defender?.Definition?.Name ?? string.Empty;
            AttackerInstanceName = Attacker?.InstanceName ?? string.Empty;
            DefenderInstanceName = Defender?.InstanceName ?? string.Empty;
            AttackerHealth = Attacker?.CurrentHealth ?? 0;
            AttackerMaxHealth = Attacker?.MaxHealth ?? 0;
            DefenderHealth = Defender?.CurrentHealth ?? 0;
            DefenderMaxHealth = Defender?.MaxHealth ?? 0;
            DefenderDomain = Defender?.CurrentDomain ?? Domain.None;
            this.BeganFromContact = BeganFromContact;
            this.AttackerLocked = AttackerLocked;
            this.DefenderLocked = DefenderLocked;
            Resolved = Resolution != null;
            this.SelectedAttackName = AttackerLocked
                ? SelectedAttackName ?? string.Empty : string.Empty;
            this.AttackerOptions = IsAttacker
                ? AttackerOptions ?? Array.Empty<ActionOption>()
                : Array.Empty<ActionOption>();
            this.DefenderOptions = !IsAttacker && AttackerLocked
                ? DefenderOptions ?? Array.Empty<ActionOption>()
                : Array.Empty<ActionOption>();
            this.AllySupport = !IsAttacker && AttackerLocked
                ? AllySupport ?? Array.Empty<AllySupportGroup>()
                : Array.Empty<AllySupportGroup>();
            this.Resolution = Resolution;
        }
    }


    public sealed class ParticipantView
    {
        public bool IsBattlePhaseActive { get; }
        public bool BattleEnded { get; }
        public bool CanAct { get; }
        public bool EngagementActive { get; }
        public float SecondsRemaining { get; }
        public int AttacksCommitted { get; }
        public UnitView[] Units { get; }
        public EngagementView Engagement { get; }

        internal ParticipantView(bool Active, bool Ended, bool CanAct,
            float SecondsRemaining, int AttacksCommitted,
            UnitView[] Units, EngagementView Engagement)
        {
            IsBattlePhaseActive = Active;
            BattleEnded = Ended;
            this.CanAct = CanAct;
            EngagementActive = Engagement != null && Engagement.IsActive;
            this.SecondsRemaining = SecondsRemaining;
            this.AttacksCommitted = AttacksCommitted;
            this.Units = Units ?? Array.Empty<UnitView>();
            this.Engagement = Engagement;
        }
    }


    public sealed class TargetView
    {
        public Vector2Int Coordinate { get; }
        public Domain Domain { get; }
        public bool IsAbsoluteVisible { get; }
        public int LegalWeaponCount { get; }
        public int LegalAbilityCount { get; }

        internal TargetView(Vector2Int Coordinate, Domain Domain,
            bool IsAbsoluteVisible, int LegalWeaponCount, int LegalAbilityCount)
        {
            this.Coordinate = Coordinate;
            this.Domain = Domain;
            this.IsAbsoluteVisible = IsAbsoluteVisible;
            this.LegalWeaponCount = LegalWeaponCount;
            this.LegalAbilityCount = LegalAbilityCount;
        }
    }


    private sealed class ParticipantState
    {
        public readonly RuntimeParticipant Participant;
        public readonly HashSet<RuntimeUnit> AttackedUnits = new HashSet<RuntimeUnit>();
        public bool Ended;

        public ParticipantState(RuntimeParticipant Participant)
        {
            this.Participant = Participant;
        }
    }


    private GameMap _GameMap;
    private GameUnits _GameUnits;
    private GameKnowledge _GameKnowledge;
    private GameUnitEffects _GameUnitEffects;
    private ParticipantState _ParticipantA;
    private ParticipantState _ParticipantB;
    private RuntimeParticipant _ActiveParticipant;
    private RuntimeUnit _EngagementAttacker;
    private RuntimeUnit _EngagementDefender;
    private bool _EngagementBeganFromContact;
    private CombatActionKind _AttackKind;
    private int _AttackSlotIndex = -1;
    private string _SelectedAttackName = string.Empty;
    private CombatActionKind _DefenseKind;
    private int _DefenseSlotIndex = -1;
    private RuntimeUnit _SupportUnit;
    private CombatActionKind _SupportKind;
    private int _SupportSlotIndex = -1;
    private bool _AttackerLocked;
    private bool _DefenderLocked;
    private ResolutionView _Resolution;
    private float _SecondsPerParticipant;
    private float _SecondsRemaining;
    private bool _PhaseActive;

    public bool IsInitialized { get; private set; }
    public bool EngagementActive =>
        _EngagementAttacker != null && _EngagementDefender != null;


    internal bool Initialize(GameMap GameMap, GameUnits GameUnits,
        GameKnowledge GameKnowledge, GameUnitEffects GameUnitEffects,
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB, out string ValidationMessage)
    {
        IsInitialized = false;
        if (GameMap == null || !GameMap.Initialized ||
            GameUnits == null || !GameUnits.Initialized ||
            GameKnowledge == null || !GameKnowledge.IsInitialized ||
            GameUnitEffects == null || !GameUnitEffects.Initialized ||
            ParticipantA == null || ParticipantB == null ||
            ReferenceEquals(ParticipantA, ParticipantB))
        {
            ValidationMessage =
                "Battle requires initialized Map, Unit, knowledge and participant state.";
            return false;
        }

        _GameMap = GameMap;
        _GameUnits = GameUnits;
        _GameKnowledge = GameKnowledge;
        _GameUnitEffects = GameUnitEffects;
        _ParticipantA = new ParticipantState(ParticipantA);
        _ParticipantB = new ParticipantState(ParticipantB);
        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool BeginPhase(RuntimeParticipant FirstParticipant,
        float SecondsPerParticipant, out string ValidationMessage)
    {
        if (!IsInitialized ||
            (!ReferenceEquals(FirstParticipant, _ParticipantA.Participant) &&
             !ReferenceEquals(FirstParticipant, _ParticipantB.Participant)))
        {
            ValidationMessage = "Battle could not resolve its first participant.";
            return false;
        }

        ResetState(_ParticipantA);
        ResetState(_ParticipantB);
        ClearEngagement();
        _ActiveParticipant = FirstParticipant;
        _SecondsPerParticipant = Mathf.Max(1f, SecondsPerParticipant);
        _SecondsRemaining = _SecondsPerParticipant;
        _PhaseActive = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetView(RuntimeParticipant Participant,
        out ParticipantView View)
    {
        View = null;
        if (!TryGetState(Participant, out ParticipantState State) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster))
            return false;

        List<UnitView> Units = new List<UnitView>();
        for (int i = 0; i < Roster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Unit = Roster.RuntimeUnits[i];
            if (Unit != null)
                Units.Add(new UnitView(Unit.RuntimeID,
                    State.AttackedUnits.Contains(Unit)));
        }

        bool CanAct = _PhaseActive && !State.Ended &&
            ReferenceEquals(Participant, _ActiveParticipant) && !EngagementActive;
        EngagementView Engagement = EngagementActive
            ? new EngagementView(Participant, _EngagementAttacker,
                _EngagementDefender, _EngagementBeganFromContact,
                _AttackerLocked, _DefenderLocked, _SelectedAttackName,
                BuildAttackerOptions(), BuildDefenderOptions(),
                BuildAllySupportOptions(), _Resolution)
            : null;
        View = new ParticipantView(_PhaseActive, State.Ended, CanAct,
            _SecondsRemaining, State.AttackedUnits.Count,
            Units.ToArray(), Engagement);
        return true;
    }


    internal bool TryGetFireControlArea(RuntimeParticipant Participant,
        string RuntimeUnitID, Domain Domain,
        out Vector2Int[] Coordinates, out string ValidationMessage)
    {
        Coordinates = Array.Empty<Vector2Int>();
        if (!TryGetReadyAttacker(Participant, RuntimeUnitID,
                out RuntimeUnit Unit, out _, out ValidationMessage) ||
            !IsSingleDomain(Domain))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "Select exactly one Fire Control domain.";
            return false;
        }

        HashSet<Vector2Int> Area = BuildFireControlArea(Unit, Domain);
        Coordinates = ToArray(Area);
        if (Coordinates.Length == 0)
        {
            ValidationMessage =
                $"Unit '{Unit.InstanceName}' has no available {Domain} Fire Control area.";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetTarget(RuntimeParticipant Participant,
        string RuntimeUnitID, Vector2Int Coordinate,
        out TargetView Target, out string ValidationMessage)
    {
        Target = null;
        if (!TryGetReadyAttacker(Participant, RuntimeUnitID,
                out RuntimeUnit Attacker, out _, out ValidationMessage) ||
            !_GameKnowledge.TryResolveKnownEnemyAtCoordinate(
                Participant, Coordinate, out RuntimeUnit Defender,
                out bool IsAbsoluteVisible))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "That coordinate is not a known enemy target.";
            return false;
        }

        CountLegalActions(Attacker, Defender, IsAbsoluteVisible,
            out int Weapons, out int Abilities);
        if (Weapons == 0 && Abilities == 0)
        {
            ValidationMessage =
                "The selected Unit has no legal action against that target.";
            return false;
        }

        Target = new TargetView(Coordinate, Defender.CurrentDomain,
            IsAbsoluteVisible, Weapons, Abilities);
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryBeginEngagement(RuntimeParticipant Participant,
        string RuntimeUnitID, Vector2Int TargetCoordinate,
        out string ValidationMessage)
    {
        if (EngagementActive)
        {
            ValidationMessage = "Another engagement is already active.";
            return false;
        }
        if (!TryGetTarget(Participant, RuntimeUnitID, TargetCoordinate,
                out TargetView Target, out ValidationMessage) ||
            !TryGetReadyAttacker(Participant, RuntimeUnitID,
                out RuntimeUnit Attacker, out _, out ValidationMessage) ||
            !_GameKnowledge.TryResolveKnownEnemyAtCoordinate(
                Participant, TargetCoordinate, out RuntimeUnit Defender,
                out bool IsAbsoluteVisible))
            return false;

        _EngagementAttacker = Attacker;
        _EngagementDefender = Defender;
        _EngagementBeganFromContact = !IsAbsoluteVisible;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryCancelEngagement(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!EngagementActive ||
            !ReferenceEquals(Participant, _EngagementAttacker.Owner))
        {
            ValidationMessage = "Only the attacker may cancel this open engagement.";
            return false;
        }
        ClearEngagement();
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryLockAttack(RuntimeParticipant Participant,
        CombatActionKind Kind, int SlotIndex, out string ValidationMessage)
    {
        if (!EngagementActive || _Resolution != null || _AttackerLocked ||
            !ReferenceEquals(Participant, _EngagementAttacker.Owner) ||
            !TryFindOption(BuildAttackerOptions(), Kind, SlotIndex,
                out ActionOption SelectedOption))
        {
            ValidationMessage = "The selected attack cannot be locked for this engagement.";
            return false;
        }
        _AttackKind = Kind;
        _AttackSlotIndex = SlotIndex;
        _SelectedAttackName = SelectedOption.Name;
        _AttackerLocked = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryLockDefense(RuntimeParticipant Participant,
        CombatActionKind DefenseKind, int DefenseSlotIndex,
        string SupportRuntimeUnitID, CombatActionKind SupportKind,
        int SupportSlotIndex, out ResolutionView Resolution,
        out string ValidationMessage)
    {
        Resolution = null;
        if (!EngagementActive || !_AttackerLocked || _DefenderLocked ||
            _Resolution != null ||
            !ReferenceEquals(Participant, _EngagementDefender.Owner))
        {
            ValidationMessage = "Defense is not accepting a lock-in from this participant.";
            return false;
        }

        if (DefenseKind != CombatActionKind.None &&
            !TryFindOption(BuildDefenderOptions(), DefenseKind,
                DefenseSlotIndex, out _))
        {
            ValidationMessage = "The selected defender action is not legal.";
            return false;
        }

        RuntimeUnit SupportUnit = null;
        if (SupportKind != CombatActionKind.None)
        {
            AllySupportGroup[] Groups = BuildAllySupportOptions();
            bool Found = false;
            for (int i = 0; i < Groups.Length && !Found; i++)
            {
                if (Groups[i].RuntimeUnitID != SupportRuntimeUnitID ||
                    !TryFindOption(Groups[i].Options, SupportKind,
                        SupportSlotIndex, out _))
                    continue;
                Found = TryFindRuntimeUnit(_EngagementDefender.Owner,
                    SupportRuntimeUnitID, out SupportUnit);
            }
            if (!Found)
            {
                ValidationMessage = "The selected allied support action is not legal.";
                return false;
            }
        }

        _DefenseKind = DefenseKind;
        _DefenseSlotIndex = DefenseSlotIndex;
        _SupportUnit = SupportUnit;
        _SupportKind = SupportKind;
        _SupportSlotIndex = SupportSlotIndex;
        _DefenderLocked = true;
        if (!ResolveEngagement(out _Resolution, out ValidationMessage))
        {
            _DefenderLocked = false;
            return false;
        }
        Resolution = _Resolution;
        return true;
    }


    internal bool TryContinueEngagement(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!EngagementActive || _Resolution == null ||
            (!ReferenceEquals(Participant, _EngagementAttacker.Owner) &&
             !ReferenceEquals(Participant, _EngagementDefender.Owner)))
        {
            ValidationMessage =
                "Only a participant in the resolved engagement may continue.";
            return false;
        }
        MarkEngagementResolved();
        ValidationMessage = string.Empty;
        return true;
    }


    // Called by the resolution transaction after costs and outcomes complete.
    internal void MarkEngagementResolved()
    {
        if (!EngagementActive ||
            !TryGetState(_EngagementAttacker.Owner, out ParticipantState State))
            return;
        State.AttackedUnits.Add(_EngagementAttacker);
        ClearEngagement();
    }


    internal bool TryEndBattle(RuntimeParticipant Participant,
        out bool AllParticipantsEnded, out string ValidationMessage)
    {
        AllParticipantsEnded = false;
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage))
            return false;
        TryGetState(Participant, out ParticipantState State);
        EndParticipant(State, out AllParticipantsEnded);
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool Tick(float DeltaSeconds,
        out RuntimeParticipant TimedOutParticipant,
        out bool AllParticipantsEnded)
    {
        TimedOutParticipant = null;
        AllParticipantsEnded = false;
        if (!_PhaseActive || _ActiveParticipant == null || EngagementActive)
            return false;
        _SecondsRemaining = Mathf.Max(0f,
            _SecondsRemaining - Mathf.Max(0f, DeltaSeconds));
        if (_SecondsRemaining > 0f)
            return false;
        TimedOutParticipant = _ActiveParticipant;
        TryGetState(TimedOutParticipant, out ParticipantState State);
        EndParticipant(State, out AllParticipantsEnded);
        return true;
    }


    internal bool CanAcceptParticipantAction(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!_PhaseActive || EngagementActive ||
            !TryGetState(Participant, out ParticipantState State) ||
            State.Ended || !ReferenceEquals(Participant, _ActiveParticipant))
        {
            ValidationMessage = "Battle is not accepting overworld actions from this participant.";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }


    private bool TryGetReadyAttacker(RuntimeParticipant Participant,
        string RuntimeUnitID, out RuntimeUnit Unit,
        out ParticipantState State, out string ValidationMessage)
    {
        Unit = null;
        State = null;
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster) ||
            !Roster.TryGetRuntimeUnit(RuntimeUnitID, out Unit) || Unit == null ||
            !Unit.IsActive || !_GameMap.IsUnitOnMap(Unit))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "The requested attacker is not active on the Map.";
            return false;
        }
        TryGetState(Participant, out State);
        if (State.AttackedUnits.Contains(Unit))
        {
            ValidationMessage = "That Unit has already initiated an engagement this Battle phase.";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }


    private ActionOption[] BuildAttackerOptions()
    {
        if (!EngagementActive)
            return Array.Empty<ActionOption>();
        List<ActionOption> Options = new List<ActionOption>();
        if (!_GameMap.TryGetUnitCoordinates(_EngagementDefender,
                out Vector2Int[] Footprint))
            return Options.ToArray();
        bool InFireControl = Overlaps(BuildFireControlArea(
            _EngagementAttacker, _EngagementDefender.CurrentDomain), Footprint);
        for (int i = 0; i < _EngagementAttacker.WeaponSlots.Length; i++)
        {
            RuntimeWeaponSlotState Slot = _EngagementAttacker.WeaponSlots[i];
            if (Slot?.Weapon == null || !Slot.HasCharges() || !InFireControl ||
                (Slot.Weapon.TargetDomains & _EngagementDefender.CurrentDomain) == 0)
                continue;
            Options.Add(new ActionOption(CombatActionKind.Weapon, i,
                Slot.Weapon.Name, Slot.Weapon.UsesD20Roll,
                Slot.CurrentCharges, Slot.MaxCharges));
        }

        for (int i = 0; i < _EngagementAttacker.Abilities.Length; i++)
        {
            Definition_Ability Ability = _EngagementAttacker.Abilities[i]?.Ability;
            if (!IsLegalAttackAbility(_EngagementAttacker, Ability,
                    _EngagementDefender, !_EngagementBeganFromContact))
                continue;
            Options.Add(new ActionOption(CombatActionKind.Ability, i,
                Ability.Name, Ability.UsesD20Roll, 0, 0));
        }
        return Options.ToArray();
    }


    private ActionOption[] BuildDefenderOptions()
    {
        if (!EngagementActive || !_AttackerLocked)
            return Array.Empty<ActionOption>();
        return BuildDefenseOptions(_EngagementDefender, false);
    }


    private AllySupportGroup[] BuildAllySupportOptions()
    {
        if (!EngagementActive || !_AttackerLocked ||
            !_GameUnits.TryGetRoster(_EngagementDefender.Owner,
                out ParticipantUnitRoster Roster))
            return Array.Empty<AllySupportGroup>();
        List<AllySupportGroup> Groups = new List<AllySupportGroup>();
        for (int i = 0; i < Roster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Unit = Roster.RuntimeUnits[i];
            if (Unit == null || ReferenceEquals(Unit, _EngagementDefender) ||
                !Unit.IsActive || !_GameMap.IsUnitOnMap(Unit) ||
                !CanSupportDefender(Unit))
                continue;
            ActionOption[] Options = BuildDefenseOptions(Unit, true);
            if (Options.Length > 0)
                Groups.Add(new AllySupportGroup(Unit, Options));
        }
        return Groups.ToArray();
    }


    private ActionOption[] BuildDefenseOptions(RuntimeUnit Unit, bool Ally)
    {
        List<ActionOption> Options = new List<ActionOption>();
        Definition_Weapon IncomingWeapon = GetSelectedWeapon();
        for (int i = 0; i < Unit.SystemSlots.Length; i++)
        {
            RuntimeSystemSlotState Slot = Unit.SystemSlots[i];
            Definition_System System = Slot?.System;
            SystemDefenseCapability Defense = System?.Defense;
            if (Defense == null || !Defense.Enabled ||
                (System.OperationalPhases & GameplayPhase.Battle) == 0 ||
                (Ally ? !Defense.CanDefendAllies : !Defense.CanDefendSelf) ||
                !Slot.HasCharges() ||
                (IncomingWeapon != null && !Defense.CanDefendAgainst(IncomingWeapon)) ||
                (Ally && !DoesPatternReachDefender(Unit, System.Pattern)))
                continue;
            Options.Add(new ActionOption(CombatActionKind.System, i,
                System.Name, Defense.UsesD20Roll,
                Slot.CurrentCharges, Slot.MaxCharges));
        }
        for (int i = 0; i < Unit.Abilities.Length; i++)
        {
            Definition_Ability Ability = Unit.Abilities[i]?.Ability;
            if (Ability == null ||
                (Ability.UsablePhases & GameplayPhase.Battle) == 0 ||
                (Ability.CombatRoles & CombatRole.Defender) == 0 ||
                Ability.OutcomeType != AbilityOutcomeType.Defense ||
                (Ability.ValidTargets & (Ally ? GameplayTarget.Ally : GameplayTarget.Self)) == 0 ||
                (Ally && !DoesPatternReachDefender(Unit, Ability.Pattern)))
                continue;
            Options.Add(new ActionOption(CombatActionKind.Ability, i,
                Ability.Name, Ability.UsesD20Roll, 0, 0));
        }
        return Options.ToArray();
    }


    private bool DoesPatternReachDefender(RuntimeUnit Unit,
        Definition_Pattern Pattern)
    {
        return Pattern != null &&
            _GameMap.TryGetUnitCoordinates(_EngagementDefender,
                out Vector2Int[] DefenderFootprint) &&
            Overlaps(BuildPatternArea(Unit, Pattern,
                GetModifierTotal(Unit, ModifierType.AttackRange)),
                DefenderFootprint);
    }


    private bool CanSupportDefender(RuntimeUnit Unit)
    {
        if (!_GameMap.TryGetUnitCoordinates(_EngagementDefender,
                out Vector2Int[] DefenderFootprint))
            return false;
        for (int i = 0; i < Unit.SystemSlots.Length; i++)
        {
            Definition_System System = Unit.SystemSlots[i]?.System;
            if (System?.Defense == null || !System.Defense.Enabled ||
                !System.Defense.CanDefendAllies || System.Pattern == null)
                continue;
            if (Overlaps(BuildPatternArea(Unit, System.Pattern,
                    GetModifierTotal(Unit, ModifierType.AttackRange)),
                    DefenderFootprint))
                return true;
        }
        for (int i = 0; i < Unit.Abilities.Length; i++)
        {
            Definition_Ability Ability = Unit.Abilities[i]?.Ability;
            if (Ability != null && Ability.OutcomeType == AbilityOutcomeType.Defense &&
                (Ability.ValidTargets & GameplayTarget.Ally) != 0 &&
                Ability.Pattern != null &&
                Overlaps(BuildPatternArea(Unit, Ability.Pattern,
                    GetModifierTotal(Unit, ModifierType.AttackRange)),
                    DefenderFootprint))
                return true;
        }
        return false;
    }


    private bool ResolveEngagement(out ResolutionView Resolution,
        out string ValidationMessage)
    {
        Resolution = null;
        List<ResolutionEvent> Events = new List<ResolutionEvent>();
        int AttackRoll = UsesD20(_EngagementAttacker, _AttackKind, _AttackSlotIndex)
            ? UnityEngine.Random.Range(1, 21) : 0;
        int AllyRoll = _SupportKind != CombatActionKind.None &&
            UsesD20(_SupportUnit, _SupportKind, _SupportSlotIndex)
                ? UnityEngine.Random.Range(1, 21) : 0;
        int DefenderRoll = _DefenseKind != CombatActionKind.None &&
            UsesD20(_EngagementDefender, _DefenseKind, _DefenseSlotIndex)
                ? UnityEngine.Random.Range(1, 21) : 0;

        Events.Add(new ResolutionEvent("ATTACKER", _SelectedAttackName));
        DefenseOutcome AllyOutcome = ResolveDefenseOutcome(_SupportUnit,
            _SupportKind, _SupportSlotIndex, AllyRoll);
        DefenseOutcome DefenderOutcome = ResolveDefenseOutcome(_EngagementDefender,
            _DefenseKind, _DefenseSlotIndex, DefenderRoll);
        if (_SupportKind != CombatActionKind.None)
            Events.Add(new ResolutionEvent("ALLY",
                GetDefenseActionName(_SupportUnit, _SupportKind, _SupportSlotIndex)));
        Events.Add(new ResolutionEvent("DEFENDER",
            _DefenseKind == CombatActionKind.None ? "No Defense" :
                GetDefenseActionName(_EngagementDefender,
                    _DefenseKind, _DefenseSlotIndex)));
        Events.Add(new ResolutionEvent("ROLL",
            $"Attacker {FormatRoll(AttackRoll)} · Ally {FormatRoll(AllyRoll)} · " +
            $"Defender {FormatRoll(DefenderRoll)}"));
        if (AllyOutcome != null)
            Events.Add(new ResolutionEvent("ALLY OUTCOME", AllyOutcome.Name));
        if (DefenderOutcome != null)
            Events.Add(new ResolutionEvent("DEFENSE OUTCOME", DefenderOutcome.Name));

        int RollModifier = (AllyOutcome?.AttackRollModifier ?? 0) +
            (DefenderOutcome?.AttackRollModifier ?? 0);
        AttackOutcome BaseAttackOutcome = ResolveAttackOutcome(AttackRoll, 0);
        bool BypassDefense = BaseAttackOutcome != null &&
            BaseAttackOutcome.HasAttackProperty(AttackProperty.BypassDefense);
        if ((AllyOutcome?.StripBypassDefense ?? false) ||
            (DefenderOutcome?.StripBypassDefense ?? false))
            BypassDefense = false;
        bool GuaranteedHit = BaseAttackOutcome != null &&
            BaseAttackOutcome.HasAttackProperty(AttackProperty.GuaranteedHit);
        if ((AllyOutcome?.StripGuaranteedHit ?? false) ||
            (DefenderOutcome?.StripGuaranteedHit ?? false))
            GuaranteedHit = false;
        AttackOutcome AttackOutcome = ResolveAttackOutcome(AttackRoll,
            BypassDefense ? 0 : RollModifier);
        if (AttackOutcome == null || BaseAttackOutcome == null)
        {
            ValidationMessage = "The selected attack has no outcome for the resolved roll.";
            return false;
        }

        bool Negated = !BypassDefense && !GuaranteedHit &&
            ((AllyOutcome?.NegatesAttack ?? false) ||
             (DefenderOutcome?.NegatesAttack ?? false));
        int DamageModifier = BypassDefense ? 0 :
            (AllyOutcome?.DamageModifier ?? 0) +
            (DefenderOutcome?.DamageModifier ?? 0);
        int Damage = AttackOutcome.Result == OutcomeResult.Hit && !Negated
            ? Mathf.Max(0, AttackOutcome.Damage + DamageModifier +
                GetModifierTotal(_EngagementAttacker, ModifierType.Damage))
            : 0;

        if (!SpendSelectedCharges(out ValidationMessage))
            return false;
        if (!_GameUnitEffects.TryApplyDamage(_EngagementDefender, Damage,
                out bool Destroyed, out ValidationMessage))
            return false;

        ApplyDefenseActions(_SupportUnit, AllyOutcome);
        ApplyDefenseActions(_EngagementDefender, DefenderOutcome);
        ApplyWeaponStatusAfflictions(AttackOutcome);

        string ResultName = Negated ? "NEGATED" : AttackOutcome.Name;
        Events.Add(new ResolutionEvent("RESULT",
            $"{ResultName} · {Damage} damage · " +
            $"{_EngagementDefender.CurrentHealth}/{_EngagementDefender.MaxHealth} HP"));
        if (Destroyed)
            Events.Add(new ResolutionEvent("DESTROYED", _EngagementDefender.InstanceName));

        Resolution = new ResolutionView(AttackRoll, AllyRoll, DefenderRoll,
            Damage, Destroyed, ResultName, Events.ToArray());
        ValidationMessage = string.Empty;
        return true;
    }


    private AttackOutcome ResolveAttackOutcome(int Roll, int Modifier)
    {
        if (_AttackKind == CombatActionKind.Weapon &&
            _AttackSlotIndex >= 0 && _AttackSlotIndex < _EngagementAttacker.WeaponSlots.Length)
        {
            Definition_Weapon Weapon = _EngagementAttacker.WeaponSlots[_AttackSlotIndex].Weapon;
            return Weapon != null && Weapon.TryGetAttackOutcome(Roll,
                out AttackOutcome Outcome, Modifier) ? Outcome : null;
        }
        if (_AttackKind == CombatActionKind.Ability &&
            _AttackSlotIndex >= 0 && _AttackSlotIndex < _EngagementAttacker.Abilities.Length)
        {
            Definition_Ability Ability = _EngagementAttacker.Abilities[_AttackSlotIndex].Ability;
            return Ability != null && Ability.TryGetAttackOutcome(Roll,
                out AttackOutcome Outcome, Modifier) ? Outcome : null;
        }
        return null;
    }


    private DefenseOutcome ResolveDefenseOutcome(RuntimeUnit Unit,
        CombatActionKind Kind, int SlotIndex, int Roll)
    {
        if (Unit == null || Kind == CombatActionKind.None)
            return null;
        if (Kind == CombatActionKind.System && SlotIndex >= 0 &&
            SlotIndex < Unit.SystemSlots.Length)
        {
            SystemDefenseCapability Defense = Unit.SystemSlots[SlotIndex].System?.Defense;
            return Defense != null && Defense.TryGetDefenseOutcome(Roll,
                out DefenseOutcome Outcome) ? Outcome : null;
        }
        if (Kind == CombatActionKind.Ability && SlotIndex >= 0 &&
            SlotIndex < Unit.Abilities.Length)
        {
            Definition_Ability Ability = Unit.Abilities[SlotIndex].Ability;
            return Ability != null && Ability.TryGetDefenseOutcome(Roll,
                out DefenseOutcome Outcome) ? Outcome : null;
        }
        return null;
    }


    private bool SpendSelectedCharges(out string ValidationMessage)
    {
        if (_AttackKind == CombatActionKind.Weapon &&
            !_EngagementAttacker.WeaponSlots[_AttackSlotIndex].TrySpendCharges(1))
        {
            ValidationMessage = "The selected weapon no longer has a charge.";
            return false;
        }
        if (!SpendDefenseCharge(_SupportUnit, _SupportKind, _SupportSlotIndex) ||
            !SpendDefenseCharge(_EngagementDefender, _DefenseKind, _DefenseSlotIndex))
        {
            ValidationMessage = "A selected defensive system no longer has a charge.";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }


    private static bool SpendDefenseCharge(RuntimeUnit Unit,
        CombatActionKind Kind, int SlotIndex)
    {
        return Kind != CombatActionKind.System ||
            (Unit != null && SlotIndex >= 0 && SlotIndex < Unit.SystemSlots.Length &&
             (!Unit.SystemSlots[SlotIndex].UsesCharges ||
              Unit.SystemSlots[SlotIndex].TrySpendCharges(1)));
    }


    private void ApplyDefenseActions(RuntimeUnit Unit, DefenseOutcome Outcome)
    {
        if (Unit == null || Outcome?.Actions == null || !Unit.IsActive)
            return;
        _GameUnitEffects.TryApplyActions(Unit, Outcome.Actions, out _, out _);
    }


    private void ApplyWeaponStatusAfflictions(AttackOutcome Outcome)
    {
        Definition_Weapon Weapon = GetSelectedWeapon();
        if (Weapon?.StatusAfflictions == null || Outcome == null ||
            !_EngagementDefender.IsActive)
            return;
        OutcomeTrigger Trigger = Outcome.Result == OutcomeResult.Hit
            ? OutcomeTrigger.Hit : OutcomeTrigger.Miss;
        if (Outcome.HasAttackProperty(AttackProperty.Critical))
            Trigger |= OutcomeTrigger.Critical;
        int ChanceModifier = GetModifierTotal(_EngagementAttacker,
            ModifierType.StatusInfliction) - GetModifierTotal(_EngagementDefender,
            ModifierType.StatusProtection);
        for (int i = 0; i < Weapon.StatusAfflictions.Length; i++)
        {
            StatusAffliction Affliction = Weapon.StatusAfflictions[i];
            if (Affliction?.Status == null || (Affliction.Triggers & Trigger) == 0 ||
                UnityEngine.Random.Range(1, 101) >
                    Mathf.Clamp(Affliction.ChancePercent + ChanceModifier, 0, 100))
                continue;
            _EngagementDefender.ApplyStatus(Affliction.Status);
        }
    }


    private bool UsesD20(RuntimeUnit Unit, CombatActionKind Kind, int SlotIndex)
    {
        if (Unit == null)
            return false;
        if (Kind == CombatActionKind.Weapon && SlotIndex >= 0 &&
            SlotIndex < Unit.WeaponSlots.Length)
            return Unit.WeaponSlots[SlotIndex].Weapon?.UsesD20Roll == true;
        if (Kind == CombatActionKind.System && SlotIndex >= 0 &&
            SlotIndex < Unit.SystemSlots.Length)
            return Unit.SystemSlots[SlotIndex].System?.Defense?.UsesD20Roll == true;
        if (Kind == CombatActionKind.Ability && SlotIndex >= 0 &&
            SlotIndex < Unit.Abilities.Length)
            return Unit.Abilities[SlotIndex].Ability?.UsesD20Roll == true;
        return false;
    }


    private Definition_Weapon GetSelectedWeapon()
    {
        return _AttackKind == CombatActionKind.Weapon && _EngagementAttacker != null &&
            _AttackSlotIndex >= 0 && _AttackSlotIndex < _EngagementAttacker.WeaponSlots.Length
                ? _EngagementAttacker.WeaponSlots[_AttackSlotIndex].Weapon : null;
    }


    private static string GetDefenseActionName(RuntimeUnit Unit,
        CombatActionKind Kind, int SlotIndex)
    {
        if (Unit == null)
            return string.Empty;
        if (Kind == CombatActionKind.System && SlotIndex >= 0 &&
            SlotIndex < Unit.SystemSlots.Length)
            return Unit.SystemSlots[SlotIndex].System?.Name ?? string.Empty;
        if (Kind == CombatActionKind.Ability && SlotIndex >= 0 &&
            SlotIndex < Unit.Abilities.Length)
            return Unit.Abilities[SlotIndex].Ability?.Name ?? string.Empty;
        return string.Empty;
    }


    private string GetSelectedAttackName()
    {
        ActionOption[] Options = BuildAttackerOptions();
        return TryFindOption(Options, _AttackKind, _AttackSlotIndex,
            out ActionOption Option) ? Option.Name : string.Empty;
    }


    private static bool TryFindOption(ActionOption[] Options,
        CombatActionKind Kind, int SlotIndex, out ActionOption Found)
    {
        Found = null;
        if (Options == null)
            return false;
        for (int i = 0; i < Options.Length; i++)
            if (Options[i].Kind == Kind && Options[i].SlotIndex == SlotIndex)
            {
                Found = Options[i];
                return true;
            }
        return false;
    }


    private bool TryFindRuntimeUnit(RuntimeParticipant Participant,
        string RuntimeUnitID, out RuntimeUnit Unit)
    {
        Unit = null;
        return _GameUnits.TryGetRoster(Participant,
            out ParticipantUnitRoster Roster) &&
            Roster.TryGetRuntimeUnit(RuntimeUnitID, out Unit) && Unit != null;
    }


    private static string FormatRoll(int Roll) => Roll > 0 ? Roll.ToString() : "flat";


    private bool IsLegalAttackAbility(RuntimeUnit Attacker,
        Definition_Ability Ability, RuntimeUnit Defender, bool IsAbsoluteVisible)
    {
        if (Ability == null ||
            (Ability.UsablePhases & GameplayPhase.Battle) == 0 ||
            (Ability.CombatRoles & CombatRole.Attacker) == 0 ||
            Ability.OutcomeType != AbilityOutcomeType.Attack ||
            (Ability.TargetDomains & Defender.CurrentDomain) == 0 ||
            (Ability.ValidTargets & (IsAbsoluteVisible
                ? GameplayTarget.Enemy : GameplayTarget.Contact)) == 0 ||
            Ability.Pattern == null || Ability.Pattern.Mode != PatternMode.OriginPoint ||
            !_GameMap.TryGetUnitCoordinates(Defender, out Vector2Int[] Footprint))
            return false;
        return Overlaps(BuildPatternArea(Attacker, Ability.Pattern,
            GetModifierTotal(Attacker, ModifierType.AttackRange)), Footprint);
    }


    private HashSet<Vector2Int> BuildFireControlArea(RuntimeUnit Unit, Domain Domain)
    {
        HashSet<Vector2Int> Area = new HashSet<Vector2Int>();
        if (!_GameMap.TryGetUnitMovementPivotCoordinate(Unit, out Vector2Int Pivot))
            return Area;
        Vector2Int OriginOffset = Unit.Definition.OriginPoint -
            Unit.Definition.MovementPivot;
        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                OriginOffset, Unit.Facing, out Vector2Int RotatedOrigin))
            return Area;
        Vector2Int Origin = Pivot + RotatedOrigin;

        for (int i = 0; i < Unit.SystemSlots.Length; i++)
        {
            RuntimeSystemSlotState Slot = Unit.SystemSlots[i];
            Definition_System System = Slot?.System;
            if (System == null || System.FireControl == null ||
                !System.FireControl.Enabled ||
                (System.OperationalPhases & GameplayPhase.Battle) == 0 ||
                (System.Domains & Domain) == 0 ||
                System.Pattern == null ||
                System.Pattern.Mode != PatternMode.OriginPoint ||
                !Slot.HasCharges())
                continue;

            AddPattern(Area, System.Pattern, Origin, Unit.Facing);
        }

        ApplyRangeModifier(Area, GetModifierTotal(Unit, ModifierType.AttackRange));
        return Area;
    }


    private void CountLegalActions(RuntimeUnit Attacker, RuntimeUnit Defender,
        bool IsAbsoluteVisible, out int WeaponCount, out int AbilityCount)
    {
        WeaponCount = 0;
        AbilityCount = 0;
        if (!_GameMap.TryGetUnitCoordinates(Defender, out Vector2Int[] Footprint))
            return;

        HashSet<Vector2Int> FireControl =
            BuildFireControlArea(Attacker, Defender.CurrentDomain);
        bool FireControlReaches = Overlaps(FireControl, Footprint);
        for (int i = 0; i < Attacker.WeaponSlots.Length; i++)
        {
            RuntimeWeaponSlotState Slot = Attacker.WeaponSlots[i];
            if (Slot?.Weapon != null && Slot.HasCharges() && FireControlReaches &&
                (Slot.Weapon.TargetDomains & Defender.CurrentDomain) != 0)
                WeaponCount++;
        }

        for (int i = 0; i < Attacker.Abilities.Length; i++)
        {
            Definition_Ability Ability = Attacker.Abilities[i]?.Ability;
            if (Ability == null ||
                (Ability.UsablePhases & GameplayPhase.Battle) == 0 ||
                (Ability.CombatRoles & CombatRole.Attacker) == 0 ||
                Ability.OutcomeType != AbilityOutcomeType.Attack ||
                (Ability.TargetDomains & Defender.CurrentDomain) == 0 ||
                (Ability.ValidTargets & (IsAbsoluteVisible
                    ? GameplayTarget.Enemy : GameplayTarget.Contact)) == 0)
                continue;

            if (Ability.Pattern == null ||
                Ability.Pattern.Mode != PatternMode.OriginPoint)
                continue;
            HashSet<Vector2Int> AbilityArea = BuildPatternArea(Attacker,
                Ability.Pattern, GetModifierTotal(Attacker, ModifierType.AttackRange));
            if (Overlaps(AbilityArea, Footprint))
                AbilityCount++;
        }
    }


    private HashSet<Vector2Int> BuildPatternArea(RuntimeUnit Unit,
        Definition_Pattern Pattern, int RangeModifier)
    {
        HashSet<Vector2Int> Area = new HashSet<Vector2Int>();
        if (!_GameMap.TryGetUnitMovementPivotCoordinate(Unit, out Vector2Int Pivot))
            return Area;
        Vector2Int OriginOffset = Unit.Definition.OriginPoint -
            Unit.Definition.MovementPivot;
        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                OriginOffset, Unit.Facing, out Vector2Int RotatedOrigin))
            return Area;
        AddPattern(Area, Pattern, Pivot + RotatedOrigin, Unit.Facing);
        ApplyRangeModifier(Area, RangeModifier);
        return Area;
    }


    private void AddPattern(HashSet<Vector2Int> Area,
        Definition_Pattern Pattern, Vector2Int Origin, UnitFacing Facing)
    {
        PatternLayer[] Layers = Pattern?.Layers;
        if (Layers == null)
            return;
        for (int l = 0; l < Layers.Length; l++)
        {
            Vector2Int[] Offsets = Layers[l]?.Offsets;
            if (Offsets == null)
                continue;
            for (int i = 0; i < Offsets.Length; i++)
                if (UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Offsets[i], Facing, out Vector2Int Rotated) &&
                    _GameMap.IsInsideBounds(Origin + Rotated))
                    Area.Add(Origin + Rotated);
        }
    }


    private void ApplyRangeModifier(HashSet<Vector2Int> Area, int Modifier)
    {
        Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        for (int Step = 0; Step < Mathf.Abs(Modifier) && Area.Count > 0; Step++)
        {
            if (Modifier > 0)
            {
                HashSet<Vector2Int> Expanded = new HashSet<Vector2Int>(Area);
                foreach (Vector2Int Coordinate in Area)
                    for (int d = 0; d < Directions.Length; d++)
                        if (_GameMap.IsInsideBounds(Coordinate + Directions[d]))
                            Expanded.Add(Coordinate + Directions[d]);
                Area.Clear();
                Area.UnionWith(Expanded);
            }
            else
            {
                List<Vector2Int> Boundary = new List<Vector2Int>();
                foreach (Vector2Int Coordinate in Area)
                    for (int d = 0; d < Directions.Length; d++)
                        if (!Area.Contains(Coordinate + Directions[d]))
                        {
                            Boundary.Add(Coordinate);
                            break;
                        }
                for (int i = 0; i < Boundary.Count; i++)
                    Area.Remove(Boundary[i]);
            }
        }
    }


    private static int GetModifierTotal(RuntimeUnit Unit, ModifierType Type)
    {
        int Total = 0;
        for (int i = 0; i < Unit.ActiveModifiers.Count; i++)
        {
            GameplayModifier Modifier = Unit.ActiveModifiers[i]?.Modifier;
            if (Modifier != null && Modifier.Type == Type &&
                (Modifier.ApplicablePhases & GameplayPhase.Battle) != 0)
                Total += Modifier.Value;
        }
        return Total;
    }


    private static bool Overlaps(HashSet<Vector2Int> Area, Vector2Int[] Footprint)
    {
        for (int i = 0; i < Footprint.Length; i++)
            if (Area.Contains(Footprint[i]))
                return true;
        return false;
    }


    private static Vector2Int[] ToArray(HashSet<Vector2Int> Values)
    {
        Vector2Int[] Result = new Vector2Int[Values.Count];
        Values.CopyTo(Result);
        return Result;
    }


    private static bool IsSingleDomain(Domain Domain) =>
        Domain == Domain.Air || Domain == Domain.Surface ||
        Domain == Domain.Subsurface;


    private void ResetState(ParticipantState State)
    {
        State.AttackedUnits.Clear();
        State.Ended = false;
    }


    private void EndParticipant(ParticipantState State, out bool AllEnded)
    {
        State.Ended = true;
        AllEnded = _ParticipantA.Ended && _ParticipantB.Ended;
        if (AllEnded)
        {
            _PhaseActive = false;
            _ActiveParticipant = null;
            _SecondsRemaining = 0f;
        }
        else
        {
            _ActiveParticipant = ReferenceEquals(State, _ParticipantA)
                ? _ParticipantB.Participant : _ParticipantA.Participant;
            _SecondsRemaining = _SecondsPerParticipant;
        }
    }


    private void ClearEngagement()
    {
        _EngagementAttacker = null;
        _EngagementDefender = null;
        _EngagementBeganFromContact = false;
        _AttackKind = CombatActionKind.None;
        _AttackSlotIndex = -1;
        _SelectedAttackName = string.Empty;
        _DefenseKind = CombatActionKind.None;
        _DefenseSlotIndex = -1;
        _SupportUnit = null;
        _SupportKind = CombatActionKind.None;
        _SupportSlotIndex = -1;
        _AttackerLocked = false;
        _DefenderLocked = false;
        _Resolution = null;
    }


    private bool TryGetState(RuntimeParticipant Participant,
        out ParticipantState State)
    {
        State = ReferenceEquals(Participant, _ParticipantA?.Participant)
            ? _ParticipantA
            : ReferenceEquals(Participant, _ParticipantB?.Participant)
                ? _ParticipantB : null;
        return IsInitialized && State != null;
    }
}
