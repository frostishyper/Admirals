using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative Pre-Battle turn state and Deployment-System boundary.
// Controllers submit plain Unit/System/tile data; this class validates the
// active participant, authored phase/capability, range, charges and Map truth.
public sealed class GamePreBattle
{
    public sealed class ParticipantView
    {
        public bool IsPreBattlePhaseActive { get; }
        public bool PreBattleEnded { get; }
        public bool CanAct { get; }
        public int ActionsCommitted { get; }
        public float SecondsRemaining { get; }


        internal ParticipantView(
            bool IsPreBattlePhaseActive,
            bool PreBattleEnded,
            bool CanAct,
            int ActionsCommitted,
            float SecondsRemaining
        )
        {
            this.IsPreBattlePhaseActive = IsPreBattlePhaseActive;
            this.PreBattleEnded = PreBattleEnded;
            this.CanAct = CanAct;
            this.ActionsCommitted = ActionsCommitted;
            this.SecondsRemaining = SecondsRemaining;
        }
    }


    private sealed class ParticipantState
    {
        public readonly RuntimeParticipant Participant;
        public readonly HashSet<string> UsedAbilities =
            new HashSet<string>();

        public bool Ended;
        public int ActionsCommitted;


        public ParticipantState(RuntimeParticipant Participant)
        {
            this.Participant = Participant;
        }
    }


    private GameMap _GameMap;
    private GameUnits _GameUnits;
    private GameUnitEffects _GameUnitEffects;
    private ParticipantState _ParticipantA;
    private ParticipantState _ParticipantB;
    private RuntimeParticipant _ActiveParticipant;
    private bool _PhaseActive;
    private float _SecondsPerParticipant;
    private float _SecondsRemaining;
    private int _NextMineSequence;


    public bool IsInitialized { get; private set; }


    internal bool Initialize(
        GameMap GameMap,
        GameUnits GameUnits,
        GameUnitEffects GameUnitEffects,
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB,
        out string ValidationMessage
    )
    {
        IsInitialized = false;


        if (GameMap == null ||
            !GameMap.Initialized ||
            GameUnits == null ||
            !GameUnits.Initialized ||
            GameUnitEffects == null ||
            !GameUnitEffects.Initialized ||
            ParticipantA == null ||
            ParticipantB == null ||
            ReferenceEquals(ParticipantA, ParticipantB))
        {
            ValidationMessage =
                "Pre-Battle requires initialized Map, Unit and participant state.";

            return false;
        }


        _GameMap = GameMap;
        _GameUnits = GameUnits;
        _GameUnitEffects = GameUnitEffects;
        _ParticipantA = new ParticipantState(ParticipantA);
        _ParticipantB = new ParticipantState(ParticipantB);
        _PhaseActive = false;
        _NextMineSequence = 0;
        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool BeginPhase(
        RuntimeParticipant FirstParticipant,
        float SecondsPerParticipant,
        out string ValidationMessage
    )
    {
        if (!IsInitialized ||
            (!ReferenceEquals(FirstParticipant, _ParticipantA.Participant) &&
             !ReferenceEquals(FirstParticipant, _ParticipantB.Participant)))
        {
            ValidationMessage =
                "Pre-Battle first participant does not belong to this Match.";

            return false;
        }


        ResetState(_ParticipantA);
        ResetState(_ParticipantB);
        _ActiveParticipant = FirstParticipant;
        _SecondsPerParticipant = Mathf.Max(1f, SecondsPerParticipant);
        _SecondsRemaining = _SecondsPerParticipant;
        _PhaseActive = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetView(
        RuntimeParticipant Participant,
        out ParticipantView View
    )
    {
        View = null;


        if (!TryGetState(Participant, out ParticipantState State))
            return false;


        View = new ParticipantView(
            _PhaseActive,
            State.Ended,
            _PhaseActive &&
                !State.Ended &&
                ReferenceEquals(Participant, _ActiveParticipant),
            State.ActionsCommitted,
            _SecondsRemaining
        );

        return true;
    }


    internal bool CanAcceptParticipantAction(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!_PhaseActive ||
            !TryGetState(Participant, out ParticipantState State))
        {
            ValidationMessage =
                "Pre-Battle is not active for this participant.";

            return false;
        }


        if (State.Ended)
        {
            ValidationMessage =
                "This participant has already ended Pre-Battle.";

            return false;
        }


        if (!ReferenceEquals(Participant, _ActiveParticipant))
        {
            ValidationMessage =
                "The other participant currently has Pre-Battle priority.";

            return false;
        }


        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetMineDeploymentArea(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        out Vector2Int[] Coordinates,
        out string ValidationMessage
    )
    {
        Coordinates = Array.Empty<Vector2Int>();


        if (!TryGetMineSystem(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                out _,
                out RuntimeSystemSlotState Slot,
                out Vector2Int Origin,
                out ValidationMessage))
        {
            return false;
        }


        List<Vector2Int> Legal = new List<Vector2Int>();
        Definition_Pattern Pattern = Slot.System.Pattern;


        if (Pattern.Layers == null)
        {
            ValidationMessage =
                $"System '{Slot.System.Name}' has no authored deployment tiles.";
            return false;
        }


        for (int LayerIndex = 0;
            LayerIndex < Pattern.Layers.Length;
            LayerIndex++)
        {
            PatternLayer Layer = Pattern.Layers[LayerIndex];
            if (Layer?.Offsets == null)
                continue;


            for (int OffsetIndex = 0;
                OffsetIndex < Layer.Offsets.Length;
                OffsetIndex++)
            {
                if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Layer.Offsets[OffsetIndex],
                        GetUnitFacing(Participant, RuntimeUnitID),
                        out Vector2Int RotatedOffset))
                {
                    continue;
                }


                Vector2Int Coordinate = Origin + RotatedOffset;
                string PreviewID = $"mine_preview_{_NextMineSequence + 1}";


                if (_GameMap.ValidateMinePlacement(
                        PreviewID,
                        Participant,
                        Slot.System,
                        Slot.System.MineDeployment.TriggerDomains,
                        Coordinate,
                        out _))
                {
                    Legal.Add(Coordinate);
                }
            }
        }


        Coordinates = Legal.ToArray();
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool ValidateMineDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        Vector2Int Coordinate,
        out string ValidationMessage
    )
    {
        if (!TryGetMineSystem(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                out RuntimeUnit Unit,
                out RuntimeSystemSlotState Slot,
                out Vector2Int Origin,
                out ValidationMessage))
        {
            return false;
        }


        if (!PatternContains(
                Slot.System.Pattern,
                Unit.Facing,
                Origin,
                Coordinate))
        {
            ValidationMessage =
                $"Tile {Coordinate} is outside '{Slot.System.Name}' deployment range.";

            return false;
        }


        return _GameMap.ValidateMinePlacement(
            $"mine_preview_{_NextMineSequence + 1}",
            Participant,
            Slot.System,
            Slot.System.MineDeployment.TriggerDomains,
            Coordinate,
            out ValidationMessage
        );
    }


    internal bool TryCommitMineDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        Vector2Int Coordinate,
        out string ValidationMessage
    )
    {
        if (!ValidateMineDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                Coordinate,
                out ValidationMessage) ||
            !TryGetMineSystem(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                out _,
                out RuntimeSystemSlotState Slot,
                out _,
                out ValidationMessage))
        {
            return false;
        }


        string MineInstanceID =
            $"mine_{Participant.Participant}_{++_NextMineSequence}";


        if (!_GameMap.TryPlaceMine(
                MineInstanceID,
                Participant,
                Slot.System,
                Slot.System.MineDeployment.TriggerDomains,
                Coordinate,
                out ValidationMessage))
        {
            _NextMineSequence--;
            return false;
        }


        if (!Slot.TrySpendCharges(1))
        {
            _GameMap.TryRemoveMine(MineInstanceID, out _);
            _NextMineSequence--;
            ValidationMessage =
                $"System '{Slot.System.Name}' could not spend its charge.";

            return false;
        }


        TryGetState(Participant, out ParticipantState State);
        State.ActionsCommitted++;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetUnitDeploymentArea(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        UnitFacing DeployedFacing,
        out Vector2Int[] Coordinates,
        out string ValidationMessage
    )
    {
        Coordinates = Array.Empty<Vector2Int>();


        if (!TryGetUnitDeploymentSystem(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                out RuntimeUnit Carrier,
                out RuntimeSystemSlotState Slot,
                out _,
                out Vector2Int Origin,
                out ValidationMessage))
        {
            return false;
        }


        List<Vector2Int> Legal = new List<Vector2Int>();
        Definition_Pattern Pattern = Slot.System.Pattern;


        for (int LayerIndex = 0;
            LayerIndex < Pattern.Layers.Length;
            LayerIndex++)
        {
            PatternLayer Layer = Pattern.Layers[LayerIndex];
            if (Layer?.Offsets == null)
                continue;


            for (int OffsetIndex = 0;
                OffsetIndex < Layer.Offsets.Length;
                OffsetIndex++)
            {
                if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Layer.Offsets[OffsetIndex],
                        Carrier.Facing,
                        out Vector2Int RotatedOffset))
                {
                    continue;
                }


                Vector2Int Coordinate = Origin + RotatedOffset;

                if (TryPrepareUnitDeployment(
                        Participant,
                        RuntimeUnitID,
                        SystemSlotIndex,
                        CarriedUnitIndex,
                        Coordinate,
                        DeployedFacing,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    Legal.Add(Coordinate);
                }
            }
        }


        Coordinates = Legal.ToArray();
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool ValidateUnitDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        return TryPrepareUnitDeployment(
            Participant,
            RuntimeUnitID,
            SystemSlotIndex,
            CarriedUnitIndex,
            MovementPivotCoordinate,
            Facing,
            out _,
            out _,
            out _,
            out ValidationMessage
        );
    }


    internal bool TryCommitUnitDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out RuntimeUnit DeployedUnit,
        out string ValidationMessage
    )
    {
        DeployedUnit = null;


        if (!TryPrepareUnitDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                MovementPivotCoordinate,
                Facing,
                out RuntimeSystemSlotState Slot,
                out PreparedCarriedUnitDeployment PreparedUnit,
                out PreparedMapUnitDeployment PreparedMap,
                out ValidationMessage))
        {
            return false;
        }


        PreparedUnit.Roster.CommitPreparedCarriedUnitDeployment(PreparedUnit);
        _GameMap.CommitPreparedUnitPlacementBatch(PreparedMap);
        DeployedUnit = PreparedUnit.RuntimeUnit;


        if (Slot.UsesCharges && !Slot.TrySpendCharges(1))
        {
            throw new InvalidOperationException(
                $"System '{Slot.System.Name}' failed to spend a validated deployment charge."
            );
        }


        if (PreparedMap.TriggeredMines.Length > 0 &&
            !_GameUnitEffects.ResolveTriggeredMines(
                DeployedUnit,
                PreparedMap.TriggeredMines,
                out _,
                out string MineResolutionMessage))
        {
            Debug.LogError(
                $"Mine resolution failed for deployed Unit '{DeployedUnit.InstanceName}': {MineResolutionMessage}"
            );
        }


        TryGetState(Participant, out ParticipantState State);
        State.ActionsCommitted++;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool CanUseAbility(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int AbilitySlotIndex,
        out string ValidationMessage
    )
    {
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
        {
            return false;
        }


        string Key = AbilityKey(RuntimeUnitID, AbilitySlotIndex);
        if (State.UsedAbilities.Contains(Key))
        {
            ValidationMessage =
                "That Ability has already been used during this Pre-Battle phase.";

            return false;
        }


        ValidationMessage = string.Empty;
        return true;
    }


    internal void CommitAbilityUse(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int AbilitySlotIndex
    )
    {
        if (TryGetState(Participant, out ParticipantState State))
        {
            State.UsedAbilities.Add(
                AbilityKey(RuntimeUnitID, AbilitySlotIndex)
            );

            State.ActionsCommitted++;
        }
    }


    internal bool TryEndPreBattle(
        RuntimeParticipant Participant,
        out bool AllParticipantsEnded,
        out string ValidationMessage
    )
    {
        AllParticipantsEnded = false;


        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
        {
            return false;
        }


        State.Ended = true;


        if (_ParticipantA.Ended && _ParticipantB.Ended)
        {
            _PhaseActive = false;
            _ActiveParticipant = null;
            _SecondsRemaining = 0f;
            AllParticipantsEnded = true;
        }
        else
        {
            _ActiveParticipant = ReferenceEquals(
                    Participant,
                    _ParticipantA.Participant)
                ? _ParticipantB.Participant
                : _ParticipantA.Participant;

            _SecondsRemaining = _SecondsPerParticipant;
        }


        ValidationMessage = string.Empty;
        return true;
    }


    internal bool Tick(
        float DeltaSeconds,
        out RuntimeParticipant TimedOutParticipant,
        out bool AllParticipantsEnded
    )
    {
        TimedOutParticipant = null;
        AllParticipantsEnded = false;


        if (!_PhaseActive || _ActiveParticipant == null)
            return false;


        _SecondsRemaining = Mathf.Max(
            0f,
            _SecondsRemaining - Mathf.Max(0f, DeltaSeconds)
        );


        if (_SecondsRemaining > 0f)
            return false;


        TimedOutParticipant = _ActiveParticipant;
        return TryEndPreBattle(
            TimedOutParticipant,
            out AllParticipantsEnded,
            out _
        );
    }


    private bool TryPrepareUnitDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out RuntimeSystemSlotState Slot,
        out PreparedCarriedUnitDeployment PreparedUnit,
        out PreparedMapUnitDeployment PreparedMap,
        out string ValidationMessage
    )
    {
        Slot = null;
        PreparedUnit = null;
        PreparedMap = null;


        if (!TryGetUnitDeploymentSystem(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                out RuntimeUnit Carrier,
                out Slot,
                out RuntimeCarriedUnit CarriedUnit,
                out Vector2Int Origin,
                out ValidationMessage))
        {
            return false;
        }


        if (!PatternContains(
                Slot.System.Pattern,
                Carrier.Facing,
                Origin,
                MovementPivotCoordinate))
        {
            ValidationMessage =
                $"Tile {MovementPivotCoordinate} is outside '{Slot.System.Name}' deployment range.";
            return false;
        }


        if (!_GameUnits.TryGetRoster(
                Participant,
                out ParticipantUnitRoster Roster) ||
            !Roster.TryPrepareCarriedUnitDeployment(
                CarriedUnit,
                Facing,
                out PreparedUnit,
                out ValidationMessage))
        {
            return false;
        }


        if (!UnitFootprintGeometry.TryBuildPlacement(
                PreparedUnit.RuntimeUnit.Definition,
                MovementPivotCoordinate,
                Facing,
                out RuntimeUnitTilePlacement[] Placements,
                out ValidationMessage))
        {
            return false;
        }


        RuntimeUnitPlacementRequest[] Requests =
        {
            new RuntimeUnitPlacementRequest(
                PreparedUnit.RuntimeUnit,
                Placements,
                MapTileCondition.None
            )
        };


        return _GameMap.TryPrepareUnitPlacementBatch(
            Requests,
            out PreparedMap,
            out ValidationMessage
        );
    }


    private bool TryGetUnitDeploymentSystem(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        out RuntimeUnit Carrier,
        out RuntimeSystemSlotState Slot,
        out RuntimeCarriedUnit CarriedUnit,
        out Vector2Int Origin,
        out string ValidationMessage
    )
    {
        Carrier = null;
        Slot = null;
        CarriedUnit = null;
        Origin = default;


        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster) ||
            !Roster.TryGetRuntimeUnit(RuntimeUnitID, out Carrier))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "The requested carrier does not belong to this participant.";

            return false;
        }


        if (!Carrier.IsActive ||
            !_GameMap.TryGetUnitMovementPivotCoordinate(Carrier, out Vector2Int Pivot))
        {
            ValidationMessage = "The requested carrier is not active on the Map.";
            return false;
        }


        if (SystemSlotIndex < 0 ||
            SystemSlotIndex >= Carrier.SystemSlots.Length ||
            Carrier.SystemSlots[SystemSlotIndex] == null ||
            Carrier.SystemSlots[SystemSlotIndex].System == null)
        {
            ValidationMessage = "The requested System slot is empty or does not exist.";
            return false;
        }


        Slot = Carrier.SystemSlots[SystemSlotIndex];
        Definition_System System = Slot.System;


        if (!System.Validate(out string SystemMessage))
        {
            ValidationMessage =
                $"System '{System.name}' is invalid: {SystemMessage}";
            return false;
        }


        if ((System.OperationalPhases & GameplayPhase.PreBattle) == 0 ||
            System.UnitDeployment == null ||
            !System.UnitDeployment.Enabled ||
            System.Pattern == null ||
            System.Pattern.Mode != PatternMode.OriginPoint)
        {
            ValidationMessage =
                $"System '{System.Name}' is not a Pre-Battle Unit Deployment System.";
            return false;
        }


        if (!Slot.HasCharges())
        {
            ValidationMessage =
                $"System '{System.Name}' has no charges remaining.";
            return false;
        }


        if (CarriedUnitIndex < 0 ||
            CarriedUnitIndex >= Slot.CarriedUnits.Count)
        {
            ValidationMessage = "The requested carried Unit does not exist.";
            return false;
        }


        CarriedUnit = Slot.CarriedUnits[CarriedUnitIndex];


        if (CarriedUnit == null ||
            CarriedUnit.State != RuntimeCarriedUnitState.Carried ||
            CarriedUnit.Unit == null ||
            !System.UnitDeployment.IsEligibleFor(CarriedUnit.Unit.UnitTypes))
        {
            ValidationMessage =
                "The requested carried Unit is unavailable or ineligible for this System.";
            return false;
        }


        Vector2Int LocalOriginOffset =
            Carrier.Definition.OriginPoint - Carrier.Definition.MovementPivot;


        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                LocalOriginOffset,
                Carrier.Facing,
                out Vector2Int RotatedOriginOffset))
        {
            ValidationMessage = "The carrier has an invalid facing.";
            return false;
        }


        Origin = Pivot + RotatedOriginOffset;
        ValidationMessage = string.Empty;
        return true;
    }


    private bool TryGetMineSystem(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        out RuntimeUnit Unit,
        out RuntimeSystemSlotState Slot,
        out Vector2Int Origin,
        out string ValidationMessage
    )
    {
        Unit = null;
        Slot = null;
        Origin = default;


        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster) ||
            !Roster.TryGetRuntimeUnit(RuntimeUnitID, out Unit))
        {
            if (string.IsNullOrWhiteSpace(ValidationMessage))
                ValidationMessage = "The requested Unit does not belong to this participant.";

            return false;
        }


        if (!Unit.IsActive ||
            !_GameMap.TryGetUnitMovementPivotCoordinate(Unit, out Vector2Int Pivot))
        {
            ValidationMessage = "The requested Unit is not active on the Map.";
            return false;
        }


        if (SystemSlotIndex < 0 ||
            SystemSlotIndex >= Unit.SystemSlots.Length ||
            Unit.SystemSlots[SystemSlotIndex] == null ||
            Unit.SystemSlots[SystemSlotIndex].System == null)
        {
            ValidationMessage = "The requested System slot is empty or does not exist.";
            return false;
        }


        Slot = Unit.SystemSlots[SystemSlotIndex];
        Definition_System System = Slot.System;


        if (!System.Validate(out string SystemMessage))
        {
            ValidationMessage =
                $"System '{System.name}' is invalid: {SystemMessage}";
            return false;
        }


        if ((System.OperationalPhases & GameplayPhase.PreBattle) == 0 ||
            System.MineDeployment == null ||
            !System.MineDeployment.Enabled ||
            System.Pattern == null ||
            System.Pattern.Mode != PatternMode.OriginPoint)
        {
            ValidationMessage =
                $"System '{System.Name}' is not a Pre-Battle Mine Deployment System.";
            return false;
        }


        if (!Slot.HasCharges())
        {
            ValidationMessage =
                $"System '{System.Name}' has no charges remaining.";
            return false;
        }


        Vector2Int LocalOriginOffset =
            Unit.Definition.OriginPoint - Unit.Definition.MovementPivot;


        if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                LocalOriginOffset,
                Unit.Facing,
                out Vector2Int RotatedOriginOffset))
        {
            ValidationMessage = "The Unit has an invalid facing.";
            return false;
        }


        Origin = Pivot + RotatedOriginOffset;
        ValidationMessage = string.Empty;
        return true;
    }


    private static bool PatternContains(
        Definition_Pattern Pattern,
        UnitFacing Facing,
        Vector2Int Origin,
        Vector2Int Coordinate
    )
    {
        if (Pattern?.Layers == null)
            return false;


        for (int LayerIndex = 0;
            LayerIndex < Pattern.Layers.Length;
            LayerIndex++)
        {
            PatternLayer Layer = Pattern.Layers[LayerIndex];
            if (Layer?.Offsets == null)
                continue;


            for (int OffsetIndex = 0;
                OffsetIndex < Layer.Offsets.Length;
                OffsetIndex++)
            {
                if (UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                        Layer.Offsets[OffsetIndex],
                        Facing,
                        out Vector2Int RotatedOffset) &&
                    Origin + RotatedOffset == Coordinate)
                {
                    return true;
                }
            }
        }


        return false;
    }


    private UnitFacing GetUnitFacing(
        RuntimeParticipant Participant,
        string RuntimeUnitID
    )
    {
        return _GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster Roster) &&
            Roster.TryGetRuntimeUnit(RuntimeUnitID, out RuntimeUnit Unit)
                ? Unit.Facing
                : UnitFacing.East;
    }


    private bool TryGetState(
        RuntimeParticipant Participant,
        out ParticipantState State
    )
    {
        State = null;


        if (!IsInitialized || Participant == null)
            return false;


        if (ReferenceEquals(Participant, _ParticipantA.Participant))
            State = _ParticipantA;
        else if (ReferenceEquals(Participant, _ParticipantB.Participant))
            State = _ParticipantB;


        return State != null;
    }


    private static string AbilityKey(
        string RuntimeUnitID,
        int AbilitySlotIndex
    )
    {
        return $"{RuntimeUnitID}:{AbilitySlotIndex}";
    }


    private static void ResetState(ParticipantState State)
    {
        State.Ended = false;
        State.ActionsCommitted = 0;
        State.UsedAbilities.Clear();
    }
}
