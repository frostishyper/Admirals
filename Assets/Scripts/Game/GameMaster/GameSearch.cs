using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative Search-phase state. Controllers submit a Unit, sensor slot and
// orientation; this class owns quota, eligibility, rolls and knowledge changes.
public sealed class GameSearch
{
    public sealed class SearchTile
    {
        public Vector2Int Coordinate { get; }
        public int Strength { get; }

        internal SearchTile(Vector2Int Coordinate, int Strength)
        {
            this.Coordinate = Coordinate;
            this.Strength = Strength;
        }
    }

    public sealed class ContactTile
    {
        public Vector2Int Coordinate { get; }
        public Domain Domain { get; }

        internal ContactTile(Vector2Int Coordinate, Domain Domain)
        {
            this.Coordinate = Coordinate;
            this.Domain = Domain;
        }
    }

    public sealed class CommitResult
    {
        public string RuntimeUnitID { get; }
        public int SystemSlotIndex { get; }
        public UnitFacing SearchFacing { get; }
        public SearchTile[] Tiles { get; }
        public ContactTile[] Contacts { get; }

        internal CommitResult(string RuntimeUnitID, int SystemSlotIndex,
            UnitFacing SearchFacing, SearchTile[] Tiles, ContactTile[] Contacts)
        {
            this.RuntimeUnitID = RuntimeUnitID ?? string.Empty;
            this.SystemSlotIndex = SystemSlotIndex;
            this.SearchFacing = SearchFacing;
            this.Tiles = Tiles ?? Array.Empty<SearchTile>();
            this.Contacts = Contacts ?? Array.Empty<ContactTile>();
        }
    }

    public sealed class UnitView
    {
        public string RuntimeUnitID { get; }
        public bool HasSearched { get; }

        internal UnitView(string RuntimeUnitID, bool HasSearched)
        {
            this.RuntimeUnitID = RuntimeUnitID ?? string.Empty;
            this.HasSearched = HasSearched;
        }
    }

    public sealed class ParticipantView
    {
        public bool IsSearchPhaseActive { get; }
        public bool SearchEnded { get; }
        public bool CanAct { get; }
        public bool CanEndSearch { get; }
        public int SearchLimit { get; }
        public int SearchesCommitted { get; }
        public float SecondsRemaining { get; }
        public UnitView[] Units { get; }

        internal ParticipantView(bool IsSearchPhaseActive, bool SearchEnded,
            bool CanAct, int SearchLimit, int SearchesCommitted,
            float SecondsRemaining, UnitView[] Units)
        {
            this.IsSearchPhaseActive = IsSearchPhaseActive;
            this.SearchEnded = SearchEnded;
            this.CanAct = CanAct;
            CanEndSearch = CanAct && SearchesCommitted >= SearchLimit;
            this.SearchLimit = SearchLimit;
            this.SearchesCommitted = SearchesCommitted;
            this.SecondsRemaining = SecondsRemaining;
            this.Units = Units ?? Array.Empty<UnitView>();
        }
    }

    private sealed class ParticipantState
    {
        public readonly RuntimeParticipant Participant;
        public readonly HashSet<RuntimeUnit> SearchedUnits = new HashSet<RuntimeUnit>();
        public readonly HashSet<string> UsedAbilities = new HashSet<string>();
        public int SearchLimit;
        public bool Ended;

        public ParticipantState(RuntimeParticipant Participant)
        {
            this.Participant = Participant;
        }
    }

    private GameMap _GameMap;
    private GameUnits _GameUnits;
    private GameKnowledge _GameKnowledge;
    private ParticipantState _ParticipantA;
    private ParticipantState _ParticipantB;
    private RuntimeParticipant _ActiveParticipant;
    private float _SecondsPerParticipant;
    private float _SecondsRemaining;
    private bool _PhaseActive;

    public bool IsInitialized { get; private set; }

    internal bool Initialize(GameMap GameMap, GameUnits GameUnits,
        GameKnowledge GameKnowledge, RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB, out string ValidationMessage)
    {
        IsInitialized = false;
        if (GameMap == null || !GameMap.Initialized ||
            GameUnits == null || !GameUnits.Initialized ||
            GameKnowledge == null || !GameKnowledge.IsInitialized ||
            ParticipantA == null || ParticipantB == null ||
            ReferenceEquals(ParticipantA, ParticipantB))
        {
            ValidationMessage =
                "Search requires initialized Map, Unit, knowledge and participant state.";
            return false;
        }

        _GameMap = GameMap;
        _GameUnits = GameUnits;
        _GameKnowledge = GameKnowledge;
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
             !ReferenceEquals(FirstParticipant, _ParticipantB.Participant)) ||
            !_GameUnits.TryGetRoster(_ParticipantA.Participant, out ParticipantUnitRoster ARoster) ||
            !_GameUnits.TryGetRoster(_ParticipantB.Participant, out ParticipantUnitRoster BRoster))
        {
            ValidationMessage = "Search could not resolve its participants and rosters.";
            return false;
        }

        ResetState(_ParticipantA, ARoster);
        ResetState(_ParticipantB, BRoster);
        _ActiveParticipant = FirstParticipant;
        _SecondsPerParticipant = Mathf.Max(1f, SecondsPerParticipant);
        _SecondsRemaining = _SecondsPerParticipant;
        _PhaseActive = true;
        ValidationMessage = string.Empty;
        return true;
    }

    internal bool TryGetView(RuntimeParticipant Participant, out ParticipantView View)
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
                Units.Add(new UnitView(Unit.RuntimeID, State.SearchedUnits.Contains(Unit)));
        }

        bool CanAct = _PhaseActive && !State.Ended &&
            ReferenceEquals(Participant, _ActiveParticipant);
        View = new ParticipantView(_PhaseActive, State.Ended, CanAct,
            State.SearchLimit, State.SearchedUnits.Count,
            _SecondsRemaining, Units.ToArray());
        return true;
    }

    internal bool TryGetPattern(RuntimeParticipant Participant,
        string RuntimeUnitID, int SystemSlotIndex, UnitFacing SearchFacing,
        out SearchTile[] Tiles, out string ValidationMessage)
    {
        Tiles = Array.Empty<SearchTile>();
        if (!TryGetSensor(Participant, RuntimeUnitID, SystemSlotIndex,
                out RuntimeUnit Unit, out RuntimeSystemSlotState Slot,
                out Vector2Int Origin, out ValidationMessage))
            return false;

        int SearchModifier = GetModifierTotal(Unit, ModifierType.Search);
        Dictionary<Vector2Int, int> StrengthByCoordinate =
            new Dictionary<Vector2Int, int>();
        PatternLayer[] Layers = Slot.System.Pattern.Layers;
        for (int l = 0; l < Layers.Length; l++)
        {
            PatternLayer Layer = Layers[l];
            if (Layer?.Offsets == null)
                continue;
            for (int o = 0; o < Layer.Offsets.Length; o++)
            {
                if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Layer.Offsets[o], SearchFacing, out Vector2Int Rotated))
                    continue;
                Vector2Int Coordinate = Origin + Rotated;
                if (_GameMap.IsInsideBounds(Coordinate))
                {
                    int Strength = Layer.Strength + SearchModifier;
                    if (!StrengthByCoordinate.TryGetValue(
                            Coordinate, out int Existing) || Strength > Existing)
                        StrengthByCoordinate[Coordinate] = Strength;
                }
            }
        }

        List<SearchTile> Result = new List<SearchTile>();
        foreach (KeyValuePair<Vector2Int, int> Entry in StrengthByCoordinate)
            Result.Add(new SearchTile(Entry.Key, Entry.Value));
        Tiles = Result.ToArray();
        ValidationMessage = Tiles.Length > 0
            ? string.Empty
            : "The selected sensor has no in-bounds Search tiles.";
        return Tiles.Length > 0;
    }

    internal bool TryCommitSearch(RuntimeParticipant Participant,
        string RuntimeUnitID, int SystemSlotIndex, UnitFacing SearchFacing,
        out CommitResult Result, out string ValidationMessage)
    {
        Result = null;
        if (!TryGetPattern(Participant, RuntimeUnitID, SystemSlotIndex,
                SearchFacing, out SearchTile[] Tiles, out ValidationMessage) ||
            !TryGetSensor(Participant, RuntimeUnitID, SystemSlotIndex,
                out RuntimeUnit Unit, out RuntimeSystemSlotState Slot,
                out _, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
            return false;

        if (State.SearchedUnits.Contains(Unit))
        {
            ValidationMessage = "That Unit has already searched this phase.";
            return false;
        }
        if (State.SearchedUnits.Count >= State.SearchLimit)
        {
            ValidationMessage = "This participant has reached the Search limit.";
            return false;
        }

        Dictionary<Vector2Int, int> Strengths = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < Tiles.Length; i++)
            Strengths[Tiles[i].Coordinate] = Tiles[i].Strength;

        ParticipantUnitRoster EnemyRoster = ReferenceEquals(
            Participant, _ParticipantA.Participant)
                ? _GameUnits.ParticipantB
                : _GameUnits.ParticipantA;
        List<GameKnowledge.SearchContactRecord> KnowledgeContacts =
            new List<GameKnowledge.SearchContactRecord>();
        List<ContactTile> ContactTiles = new List<ContactTile>();

        for (int i = 0; i < EnemyRoster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Enemy = EnemyRoster.RuntimeUnits[i];
            if (Enemy == null || !Enemy.IsActive ||
                (Slot.System.Domains & Enemy.CurrentDomain) == 0 ||
                _GameKnowledge.IsUnitVisibleToParticipant(Participant, Enemy) ||
                !_GameMap.TryGetUnitCoordinates(Enemy, out Vector2Int[] Footprint))
                continue;

            int Strongest = int.MinValue;
            List<Vector2Int> HitCoordinates = new List<Vector2Int>();
            for (int f = 0; f < Footprint.Length; f++)
            {
                if (Strengths.TryGetValue(Footprint[f], out int Strength))
                {
                    Strongest = Mathf.Max(Strongest, Strength);
                    HitCoordinates.Add(Footprint[f]);
                }
            }
            if (HitCoordinates.Count == 0 || !PercentCheck(Strongest))
                continue;

            int Overflow = Mathf.Max(0, Strongest - 100);
            int Stealth = (Enemy.Definition != null ? Enemy.Definition.Stealth : 0) +
                GetModifierTotal(Enemy, ModifierType.Stealth);
            int EffectiveStealth = Mathf.Max(0, Stealth - Overflow);
            if (PercentCheck(EffectiveStealth))
                continue;

            for (int h = 0; h < HitCoordinates.Count; h++)
            {
                Vector2Int Coordinate = HitCoordinates[h];
                KnowledgeContacts.Add(new GameKnowledge.SearchContactRecord(
                    Enemy, Coordinate, Enemy.CurrentDomain));
                ContactTiles.Add(new ContactTile(Coordinate, Enemy.CurrentDomain));
            }
        }

        if (Slot.UsesCharges && !Slot.TrySpendCharges(1))
        {
            ValidationMessage = $"Sensor '{Slot.System.Name}' could not spend its charge.";
            return false;
        }

        Vector2Int[] SearchedCoordinates = new Vector2Int[Tiles.Length];
        for (int i = 0; i < Tiles.Length; i++)
            SearchedCoordinates[i] = Tiles[i].Coordinate;
        _GameKnowledge.RecordSearchOutcome(Participant, SearchedCoordinates,
            KnowledgeContacts.ToArray());
        State.SearchedUnits.Add(Unit);
        Result = new CommitResult(RuntimeUnitID, SystemSlotIndex,
            SearchFacing, Tiles, ContactTiles.ToArray());
        ValidationMessage = string.Empty;
        return true;
    }

    internal bool CanUseAbility(RuntimeParticipant Participant,
        string RuntimeUnitID, int AbilitySlotIndex, out string ValidationMessage)
    {
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
            return false;
        string Key = $"{RuntimeUnitID}:{AbilitySlotIndex}";
        if (State.UsedAbilities.Contains(Key))
        {
            ValidationMessage = "That Ability has already been used during Search.";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }

    internal void CommitAbilityUse(RuntimeParticipant Participant,
        string RuntimeUnitID, int AbilitySlotIndex)
    {
        if (TryGetState(Participant, out ParticipantState State))
            State.UsedAbilities.Add($"{RuntimeUnitID}:{AbilitySlotIndex}");
    }

    internal bool TryEndSearch(RuntimeParticipant Participant,
        out bool AllParticipantsEnded, out string ValidationMessage)
    {
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
        {
            AllParticipantsEnded = false;
            return false;
        }
        if (State.SearchedUnits.Count < State.SearchLimit)
        {
            AllParticipantsEnded = false;
            ValidationMessage = "All required searches must be used before ending Search.";
            return false;
        }
        EndParticipant(State, out AllParticipantsEnded);
        ValidationMessage = string.Empty;
        return true;
    }

    internal bool Tick(float DeltaSeconds, out RuntimeParticipant TimedOutParticipant,
        out bool AllParticipantsEnded)
    {
        TimedOutParticipant = null;
        AllParticipantsEnded = false;
        if (!_PhaseActive || _ActiveParticipant == null)
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
        if (!_PhaseActive || !TryGetState(Participant, out ParticipantState State) ||
            State.Ended || !ReferenceEquals(Participant, _ActiveParticipant))
        {
            ValidationMessage = "Search is not accepting actions from this participant.";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }

    private bool TryGetSensor(RuntimeParticipant Participant, string RuntimeUnitID,
        int SystemSlotIndex, out RuntimeUnit Unit,
        out RuntimeSystemSlotState Slot, out Vector2Int Origin,
        out string ValidationMessage)
    {
        Unit = null;
        Slot = null;
        Origin = default;
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster) ||
            !Roster.TryGetRuntimeUnit(RuntimeUnitID, out Unit) ||
            Unit == null || !Unit.IsActive ||
            !_GameMap.TryGetUnitMovementPivotCoordinate(Unit, out Vector2Int Pivot))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "The requested Unit is not active on the Map.";
            return false;
        }
        if (!TryGetState(Participant, out ParticipantState State) ||
            State.SearchedUnits.Contains(Unit))
        {
            ValidationMessage = "That Unit has already searched this phase.";
            return false;
        }
        if (State.SearchedUnits.Count >= State.SearchLimit)
        {
            ValidationMessage = "This participant has reached the Search limit.";
            return false;
        }
        if (SystemSlotIndex < 0 || SystemSlotIndex >= Unit.SystemSlots.Length ||
            Unit.SystemSlots[SystemSlotIndex]?.System == null)
        {
            ValidationMessage = "The requested sensor slot is empty or does not exist.";
            return false;
        }
        Slot = Unit.SystemSlots[SystemSlotIndex];
        Definition_System System = Slot.System;
        if ((System.OperationalPhases & GameplayPhase.Search) == 0 ||
            System.Search == null || !System.Search.Enabled ||
            System.Pattern == null || System.Pattern.Mode != PatternMode.OriginPoint ||
            System.Domains == Domain.None || !Slot.HasCharges())
        {
            ValidationMessage = $"System '{System.Name}' is not an available Search sensor.";
            return false;
        }
        Vector2Int Offset = Unit.Definition.OriginPoint - Unit.Definition.MovementPivot;
        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                Offset, Unit.Facing, out Vector2Int RotatedOffset))
        {
            ValidationMessage = "The searching Unit has an invalid facing.";
            return false;
        }
        Origin = Pivot + RotatedOffset;
        ValidationMessage = string.Empty;
        return true;
    }

    private void ResetState(ParticipantState State, ParticipantUnitRoster Roster)
    {
        State.SearchedUnits.Clear();
        State.UsedAbilities.Clear();
        State.Ended = false;
        int Active = 0;
        int Eligible = 0;
        for (int i = 0; i < Roster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Unit = Roster.RuntimeUnits[i];
            if (Unit == null || !Unit.IsActive || !_GameMap.IsUnitOnMap(Unit))
                continue;
            Active++;
            if (HasAvailableSensor(Unit))
                Eligible++;
        }
        State.SearchLimit = Mathf.Min((Active + 1) / 2, Eligible);
    }

    private static bool HasAvailableSensor(RuntimeUnit Unit)
    {
        for (int i = 0; i < Unit.SystemSlots.Length; i++)
        {
            RuntimeSystemSlotState Slot = Unit.SystemSlots[i];
            Definition_System System = Slot?.System;
            if (System != null && (System.OperationalPhases & GameplayPhase.Search) != 0 &&
                System.Search != null && System.Search.Enabled &&
                System.Pattern != null && System.Pattern.Mode == PatternMode.OriginPoint &&
                System.Domains != Domain.None && Slot.HasCharges())
                return true;
        }
        return false;
    }

    private static int GetModifierTotal(RuntimeUnit Unit, ModifierType Type)
    {
        int Total = 0;
        if (Unit == null)
            return Total;
        for (int i = 0; i < Unit.ActiveModifiers.Count; i++)
        {
            RuntimeModifierState State = Unit.ActiveModifiers[i];
            GameplayModifier Modifier = State?.Modifier;
            if (Modifier != null && Modifier.Type == Type &&
                (Modifier.ApplicablePhases & GameplayPhase.Search) != 0)
                Total += Modifier.Value;
        }
        return Total;
    }

    private static bool PercentCheck(int Chance)
    {
        if (Chance <= 0)
            return false;
        if (Chance >= 100)
            return true;
        return UnityEngine.Random.Range(1, 101) <= Chance;
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

    private bool TryGetState(RuntimeParticipant Participant, out ParticipantState State)
    {
        State = ReferenceEquals(Participant, _ParticipantA?.Participant)
            ? _ParticipantA
            : ReferenceEquals(Participant, _ParticipantB?.Participant)
                ? _ParticipantB : null;
        return IsInitialized && State != null;
    }
}
