using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative Movement-phase rules and state.
//
// Controllers submit only Runtime Unit IDs, board coordinates and facings.
// Route planning uses participant knowledge; resolution checks each step
// against GameMap truth so hidden blockers can interrupt without being leaked.
public sealed class GameMovement
{
    public enum CommitOutcome
    {
        Completed = 0,
        Interrupted = 1
    }


    public sealed class RouteStep
    {
        public Vector2Int MovementPivotCoordinate { get; }
        public UnitFacing Facing { get; }


        internal RouteStep(
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing
        )
        {
            this.MovementPivotCoordinate =
                MovementPivotCoordinate;

            this.Facing =
                Facing;
        }
    }


    public sealed class MovementPlan
    {
        public string RuntimeUnitID { get; }
        public Vector2Int StartingMovementPivotCoordinate { get; }
        public UnitFacing StartingFacing { get; }
        public Vector2Int DestinationMovementPivotCoordinate { get; }
        public UnitFacing DestinationFacing { get; }
        public bool WouldConsumeMove { get; }
        public RouteStep[] Route { get; }


        internal MovementPlan(
            string RuntimeUnitID,
            Vector2Int StartingMovementPivotCoordinate,
            UnitFacing StartingFacing,
            Vector2Int DestinationMovementPivotCoordinate,
            UnitFacing DestinationFacing,
            bool WouldConsumeMove,
            RouteStep[] Route
        )
        {
            this.RuntimeUnitID =
                RuntimeUnitID;

            this.StartingMovementPivotCoordinate =
                StartingMovementPivotCoordinate;

            this.StartingFacing =
                StartingFacing;

            this.DestinationMovementPivotCoordinate =
                DestinationMovementPivotCoordinate;

            this.DestinationFacing =
                DestinationFacing;

            this.WouldConsumeMove =
                WouldConsumeMove;

            this.Route =
                Route ??
                Array.Empty<RouteStep>();
        }
    }


    public sealed class CommitResult
    {
        public CommitOutcome Outcome { get; }
        public string RuntimeUnitID { get; }
        public Vector2Int FinalMovementPivotCoordinate { get; }
        public UnitFacing FinalFacing { get; }
        public bool MoveConsumed { get; }
        public RouteStep[] TraveledRoute { get; }

        internal string DefinitionID { get; }
        internal string InstanceName { get; }
        internal bool[] ParticipantAVisibility { get; }
        internal bool[] ParticipantBVisibility { get; }
        internal ParticipantUnitTagSnapshot[] Tags { get; }


        internal CommitResult(
            CommitOutcome Outcome,
            string RuntimeUnitID,
            Vector2Int FinalMovementPivotCoordinate,
            UnitFacing FinalFacing,
            bool MoveConsumed,
            RouteStep[] TraveledRoute,
            string DefinitionID,
            string InstanceName,
            bool[] ParticipantAVisibility,
            bool[] ParticipantBVisibility,
            ParticipantUnitTagSnapshot[] Tags
        )
        {
            this.Outcome =
                Outcome;

            this.RuntimeUnitID =
                RuntimeUnitID;

            this.FinalMovementPivotCoordinate =
                FinalMovementPivotCoordinate;

            this.FinalFacing =
                FinalFacing;

            this.MoveConsumed =
                MoveConsumed;

            this.TraveledRoute =
                TraveledRoute ??
                Array.Empty<RouteStep>();

            this.DefinitionID =
                DefinitionID ??
                string.Empty;

            this.InstanceName =
                InstanceName ??
                string.Empty;

            this.ParticipantAVisibility =
                ParticipantAVisibility ??
                Array.Empty<bool>();

            this.ParticipantBVisibility =
                ParticipantBVisibility ??
                Array.Empty<bool>();

            this.Tags =
                Tags ??
                Array.Empty<ParticipantUnitTagSnapshot>();
        }
    }


    public sealed class UnitView
    {
        public string RuntimeUnitID { get; }
        public bool HasMoved { get; }


        internal UnitView(
            string RuntimeUnitID,
            bool HasMoved
        )
        {
            this.RuntimeUnitID =
                RuntimeUnitID;

            this.HasMoved =
                HasMoved;
        }
    }


    public sealed class ParticipantView
    {
        public bool IsMovementPhaseActive { get; }
        public int MovementLimit { get; }
        public int MovesCommitted { get; }
        public bool MovementEnded { get; }
        public bool CanAct { get; }
        public float SecondsRemaining { get; }
        public UnitView[] Units { get; }


        internal ParticipantView(
            bool IsMovementPhaseActive,
            int MovementLimit,
            int MovesCommitted,
            bool MovementEnded,
            bool CanAct,
            float SecondsRemaining,
            UnitView[] Units
        )
        {
            this.IsMovementPhaseActive =
                IsMovementPhaseActive;

            this.MovementLimit =
                MovementLimit;

            this.MovesCommitted =
                MovesCommitted;

            this.MovementEnded =
                MovementEnded;

            this.CanAct =
                CanAct;

            this.SecondsRemaining =
                SecondsRemaining;

            this.Units =
                Units ??
                Array.Empty<UnitView>();
        }
    }


    private sealed class ParticipantState
    {
        public readonly RuntimeParticipant Participant;
        public readonly HashSet<RuntimeUnit> MovedUnits =
            new HashSet<RuntimeUnit>();

        public bool Ended;
        public int MovementLimit;


        public ParticipantState(
            RuntimeParticipant Participant
        )
        {
            this.Participant =
                Participant;
        }
    }


    private struct RouteState : IEquatable<RouteState>
    {
        public Vector2Int Pivot;
        public UnitFacing Facing;


        public RouteState(
            Vector2Int Pivot,
            UnitFacing Facing
        )
        {
            this.Pivot =
                Pivot;

            this.Facing =
                Facing;
        }


        public bool Equals(
            RouteState Other
        )
        {
            return
                Pivot == Other.Pivot &&
                Facing == Other.Facing;
        }


        public override bool Equals(
            object Object
        )
        {
            return
                Object is RouteState Other &&
                Equals(Other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (Pivot.GetHashCode() * 397) ^
                    (int)Facing;
            }
        }
    }


    private static readonly Vector2Int[] TranslationDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };


    private GameMap _GameMap;
    private GameUnits _GameUnits;
    private GameKnowledge _GameKnowledge;
    private GameUnitEffects _GameUnitEffects;
    private ParticipantState _ParticipantA;
    private ParticipantState _ParticipantB;
    private bool _PhaseActive;
    private RuntimeParticipant _ActiveParticipant;
    private float _SecondsPerParticipant;
    private float _SecondsRemaining;


    public bool IsInitialized { get; private set; }


    internal bool Initialize(
        GameMap GameMap,
        GameUnits GameUnits,
        GameKnowledge GameKnowledge,
        GameUnitEffects GameUnitEffects,
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB,
        out string ValidationMessage
    )
    {
        IsInitialized =
            false;


        if (GameMap == null ||
            !GameMap.Initialized ||
            GameUnits == null ||
            !GameUnits.Initialized ||
            GameKnowledge == null ||
            !GameKnowledge.IsInitialized ||
            GameUnitEffects == null ||
            !GameUnitEffects.Initialized ||
            ParticipantA == null ||
            ParticipantB == null ||
            ReferenceEquals(
                ParticipantA,
                ParticipantB))
        {
            ValidationMessage =
                "Movement requires initialized Map, Unit, Knowledge and participant state.";

            return false;
        }


        _GameMap =
            GameMap;

        _GameUnits =
            GameUnits;

        _GameKnowledge =
            GameKnowledge;

        _GameUnitEffects =
            GameUnitEffects;

        _ParticipantA =
            new ParticipantState(
                ParticipantA
            );

        _ParticipantB =
            new ParticipantState(
                ParticipantB
            );

        _PhaseActive =
            false;

        IsInitialized =
            true;

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool BeginPhase(
        RuntimeParticipant FirstParticipant,
        float SecondsPerParticipant,
        out string ValidationMessage
    )
    {
        if (!IsInitialized)
        {
            ValidationMessage =
                "Movement has not been initialized.";

            return false;
        }


        if (!ReferenceEquals(FirstParticipant, _ParticipantA.Participant) &&
            !ReferenceEquals(FirstParticipant, _ParticipantB.Participant))
        {
            ValidationMessage =
                "Movement first participant does not belong to this Match.";

            return false;
        }


        _ParticipantA.MovedUnits.Clear();
        _ParticipantB.MovedUnits.Clear();


        if (!_GameUnits.TryGetRoster(
                _ParticipantA.Participant,
                out ParticipantUnitRoster ParticipantARoster) ||
            !_GameUnits.TryGetRoster(
                _ParticipantB.Participant,
                out ParticipantUnitRoster ParticipantBRoster))
        {
            ValidationMessage =
                "Movement could not resolve both participant Unit rosters.";

            return false;
        }


        _ParticipantA.MovementLimit =
            CalculateMovementLimit(
                ParticipantARoster
            );

        _ParticipantB.MovementLimit =
            CalculateMovementLimit(
                ParticipantBRoster
            );

        _ParticipantA.Ended =
            false;

        _ParticipantB.Ended =
            false;

        _ActiveParticipant =
            FirstParticipant;

        _SecondsPerParticipant =
            Mathf.Max(1f, SecondsPerParticipant);

        _SecondsRemaining =
            _SecondsPerParticipant;

        _PhaseActive =
            true;

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryGetView(
        RuntimeParticipant Participant,
        out ParticipantView View
    )
    {
        View =
            null;


        if (!TryGetState(
                Participant,
                out ParticipantState State) ||
            !_GameUnits.TryGetRoster(
                Participant,
                out ParticipantUnitRoster Roster))
        {
            return false;
        }


        List<UnitView> Units =
            new List<UnitView>();


        for (int i = 0;
            i < Roster.RuntimeUnits.Count;
            i++)
        {
            RuntimeUnit Unit =
                Roster.RuntimeUnits[i];


            if (Unit == null)
            {
                continue;
            }


            Units.Add(
                new UnitView(
                    Unit.RuntimeID,
                    State.MovedUnits.Contains(
                        Unit)
                )
            );
        }


        View =
            new ParticipantView(
                _PhaseActive,
                State.MovementLimit,
                State.MovedUnits.Count,
                State.Ended,
                _PhaseActive &&
                    ReferenceEquals(
                        Participant,
                        _ActiveParticipant),
                _SecondsRemaining,
                Units.ToArray()
            );

        return true;
    }


    internal bool CanAcceptParticipantAction(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!_PhaseActive ||
            !TryGetState(
                Participant,
                out ParticipantState State))
        {
            ValidationMessage =
                "Movement is not active for this participant.";

            return false;
        }


        if (State.Ended)
        {
            ValidationMessage =
                "This participant has already ended Movement.";

            return false;
        }


        if (!ReferenceEquals(
                Participant,
                _ActiveParticipant))
        {
            ValidationMessage =
                "The other participant currently has Movement priority.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool ValidateMovement(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        Vector2Int DestinationMovementPivotCoordinate,
        UnitFacing DestinationFacing,
        out MovementPlan Plan,
        out string ValidationMessage
    )
    {
        Plan =
            null;


        if (!TryGetMovableUnit(
                Participant,
                RuntimeUnitID,
                out ParticipantState State,
                out _,
                out RuntimeUnit Unit,
                out ValidationMessage))
        {
            return false;
        }


        if (!_GameMap.TryGetUnitMovementPivotCoordinate(
                Unit,
                out Vector2Int StartingPivot))
        {
            ValidationMessage =
                "The requested Unit has no valid MovementPivot on the Map.";

            return false;
        }


        if (!UnitFootprintGeometry.IsValidFacing(
                DestinationFacing))
        {
            ValidationMessage =
                "The requested destination Facing is invalid.";

            return false;
        }


        if (StartingPivot ==
                DestinationMovementPivotCoordinate &&
            Unit.Facing ==
                DestinationFacing)
        {
            ValidationMessage =
                "The requested movement does not change position or Facing.";

            return false;
        }


        bool WouldConsumeMove =
            WouldConsumeMovement(
                Unit,
                StartingPivot,
                Unit.Facing,
                DestinationMovementPivotCoordinate,
                DestinationFacing
            );


        if (WouldConsumeMove &&
            State.MovedUnits.Count >=
                State.MovementLimit)
        {
            ValidationMessage =
                "This participant has reached the Movement limit for the phase.";

            return false;
        }


        if (!_GameKnowledge.Refresh(
                GameplayPhase.Movement,
                out ValidationMessage))
        {
            return false;
        }


        if (!TryBuildMovementArea(
                Unit,
                StartingPivot,
                Unit.Facing,
                out HashSet<Vector2Int> MovementArea,
                out ValidationMessage))
        {
            return false;
        }


        RouteState Start =
            new RouteState(
                StartingPivot,
                Unit.Facing
            );

        RouteState Destination =
            new RouteState(
                DestinationMovementPivotCoordinate,
                DestinationFacing
            );


        if (!TryFindKnownRoute(
                Participant,
                Unit,
                MovementArea,
                Start,
                Destination,
                out RouteStep[] Route))
        {
            ValidationMessage =
                "The requested destination is not reachable using currently known information.";

            return false;
        }


        Plan =
            new MovementPlan(
                Unit.RuntimeID,
                StartingPivot,
                Unit.Facing,
                DestinationMovementPivotCoordinate,
                DestinationFacing,
                WouldConsumeMove,
                Route
            );

        ValidationMessage =
            string.Empty;

        return true;
    }


    // Returns the authored maneuver area for one owned Unit. This contains no
    // hidden terrain or enemy information; final legality still comes only
    // from ValidateMovement/TryCommitMovement.
    internal bool TryGetMovementArea(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        out Vector2Int[] MovementArea,
        out string ValidationMessage
    )
    {
        MovementArea =
            Array.Empty<Vector2Int>();


        if (!TryGetMovableUnit(
                Participant,
                RuntimeUnitID,
                out _,
                out _,
                out RuntimeUnit Unit,
                out ValidationMessage) ||
            !_GameMap.TryGetUnitMovementPivotCoordinate(
                Unit,
                out Vector2Int StartingPivot) ||
            !TryBuildMovementArea(
                Unit,
                StartingPivot,
                Unit.Facing,
                out HashSet<Vector2Int> Area,
                out ValidationMessage))
        {
            if (string.IsNullOrWhiteSpace(
                    ValidationMessage))
            {
                ValidationMessage =
                    "The requested Unit has no valid Movement area.";
            }


            return false;
        }


        MovementArea =
            new Vector2Int[Area.Count];

        Area.CopyTo(
            MovementArea
        );

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryCommitMovement(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        Vector2Int DestinationMovementPivotCoordinate,
        UnitFacing DestinationFacing,
        out CommitResult Result,
        out string ValidationMessage
    )
    {
        Result =
            null;


        if (!ValidateMovement(
                Participant,
                RuntimeUnitID,
                DestinationMovementPivotCoordinate,
                DestinationFacing,
                out MovementPlan Plan,
                out ValidationMessage) ||
            !TryGetMovableUnit(
                Participant,
                RuntimeUnitID,
                out ParticipantState State,
                out _,
                out RuntimeUnit Unit,
                out ValidationMessage))
        {
            return false;
        }


        List<RouteStep> Traveled =
            new List<RouteStep>();

        List<bool> ParticipantAVisibility =
            new List<bool>();

        List<bool> ParticipantBVisibility =
            new List<bool>();

        Traveled.Add(
            Plan.Route[0]
        );

        ParticipantAVisibility.Add(
            _GameKnowledge.IsUnitVisibleToParticipant(
                _ParticipantA.Participant,
                Unit
            )
        );

        ParticipantBVisibility.Add(
            _GameKnowledge.IsUnitVisibleToParticipant(
                _ParticipantB.Participant,
                Unit
            )
        );


        CommitOutcome Outcome =
            CommitOutcome.Completed;


        for (int i = 1;
            i < Plan.Route.Length;
            i++)
        {
            RouteStep Step =
                Plan.Route[i];


            if (!UnitFootprintGeometry.TryBuildPlacement(
                    Unit.Definition,
                    Step.MovementPivotCoordinate,
                    Step.Facing,
                    out RuntimeUnitTilePlacement[] Placements,
                    out string GeometryMessage))
            {
                Debug.LogError(
                    $"Validated Movement route became geometrically invalid: {GeometryMessage}"
                );

                Outcome =
                    CommitOutcome.Interrupted;

                break;
            }


            if (!_GameMap.TryMoveUnit(
                    Unit,
                    Placements,
                    out RuntimeMineOccupancy[] TriggeredMines,
                    out _))
            {
                // Do not return MapMessage to the participant: it may identify
                // concealed terrain, a hidden enemy, or another secret object.
                Debug.Log(
                    $"Movement for '{Unit.InstanceName}' was interrupted by concealed or blocking Map state."
                );

                Outcome =
                    CommitOutcome.Interrupted;

                break;
            }


            Unit.SetFacing(
                Step.Facing
            );

            Traveled.Add(
                Step
            );


            if (TriggeredMines.Length > 0)
            {
                if (!_GameUnitEffects.ResolveTriggeredMines(
                        Unit,
                        TriggeredMines,
                        out bool UnitDestroyed,
                        out string MineResolutionMessage))
                {
                    Debug.LogError(
                        $"Mine resolution failed for '{Unit.InstanceName}': {MineResolutionMessage}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"Movement for '{Unit.InstanceName}' triggered Mines. {MineResolutionMessage}"
                    );
                }


                if (UnitDestroyed)
                {
                    Outcome = CommitOutcome.Interrupted;

                    if (!_GameKnowledge.Refresh(
                            GameplayPhase.Movement,
                            out string DestroyedKnowledgeMessage))
                    {
                        Debug.LogError(
                            $"Knowledge refresh failed after Mine destruction: {DestroyedKnowledgeMessage}"
                        );
                    }

                    break;
                }
            }


            if (!_GameKnowledge.Refresh(
                    GameplayPhase.Movement,
                    out string KnowledgeMessage))
            {
                Debug.LogError(
                    $"Knowledge refresh failed during Movement: {KnowledgeMessage}"
                );
            }


            ParticipantAVisibility.Add(
                _GameKnowledge.IsUnitVisibleToParticipant(
                    _ParticipantA.Participant,
                    Unit
                )
            );

            ParticipantBVisibility.Add(
                _GameKnowledge.IsUnitVisibleToParticipant(
                    _ParticipantB.Participant,
                    Unit
                )
            );
        }


        RouteStep FinalStep =
            Traveled[Traveled.Count - 1];

        bool MoveConsumed =
            WouldConsumeMovement(
                Unit,
                Plan.StartingMovementPivotCoordinate,
                Plan.StartingFacing,
                FinalStep.MovementPivotCoordinate,
                FinalStep.Facing
            );


        if (MoveConsumed)
        {
            State.MovedUnits.Add(
                Unit
            );
        }


        Result =
            new CommitResult(
                Outcome,
                Unit.RuntimeID,
                FinalStep.MovementPivotCoordinate,
                FinalStep.Facing,
                MoveConsumed,
                Traveled.ToArray(),
                Unit.Definition != null
                    ? Unit.Definition.ID
                    : string.Empty,
                Unit.InstanceName,
                ParticipantAVisibility.ToArray(),
                ParticipantBVisibility.ToArray(),
                BuildTagSnapshots(Unit)
            );

        ValidationMessage =
            string.Empty;

        return true;
    }


    private static ParticipantUnitTagSnapshot[] BuildTagSnapshots(
        RuntimeUnit Unit
    )
    {
        Definition_UnitTag[] Tags =
            Unit != null
                ? Unit.GetTags()
                : Array.Empty<Definition_UnitTag>();

        ParticipantUnitTagSnapshot[] Result =
            new ParticipantUnitTagSnapshot[Tags.Length];


        for (int i = 0; i < Tags.Length; i++)
        {
            Result[i] =
                new ParticipantUnitTagSnapshot(
                    Tags[i]
                );
        }


        return Result;
    }


    internal bool TryEndMovement(
        RuntimeParticipant Participant,
        out bool AllParticipantsEnded,
        out string ValidationMessage
    )
    {
        AllParticipantsEnded =
            false;


        if (!_PhaseActive ||
            !TryGetState(
                Participant,
                out ParticipantState State))
        {
            ValidationMessage =
                "Movement is not active for this participant.";

            return false;
        }


        if (State.Ended)
        {
            ValidationMessage =
                "This participant has already ended Movement.";

            return false;
        }


        if (!ReferenceEquals(
                Participant,
                _ActiveParticipant))
        {
            ValidationMessage =
                "The other participant currently has Movement priority.";

            return false;
        }


        State.Ended =
            true;

        AllParticipantsEnded =
            _ParticipantA.Ended &&
            _ParticipantB.Ended;


        if (AllParticipantsEnded)
        {
            _PhaseActive =
                false;

            _ActiveParticipant =
                null;
        }
        else
        {
            _ActiveParticipant =
                ReferenceEquals(
                    Participant,
                    _ParticipantA.Participant)
                    ? _ParticipantB.Participant
                    : _ParticipantA.Participant;

            _SecondsRemaining =
                _SecondsPerParticipant;
        }


        ValidationMessage =
            string.Empty;

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


        if (!_PhaseActive ||
            _ActiveParticipant == null)
        {
            return false;
        }


        _SecondsRemaining =
            Mathf.Max(
                0f,
                _SecondsRemaining -
                    Mathf.Max(0f, DeltaSeconds)
            );


        if (_SecondsRemaining > 0f)
        {
            return false;
        }


        TimedOutParticipant =
            _ActiveParticipant;

        return
            TryEndMovement(
                TimedOutParticipant,
                out AllParticipantsEnded,
                out _
            );
    }


    private bool TryGetMovableUnit(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        out ParticipantState State,
        out ParticipantUnitRoster Roster,
        out RuntimeUnit Unit,
        out string ValidationMessage
    )
    {
        State =
            null;

        Roster =
            null;

        Unit =
            null;


        if (!CanAcceptParticipantAction(
                Participant,
                out ValidationMessage) ||
            !TryGetState(
                Participant,
                out State) ||
            !_GameUnits.TryGetRoster(
                Participant,
                out Roster))
        {
            return false;
        }


        if (!Roster.TryGetRuntimeUnit(
                RuntimeUnitID,
                out Unit))
        {
            ValidationMessage =
                "The requested Unit does not belong to this participant.";

            return false;
        }


        if (!Unit.IsActive ||
            !_GameMap.IsUnitOnMap(
                Unit))
        {
            ValidationMessage =
                "The requested Unit is not active on the Map.";

            return false;
        }


        if (State.MovedUnits.Contains(
                Unit))
        {
            ValidationMessage =
                "The requested Unit has already moved during this phase.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool TryBuildMovementArea(
        RuntimeUnit Unit,
        Vector2Int StartingPivot,
        UnitFacing StartingFacing,
        out HashSet<Vector2Int> MovementArea,
        out string ValidationMessage
    )
    {
        MovementArea =
            new HashSet<Vector2Int>();

        ValidationMessage =
            string.Empty;


        Definition_Pattern Pattern =
            Unit.Definition != null
                ? Unit.Definition.MovementPattern
                : null;


        if (Pattern == null ||
            Pattern.Mode !=
                PatternMode.FootprintMovement ||
            !ReferenceEquals(
                Pattern.Unit,
                Unit.Definition) ||
            !Pattern.Validate(
                out ValidationMessage))
        {
            if (string.IsNullOrEmpty(
                    ValidationMessage))
            {
                ValidationMessage =
                    "The requested Unit has no valid Footprint Movement Pattern.";
            }

            return false;
        }


        if (!UnitFootprintGeometry.TryBuildPlacement(
                Unit.Definition,
                StartingPivot,
                StartingFacing,
                out RuntimeUnitTilePlacement[] StartingFootprint,
                out ValidationMessage))
        {
            return false;
        }


        for (int i = 0;
            i < StartingFootprint.Length;
            i++)
        {
            MovementArea.Add(
                StartingFootprint[i].MapCoordinate
            );
        }


        PatternLayer[] Layers =
            Pattern.Layers;


        if (Layers != null)
        {
            for (int LayerIndex = 0;
                LayerIndex < Layers.Length;
                LayerIndex++)
            {
                PatternLayer Layer =
                    Layers[LayerIndex];


                if (Layer == null ||
                    Layer.Offsets == null)
                {
                    continue;
                }


                for (int OffsetIndex = 0;
                    OffsetIndex < Layer.Offsets.Length;
                    OffsetIndex++)
                {
                    if (!UnitFootprintGeometry.TryRotateEastCanonicalOffset(
                            Layer.Offsets[OffsetIndex],
                            StartingFacing,
                            out Vector2Int RotatedOffset))
                    {
                        ValidationMessage =
                            "The Unit's starting Facing is invalid.";

                        return false;
                    }


                    MovementArea.Add(
                        StartingPivot +
                        RotatedOffset
                    );
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool TryFindKnownRoute(
        RuntimeParticipant Participant,
        RuntimeUnit Unit,
        HashSet<Vector2Int> MovementArea,
        RouteState Start,
        RouteState Destination,
        out RouteStep[] Route
    )
    {
        Route =
            Array.Empty<RouteStep>();


        if (!IsStateLegalFromKnownInformation(
                Participant,
                Unit,
                MovementArea,
                Destination))
        {
            return false;
        }


        Queue<RouteState> Frontier =
            new Queue<RouteState>();

        Dictionary<RouteState, RouteState> Previous =
            new Dictionary<RouteState, RouteState>();

        HashSet<RouteState> Visited =
            new HashSet<RouteState>();


        Frontier.Enqueue(
            Start
        );

        Visited.Add(
            Start
        );


        bool Found =
            false;


        while (Frontier.Count > 0)
        {
            RouteState Current =
                Frontier.Dequeue();


            if (Current.Equals(
                    Destination))
            {
                Found =
                    true;

                break;
            }


            for (int DirectionIndex = 0;
                DirectionIndex < TranslationDirections.Length;
                DirectionIndex++)
            {
                RouteState Neighbor =
                    new RouteState(
                        Current.Pivot +
                            TranslationDirections[DirectionIndex],
                        Current.Facing
                    );


                TryQueueKnownNeighbor(
                    Participant,
                    Unit,
                    MovementArea,
                    Current,
                    Neighbor,
                    Frontier,
                    Previous,
                    Visited
                );
            }


            TryQueueKnownNeighbor(
                Participant,
                Unit,
                MovementArea,
                Current,
                new RouteState(
                    Current.Pivot,
                    RotateClockwise(
                        Current.Facing)
                ),
                Frontier,
                Previous,
                Visited
            );

            TryQueueKnownNeighbor(
                Participant,
                Unit,
                MovementArea,
                Current,
                new RouteState(
                    Current.Pivot,
                    RotateCounterClockwise(
                        Current.Facing)
                ),
                Frontier,
                Previous,
                Visited
            );
        }


        if (!Found)
        {
            return false;
        }


        List<RouteState> ReverseRoute =
            new List<RouteState>();

        RouteState RouteCursor =
            Destination;


        ReverseRoute.Add(
            RouteCursor
        );


        while (!RouteCursor.Equals(
            Start))
        {
            RouteCursor =
                Previous[RouteCursor];

            ReverseRoute.Add(
                RouteCursor
            );
        }


        ReverseRoute.Reverse();

        Route =
            new RouteStep[ReverseRoute.Count];


        for (int i = 0;
            i < ReverseRoute.Count;
            i++)
        {
            Route[i] =
                new RouteStep(
                    ReverseRoute[i].Pivot,
                    ReverseRoute[i].Facing
                );
        }


        return true;
    }


    private void TryQueueKnownNeighbor(
        RuntimeParticipant Participant,
        RuntimeUnit Unit,
        HashSet<Vector2Int> MovementArea,
        RouteState Current,
        RouteState Neighbor,
        Queue<RouteState> Frontier,
        Dictionary<RouteState, RouteState> Previous,
        HashSet<RouteState> Visited
    )
    {
        if (Visited.Contains(
                Neighbor) ||
            !IsStateLegalFromKnownInformation(
                Participant,
                Unit,
                MovementArea,
                Neighbor))
        {
            return;
        }


        Visited.Add(
            Neighbor
        );

        Previous.Add(
            Neighbor,
            Current
        );

        Frontier.Enqueue(
            Neighbor
        );
    }


    private bool IsStateLegalFromKnownInformation(
        RuntimeParticipant Participant,
        RuntimeUnit Unit,
        HashSet<Vector2Int> MovementArea,
        RouteState State
    )
    {
        if (!UnitFootprintGeometry.TryBuildPlacement(
                Unit.Definition,
                State.Pivot,
                State.Facing,
                out RuntimeUnitTilePlacement[] Placements,
                out _))
        {
            return false;
        }


        for (int i = 0;
            i < Placements.Length;
            i++)
        {
            Vector2Int Coordinate =
                Placements[i].MapCoordinate;


            if (!MovementArea.Contains(
                    Coordinate) ||
                !_GameMap.TryGetTile(
                    Coordinate,
                    out RuntimeMapTile Tile))
            {
                return false;
            }


            if (_GameKnowledge.TryGetTileKnowledge(
                    Participant,
                    Coordinate,
                    out ParticipantTileVisibility Visibility,
                    out MapTerrain KnownTerrain) &&
                !_GameMap.TerrainAllowsDomain(
                    KnownTerrain,
                    Unit.CurrentDomain))
            {
                return false;
            }


            if (Tile.UnitOccupancy != null &&
                !ReferenceEquals(
                    Tile.UnitOccupancy.Unit,
                    Unit))
            {
                bool OwnUnit =
                    ReferenceEquals(
                        Tile.UnitOccupancy.Owner,
                        Participant
                    );

                bool VisibleEnemy =
                    Visibility ==
                        ParticipantTileVisibility.Absolute;


                if (OwnUnit ||
                    VisibleEnemy)
                {
                    return false;
                }
            }


            if (Tile.Mine != null &&
                ReferenceEquals(
                    Tile.Mine.Owner,
                    Participant))
            {
                return false;
            }
        }


        return true;
    }


    private int CalculateMovementLimit(
        ParticipantUnitRoster Roster
    )
    {
        int ActiveUnitCount =
            0;


        for (int i = 0;
            i < Roster.RuntimeUnits.Count;
            i++)
        {
            RuntimeUnit Unit =
                Roster.RuntimeUnits[i];


            if (Unit != null &&
                Unit.IsActive &&
                _GameMap.IsUnitOnMap(
                    Unit))
            {
                ActiveUnitCount++;
            }
        }


        return
            (ActiveUnitCount + 1) /
            2;
    }


    private bool TryGetState(
        RuntimeParticipant Participant,
        out ParticipantState State
    )
    {
        State =
            null;


        if (!IsInitialized ||
            Participant == null)
        {
            return false;
        }


        if (ReferenceEquals(
                Participant,
                _ParticipantA.Participant))
        {
            State =
                _ParticipantA;
        }
        else if (ReferenceEquals(
            Participant,
            _ParticipantB.Participant))
        {
            State =
                _ParticipantB;
        }


        return
            State != null;
    }


    private static bool WouldConsumeMovement(
        RuntimeUnit Unit,
        Vector2Int StartingPivot,
        UnitFacing StartingFacing,
        Vector2Int FinalPivot,
        UnitFacing FinalFacing
    )
    {
        if (StartingPivot !=
            FinalPivot)
        {
            return true;
        }


        if (StartingFacing ==
            FinalFacing)
        {
            return false;
        }


        Vector2Int FootprintSize =
            Unit.Definition != null
                ? Unit.Definition.FootprintSize
                : Vector2Int.one;


        return
            FootprintSize.x *
                FootprintSize.y >
            1;
    }


    private static UnitFacing RotateClockwise(
        UnitFacing Facing
    )
    {
        return
            (UnitFacing)(((int)Facing + 1) % 4);
    }


    private static UnitFacing RotateCounterClockwise(
        UnitFacing Facing
    )
    {
        return
            (UnitFacing)(((int)Facing + 3) % 4);
    }
}
