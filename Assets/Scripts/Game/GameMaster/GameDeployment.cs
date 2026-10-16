using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative tentative state and rules for initial Match Deployment.
//
// GameDeployment owns:
// - participant tentative plans;
// - authored Deployment-zone resolution;
// - tentative placement validation;
// - tentative footprint conflicts;
// - selection;
// - movement through resubmission;
// - rotation;
// - reset.
//
// It does not create RuntimeUnits or commit GameMap occupancy.
// GameMaster remains responsible for phase orchestration and final commit.
public sealed class GameDeployment
{
    internal sealed class Placement
    {
        private Vector2Int _MovementPivotCoordinate;

        private UnitFacing _Facing;


        public RuntimeFleetEntryState FleetEntry
        {
            get;
        }


        public string ID =>
            FleetEntry.ID;


        public Definition_Unit Unit =>
            FleetEntry.Unit;


        public bool IsPlaced
        {
            get;
            private set;
        }


        public bool HasFacing
        {
            get;
            private set;
        }


        public Vector2Int MovementPivotCoordinate =>
            _MovementPivotCoordinate;


        public UnitFacing Facing =>
            _Facing;


        internal Placement(
            RuntimeFleetEntryState FleetEntry
        )
        {
            this.FleetEntry =
                FleetEntry;

            Reset();
        }


        internal void SetPlacement(
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing
        )
        {
            _MovementPivotCoordinate =
                MovementPivotCoordinate;

            _Facing =
                Facing;

            IsPlaced =
                true;

            HasFacing =
                true;
        }


        internal void ClearPlacement()
        {
            IsPlaced =
                false;

            _MovementPivotCoordinate =
                default;
        }


        internal void Reset()
        {
            IsPlaced =
                false;

            HasFacing =
                false;

            _MovementPivotCoordinate =
                default;

            _Facing =
                default;
        }
    }


    internal sealed class ParticipantPlan
    {
        private readonly List<Placement> _Placements =
            new List<Placement>();

        private readonly Dictionary<string, Placement> _PlacementsByID =
            new Dictionary<string, Placement>();


        public ParticipantUnitRoster UnitRoster
        {
            get;
        }


        public RuntimeParticipant Participant =>
            UnitRoster != null
                ? UnitRoster.Participant
                : null;


        public IReadOnlyList<Placement> Placements =>
            _Placements;


        public Placement SelectedPlacement
        {
            get;
            private set;
        }


        public int TotalCount =>
            _Placements.Count;


        public int PlacedCount
        {
            get
            {
                int Count =
                    0;


                for (int Index = 0;
                    Index < _Placements.Count;
                    Index++)
                {
                    if (_Placements[Index].IsPlaced)
                    {
                        Count++;
                    }
                }


                return Count;
            }
        }


        public bool AllPlaced
        {
            get
            {
                if (_Placements.Count == 0)
                {
                    return false;
                }


                for (int Index = 0;
                    Index < _Placements.Count;
                    Index++)
                {
                    Placement Current =
                        _Placements[Index];


                    if (!Current.IsPlaced ||
                        !Current.HasFacing)
                    {
                        return false;
                    }
                }


                return true;
            }
        }


        internal ParticipantPlan(
            ParticipantUnitRoster UnitRoster
        )
        {
            this.UnitRoster =
                UnitRoster;
        }


        internal bool Initialize(
            out string ValidationMessage
        )
        {
            _Placements.Clear();
            _PlacementsByID.Clear();

            SelectedPlacement =
                null;


            if (UnitRoster == null)
            {
                ValidationMessage =
                    "Participant Unit Roster is missing.";

                return false;
            }


            if (UnitRoster.Participant == null)
            {
                ValidationMessage =
                    "Participant Unit Roster has no Participant.";

                return false;
            }


            if (UnitRoster.RuntimeUnits.Count != 0)
            {
                ValidationMessage =
                    "Participant already contains RuntimeUnits before Deployment.";

                return false;
            }


            if (UnitRoster.FleetEntries.Count == 0)
            {
                ValidationMessage =
                    "Participant has no deployable Fleet Entries.";

                return false;
            }


            for (int Index = 0;
                Index < UnitRoster.FleetEntries.Count;
                Index++)
            {
                RuntimeFleetEntryState FleetEntry =
                    UnitRoster.FleetEntries[Index];


                if (FleetEntry == null)
                {
                    ValidationMessage =
                        $"Fleet Entry state {Index} is missing.";

                    return false;
                }


                if (string.IsNullOrWhiteSpace(
                        FleetEntry.ID))
                {
                    ValidationMessage =
                        $"Fleet Entry state {Index} has no ID.";

                    return false;
                }


                if (FleetEntry.Unit == null)
                {
                    ValidationMessage =
                        $"Fleet Entry '{FleetEntry.ID}' has no Unit Definition.";

                    return false;
                }


                if (FleetEntry.IsCommitted)
                {
                    ValidationMessage =
                        $"Fleet Entry '{FleetEntry.ID}' is already committed before Deployment.";

                    return false;
                }


                if (_PlacementsByID.ContainsKey(
                        FleetEntry.ID))
                {
                    ValidationMessage =
                        $"Duplicate Fleet Entry ID '{FleetEntry.ID}' exists in Deployment.";

                    return false;
                }


                Placement NewPlacement =
                    new Placement(
                        FleetEntry
                    );


                _Placements.Add(
                    NewPlacement
                );

                _PlacementsByID.Add(
                    FleetEntry.ID,
                    NewPlacement
                );
            }


            ValidationMessage =
                string.Empty;

            return true;
        }


        public bool TryGetPlacement(
            string FleetEntryID,
            out Placement FoundPlacement
        )
        {
            FoundPlacement =
                null;


            if (string.IsNullOrWhiteSpace(
                    FleetEntryID))
            {
                return false;
            }


            return
                _PlacementsByID.TryGetValue(
                    FleetEntryID,
                    out FoundPlacement
                );
        }


        internal bool TrySelect(
            string FleetEntryID
        )
        {
            if (!TryGetPlacement(
                    FleetEntryID,
                    out Placement FoundPlacement))
            {
                return false;
            }


            SelectedPlacement =
                FoundPlacement;

            return true;
        }


        internal void ClearSelection()
        {
            SelectedPlacement =
                null;
        }


        internal bool TrySetPlacement(
            string FleetEntryID,
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing
        )
        {
            if (!TryGetPlacement(
                    FleetEntryID,
                    out Placement FoundPlacement))
            {
                return false;
            }


            FoundPlacement.SetPlacement(
                MovementPivotCoordinate,
                Facing
            );

            SelectedPlacement =
                FoundPlacement;

            return true;
        }


        internal bool TryClearPlacement(
            string FleetEntryID
        )
        {
            if (!TryGetPlacement(
                    FleetEntryID,
                    out Placement FoundPlacement))
            {
                return false;
            }


            FoundPlacement.ClearPlacement();


            if (ReferenceEquals(
                    SelectedPlacement,
                    FoundPlacement))
            {
                SelectedPlacement =
                    null;
            }


            return true;
        }


        internal void Reset()
        {
            for (int Index = 0;
                Index < _Placements.Count;
                Index++)
            {
                _Placements[Index].Reset();
            }


            SelectedPlacement =
                null;
        }
    }


    // Participant-filtered, read-only Deployment output.
    //
    // Human and AI controllers may observe this view, but cannot receive or
    // mutate the authoritative ParticipantPlan that backs it.
    public sealed class PlacementView
    {
        private readonly Placement _Placement;


        public string ID =>
            _Placement.ID;


        public Definition_Unit Unit =>
            _Placement.Unit;


        public bool IsPlaced =>
            _Placement.IsPlaced;


        public bool HasFacing =>
            _Placement.HasFacing;


        public Vector2Int MovementPivotCoordinate =>
            _Placement.MovementPivotCoordinate;


        public UnitFacing Facing =>
            _Placement.Facing;


        internal PlacementView(
            Placement Placement
        )
        {
            _Placement =
                Placement;
        }
    }


    public sealed class ParticipantView
    {
        private readonly ParticipantPlan _Plan;

        private readonly List<PlacementView> _Placements =
            new List<PlacementView>();

        private readonly Dictionary<string, PlacementView> _PlacementsByID =
            new Dictionary<string, PlacementView>();


        public string DisplayName
        {
            get
            {
                if (_Plan.Participant == null ||
                    _Plan.Participant.Participant == null)
                {
                    return string.Empty;
                }


                return
                    _Plan.Participant.Participant.DisplayName;
            }
        }


        public bool IsLocked =>
            _Plan.Participant != null &&
            _Plan.Participant.DeploymentLocked;


        public IReadOnlyList<PlacementView> Placements =>
            _Placements;


        public PlacementView SelectedPlacement
        {
            get
            {
                Placement Selected =
                    _Plan.SelectedPlacement;


                if (Selected == null)
                {
                    return null;
                }


                _PlacementsByID.TryGetValue(
                    Selected.ID,
                    out PlacementView SelectedView
                );


                return SelectedView;
            }
        }


        public int TotalCount =>
            _Plan.TotalCount;


        public int PlacedCount =>
            _Plan.PlacedCount;


        public bool AllPlaced =>
            _Plan.AllPlaced;


        internal ParticipantView(
            ParticipantPlan Plan
        )
        {
            _Plan =
                Plan;


            for (int Index = 0;
                Index < _Plan.Placements.Count;
                Index++)
            {
                PlacementView View =
                    new PlacementView(
                        _Plan.Placements[Index]
                    );


                _Placements.Add(
                    View
                );

                _PlacementsByID.Add(
                    View.ID,
                    View
                );
            }
        }


        public bool TryGetPlacement(
            string FleetEntryID,
            out PlacementView FoundPlacement
        )
        {
            FoundPlacement =
                null;


            if (string.IsNullOrWhiteSpace(
                    FleetEntryID))
            {
                return false;
            }


            return
                _PlacementsByID.TryGetValue(
                    FleetEntryID,
                    out FoundPlacement
                );
        }
    }


    // Fully revalidated, immutable input for the confirmation transaction.
    internal sealed class ConfirmedPlacement
    {
        public string FleetEntryID
        {
            get;
        }


        public Definition_Unit Unit
        {
            get;
        }


        public Vector2Int MovementPivotCoordinate
        {
            get;
        }


        public UnitFacing Facing
        {
            get;
        }


        public RuntimeUnitTilePlacement[] Tiles
        {
            get;
        }


        public MapTileCondition DeploymentCondition
        {
            get;
        }


        internal ConfirmedPlacement(
            string FleetEntryID,
            Definition_Unit Unit,
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing,
            RuntimeUnitTilePlacement[] Tiles,
            MapTileCondition DeploymentCondition
        )
        {
            this.FleetEntryID =
                FleetEntryID;

            this.Unit =
                Unit;

            this.MovementPivotCoordinate =
                MovementPivotCoordinate;

            this.Facing =
                Facing;

            this.Tiles =
                Tiles;

            this.DeploymentCondition =
                DeploymentCondition;
        }
    }


    private GameMap _GameMap;

    private ParticipantView _ParticipantAView;

    private ParticipantView _ParticipantBView;


    internal ParticipantPlan ParticipantA
    {
        get;
        private set;
    }


    internal ParticipantPlan ParticipantB
    {
        get;
        private set;
    }


    public bool IsInitialized
    {
        get;
        private set;
    }


    public bool Initialize(
        GameMap GameMap,
        ParticipantUnitRoster ParticipantAUnitRoster,
        ParticipantUnitRoster ParticipantBUnitRoster,
        out string ValidationMessage
    )
    {
        IsInitialized =
            false;

        _GameMap =
            null;

        ParticipantA =
            null;

        ParticipantB =
            null;

        _ParticipantAView =
            null;

        _ParticipantBView =
            null;


        if (GameMap == null ||
            !GameMap.Initialized)
        {
            ValidationMessage =
                "GameMap is missing or has not been initialized.";

            return false;
        }


        if (ParticipantAUnitRoster == null ||
            ParticipantBUnitRoster == null)
        {
            ValidationMessage =
                "Both participant Unit Rosters are required.";

            return false;
        }


        if (ReferenceEquals(
                ParticipantAUnitRoster.Participant,
                ParticipantBUnitRoster.Participant))
        {
            ValidationMessage =
                "Deployment participants must be different.";

            return false;
        }


        ParticipantPlan NewParticipantA =
            new ParticipantPlan(
                ParticipantAUnitRoster
            );


        if (!NewParticipantA.Initialize(
                out ValidationMessage))
        {
            return false;
        }


        ParticipantPlan NewParticipantB =
            new ParticipantPlan(
                ParticipantBUnitRoster
            );


        if (!NewParticipantB.Initialize(
                out ValidationMessage))
        {
            return false;
        }


        _GameMap =
            GameMap;

        ParticipantA =
            NewParticipantA;

        ParticipantB =
            NewParticipantB;

        _ParticipantAView =
            new ParticipantView(
                ParticipantA
            );

        _ParticipantBView =
            new ParticipantView(
                ParticipantB
            );

        IsInitialized =
            true;

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryGetPlan(
        RuntimeParticipant Participant,
        out ParticipantPlan Plan
    )
    {
        Plan =
            null;


        if (!IsInitialized ||
            Participant == null)
        {
            return false;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantA.Participant))
        {
            Plan =
                ParticipantA;

            return true;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantB.Participant))
        {
            Plan =
                ParticipantB;

            return true;
        }


        return false;
    }


    internal bool TryGetView(
        RuntimeParticipant Participant,
        out ParticipantView View
    )
    {
        View =
            null;


        if (!IsInitialized ||
            Participant == null)
        {
            return false;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantA.Participant))
        {
            View =
                _ParticipantAView;

            return View != null;
        }


        if (ReferenceEquals(
                Participant,
                ParticipantB.Participant))
        {
            View =
                _ParticipantBView;

            return View != null;
        }


        return false;
    }


    internal bool TryPrepareConfirmation(
        RuntimeParticipant Participant,
        out ConfirmedPlacement[] ConfirmedPlacements,
        out string ValidationMessage
    )
    {
        ConfirmedPlacements =
            Array.Empty<ConfirmedPlacement>();


        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (Participant.DeploymentLocked)
        {
            ValidationMessage =
                "Participant Deployment is already confirmed.";

            return false;
        }


        if (!Plan.AllPlaced)
        {
            ValidationMessage =
                "Every deployable Fleet Entry must be placed before confirmation.";

            return false;
        }


        if (!TryGetDeploymentCondition(
                Participant,
                out MapTileCondition DeploymentCondition))
        {
            ValidationMessage =
                "Participant has no authored Deployment-zone mapping.";

            return false;
        }


        ConfirmedPlacement[] Prepared =
            new ConfirmedPlacement[Plan.Placements.Count];


        for (int Index = 0;
            Index < Plan.Placements.Count;
            Index++)
        {
            Placement Placement =
                Plan.Placements[Index];


            if (!ValidatePlacement(
                    Participant,
                    Placement.ID,
                    Placement.MovementPivotCoordinate,
                    Placement.Facing,
                    out ValidationMessage) ||
                !UnitFootprintGeometry.TryBuildPlacement(
                    Placement.Unit,
                    Placement.MovementPivotCoordinate,
                    Placement.Facing,
                    out RuntimeUnitTilePlacement[] Tiles,
                    out ValidationMessage))
            {
                ConfirmedPlacements =
                    Array.Empty<ConfirmedPlacement>();

                return false;
            }


            Prepared[Index] =
                new ConfirmedPlacement(
                    Placement.ID,
                    Placement.Unit,
                    Placement.MovementPivotCoordinate,
                    Placement.Facing,
                    Tiles,
                    DeploymentCondition
                );
        }


        ConfirmedPlacements =
            Prepared;

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryGetInitialFacing(
        RuntimeParticipant Participant,
        out UnitFacing Facing
    )
    {
        Facing =
            default;


        if (!TryGetDeploymentCondition(
                Participant,
                out MapTileCondition OwnCondition) ||
            !TryGetOpponentDeploymentCondition(
                Participant,
                out MapTileCondition OpponentCondition) ||
            !TryCalculateDeploymentZoneCenter(
                OwnCondition,
                out Vector2 OwnCenter) ||
            !TryCalculateDeploymentZoneCenter(
                OpponentCondition,
                out Vector2 OpponentCenter))
        {
            return false;
        }


        Vector2 Direction =
            OpponentCenter -
            OwnCenter;


        if (Direction.sqrMagnitude <=
            0f)
        {
            return false;
        }


        if (Mathf.Abs(
                Direction.x) >=
            Mathf.Abs(
                Direction.y))
        {
            Facing =
                Direction.x >= 0f
                    ? UnitFacing.East
                    : UnitFacing.West;
        }
        else
        {
            Facing =
                Direction.y >= 0f
                    ? UnitFacing.North
                    : UnitFacing.South;
        }


        return true;
    }


    internal bool TryGetDeploymentZoneCoordinates(
        RuntimeParticipant Participant,
        out Vector2Int[] Coordinates
    )
    {
        Coordinates =
            Array.Empty<Vector2Int>();


        if (!TryGetDeploymentCondition(
                Participant,
                out MapTileCondition Condition))
        {
            return false;
        }


        List<Vector2Int> FoundCoordinates =
            new List<Vector2Int>();


        for (int Y = 0;
            Y < _GameMap.MapSize.y;
            Y++)
        {
            for (int X = 0;
                X < _GameMap.MapSize.x;
                X++)
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!_GameMap.TryGetTile(
                        Coordinate,
                        out RuntimeMapTile Tile) ||
                    !Tile.HasAuthoredCondition(
                        Condition))
                {
                    continue;
                }


                FoundCoordinates.Add(
                    Coordinate
                );
            }
        }


        if (FoundCoordinates.Count == 0)
        {
            return false;
        }


        Coordinates =
            FoundCoordinates.ToArray();

        return true;
    }


    internal bool ValidatePlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        if (!IsInitialized)
        {
            ValidationMessage =
                "Deployment has not been initialized.";

            return false;
        }


        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (Participant.DeploymentLocked)
        {
            ValidationMessage =
                "Participant Deployment is already confirmed.";

            return false;
        }


        if (!Plan.TryGetPlacement(
                FleetEntryID,
                out Placement CandidatePlacement))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' is not part of this Participant's Deployment.";

            return false;
        }


        if (CandidatePlacement.FleetEntry.IsCommitted)
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' is already committed.";

            return false;
        }


        if (!UnitFootprintGeometry.TryBuildPlacement(
                CandidatePlacement.Unit,
                MovementPivotCoordinate,
                Facing,
                out RuntimeUnitTilePlacement[] CandidateTiles,
                out ValidationMessage))
        {
            return false;
        }


        if (!TryGetDeploymentCondition(
                Participant,
                out MapTileCondition DeploymentCondition))
        {
            ValidationMessage =
                "Participant has no authored Deployment-zone mapping.";

            return false;
        }


        if (!_GameMap.ValidateUnitPlacement(
                CandidatePlacement.Unit,
                Participant,
                CandidateTiles,
                DeploymentCondition,
                out _,
                out ValidationMessage))
        {
            return false;
        }


        if (!ValidateTentativePlacementConflicts(
                Plan,
                CandidatePlacement,
                CandidateTiles,
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TrySubmitPlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        if (!ValidatePlacement(
                Participant,
                FleetEntryID,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage))
        {
            return false;
        }


        TryGetPlan(
            Participant,
            out ParticipantPlan Plan
        );


        if (!Plan.TrySetPlacement(
                FleetEntryID,
                MovementPivotCoordinate,
                Facing))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' could not be added to the tentative Deployment plan.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TrySelectPlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (Participant.DeploymentLocked)
        {
            ValidationMessage =
                "Participant Deployment is already confirmed.";

            return false;
        }


        if (!Plan.TryGetPlacement(
                FleetEntryID,
                out Placement Placement))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' is not part of this Participant's Deployment.";

            return false;
        }


        if (!Placement.IsPlaced)
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' has not been placed.";

            return false;
        }


        Plan.TrySelect(
            FleetEntryID
        );

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryClearSelection(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        Plan.ClearSelection();

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryRotatePlacementClockwise(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        return
            TryRotatePlacement(
                Participant,
                FleetEntryID,
                true,
                out ValidationMessage
            );
    }


    internal bool TryRotatePlacementCounterClockwise(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        return
            TryRotatePlacement(
                Participant,
                FleetEntryID,
                false,
                out ValidationMessage
            );
    }


    internal bool TryResetPlan(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (Participant.DeploymentLocked)
        {
            ValidationMessage =
                "Participant Deployment is already confirmed.";

            return false;
        }


        Plan.Reset();

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal void ResetAll()
    {
        if (!IsInitialized)
        {
            return;
        }


        ParticipantA.Reset();
        ParticipantB.Reset();
    }


    internal bool TryGetPlacementTiles(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out RuntimeUnitTilePlacement[] Placements,
        out string ValidationMessage
    )
    {
        Placements =
            Array.Empty<RuntimeUnitTilePlacement>();


        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (!Plan.TryGetPlacement(
                FleetEntryID,
                out Placement Placement))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' is not part of this Participant's Deployment.";

            return false;
        }


        if (!Placement.IsPlaced ||
            !Placement.HasFacing)
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' has no complete tentative placement.";

            return false;
        }


        return
            UnitFootprintGeometry.TryBuildPlacement(
                Placement.Unit,
                Placement.MovementPivotCoordinate,
                Placement.Facing,
                out Placements,
                out ValidationMessage
            );
    }


    private bool TryRotatePlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        bool Clockwise,
        out string ValidationMessage
    )
    {
        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            ValidationMessage =
                "Participant has no Deployment plan in this Match.";

            return false;
        }


        if (!Plan.TryGetPlacement(
                FleetEntryID,
                out Placement Placement))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' is not part of this Participant's Deployment.";

            return false;
        }


        if (!Placement.IsPlaced ||
            !Placement.HasFacing)
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' must be placed before it can rotate.";

            return false;
        }


        if (!TryGetRotatedFacing(
                Placement.Facing,
                Clockwise,
                out UnitFacing RotatedFacing))
        {
            ValidationMessage =
                $"Fleet Entry '{FleetEntryID}' has an invalid Facing.";

            return false;
        }


        return
            TrySubmitPlacement(
                Participant,
                FleetEntryID,
                Placement.MovementPivotCoordinate,
                RotatedFacing,
                out ValidationMessage
            );
    }


    private bool ValidateTentativePlacementConflicts(
        ParticipantPlan Plan,
        Placement CandidatePlacement,
        IReadOnlyList<RuntimeUnitTilePlacement> CandidateTiles,
        out string ValidationMessage
    )
    {
        HashSet<Vector2Int> CandidateCoordinates =
            new HashSet<Vector2Int>();


        for (int Index = 0;
            Index < CandidateTiles.Count;
            Index++)
        {
            CandidateCoordinates.Add(
                CandidateTiles[Index].MapCoordinate
            );
        }


        for (int Index = 0;
            Index < Plan.Placements.Count;
            Index++)
        {
            Placement ExistingPlacement =
                Plan.Placements[Index];


            if (ExistingPlacement == null ||
                ReferenceEquals(
                    ExistingPlacement,
                    CandidatePlacement) ||
                !ExistingPlacement.IsPlaced ||
                !ExistingPlacement.HasFacing)
            {
                continue;
            }


            if (!UnitFootprintGeometry.TryBuildPlacement(
                    ExistingPlacement.Unit,
                    ExistingPlacement.MovementPivotCoordinate,
                    ExistingPlacement.Facing,
                    out RuntimeUnitTilePlacement[] ExistingTiles,
                    out string ExistingValidationMessage))
            {
                ValidationMessage =
                    $"Existing tentative Fleet Entry '{ExistingPlacement.ID}' is invalid: " +
                    ExistingValidationMessage;

                return false;
            }


            for (int TileIndex = 0;
                TileIndex < ExistingTiles.Length;
                TileIndex++)
            {
                if (!CandidateCoordinates.Contains(
                        ExistingTiles[TileIndex].MapCoordinate))
                {
                    continue;
                }


                ValidationMessage =
                    $"Fleet Entry '{CandidatePlacement.ID}' overlaps tentative Fleet Entry " +
                    $"'{ExistingPlacement.ID}' at {ExistingTiles[TileIndex].MapCoordinate}.";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool TryGetDeploymentCondition(
        RuntimeParticipant Participant,
        out MapTileCondition Condition
    )
    {
        Condition =
            MapTileCondition.None;


        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            return false;
        }


        if (ReferenceEquals(
                Plan,
                ParticipantA))
        {
            Condition =
                MapTileCondition.Deployment_Player_A;

            return true;
        }


        if (ReferenceEquals(
                Plan,
                ParticipantB))
        {
            Condition =
                MapTileCondition.Deployment_Player_B;

            return true;
        }


        return false;
    }


    private bool TryGetOpponentDeploymentCondition(
        RuntimeParticipant Participant,
        out MapTileCondition Condition
    )
    {
        Condition =
            MapTileCondition.None;


        if (!TryGetPlan(
                Participant,
                out ParticipantPlan Plan))
        {
            return false;
        }


        if (ReferenceEquals(
                Plan,
                ParticipantA))
        {
            Condition =
                MapTileCondition.Deployment_Player_B;

            return true;
        }


        if (ReferenceEquals(
                Plan,
                ParticipantB))
        {
            Condition =
                MapTileCondition.Deployment_Player_A;

            return true;
        }


        return false;
    }


    private bool TryCalculateDeploymentZoneCenter(
        MapTileCondition Condition,
        out Vector2 Center
    )
    {
        Center =
            Vector2.zero;


        if (!IsInitialized ||
            _GameMap == null ||
            !_GameMap.Initialized ||
            Condition == MapTileCondition.None)
        {
            return false;
        }


        Vector2 CoordinateTotal =
            Vector2.zero;

        int CoordinateCount =
            0;


        for (int Y = 0;
            Y < _GameMap.MapSize.y;
            Y++)
        {
            for (int X = 0;
                X < _GameMap.MapSize.x;
                X++)
            {
                if (!_GameMap.TryGetTile(
                        new Vector2Int(
                            X,
                            Y
                        ),
                        out RuntimeMapTile Tile) ||
                    !Tile.HasAuthoredCondition(
                        Condition))
                {
                    continue;
                }


                CoordinateTotal +=
                    new Vector2(
                        X,
                        Y
                    );

                CoordinateCount++;
            }
        }


        if (CoordinateCount == 0)
        {
            return false;
        }


        Center =
            CoordinateTotal /
            CoordinateCount;

        return true;
    }


    private static bool TryGetRotatedFacing(
        UnitFacing Facing,
        bool Clockwise,
        out UnitFacing RotatedFacing
    )
    {
        if (!UnitFootprintGeometry.IsValidFacing(
                Facing))
        {
            RotatedFacing =
                default;

            return false;
        }


        if (Clockwise)
        {
            switch (Facing)
            {
                case UnitFacing.North:
                    RotatedFacing = UnitFacing.East;
                    return true;

                case UnitFacing.East:
                    RotatedFacing = UnitFacing.South;
                    return true;

                case UnitFacing.South:
                    RotatedFacing = UnitFacing.West;
                    return true;

                case UnitFacing.West:
                    RotatedFacing = UnitFacing.North;
                    return true;
            }
        }
        else
        {
            switch (Facing)
            {
                case UnitFacing.North:
                    RotatedFacing = UnitFacing.West;
                    return true;

                case UnitFacing.West:
                    RotatedFacing = UnitFacing.South;
                    return true;

                case UnitFacing.South:
                    RotatedFacing = UnitFacing.East;
                    return true;

                case UnitFacing.East:
                    RotatedFacing = UnitFacing.North;
                    return true;
            }
        }


        RotatedFacing =
            default;

        return false;
    }
}

