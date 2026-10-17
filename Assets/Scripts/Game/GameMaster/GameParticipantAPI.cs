using System;
using System.Collections.Generic;
using UnityEngine;


// Participant-facing observation and input boundary.
// Owns no gameplay truth; GameMaster remains authoritative.
public sealed class GameParticipantAPI
{
    public sealed class MovementPresentationSegment
    {
        public GameMovement.RouteStep[] Steps { get; }


        internal MovementPresentationSegment(
            GameMovement.RouteStep[] Steps)
        {
            this.Steps =
                Steps ??
                Array.Empty<GameMovement.RouteStep>();
        }
    }


    public sealed class MovementPresentation
    {
        public bool IsOwnUnit { get; }
        public string RuntimeUnitID { get; }
        public string DefinitionID { get; }
        public string InstanceName { get; }
        public ParticipantUnitTagSnapshot[] Tags { get; }
        public bool RemainsVisible { get; }
        public MovementPresentationSegment[] Segments { get; }


        internal MovementPresentation(
            bool IsOwnUnit,
            string RuntimeUnitID,
            string DefinitionID,
            string InstanceName,
            ParticipantUnitTagSnapshot[] Tags,
            bool RemainsVisible,
            MovementPresentationSegment[] Segments)
        {
            this.IsOwnUnit =
                IsOwnUnit;

            this.RuntimeUnitID =
                RuntimeUnitID ??
                string.Empty;

            this.DefinitionID =
                DefinitionID ??
                string.Empty;

            this.InstanceName =
                InstanceName ??
                string.Empty;

            this.Tags =
                Tags ??
                Array.Empty<ParticipantUnitTagSnapshot>();

            this.RemainsVisible =
                RemainsVisible;

            this.Segments =
                Segments ??
                Array.Empty<MovementPresentationSegment>();
        }
    }


    public sealed class ParticipantAccess
    {
        private readonly GameParticipantAPI _API;

        public event Action SnapshotChanged;
        public event Action<MovementPresentation> MovementPresented;

        public RuntimeParticipant Participant { get; }

        public string DisplayName =>
            Participant?.Participant != null
                ? Participant.Participant.DisplayName
                : string.Empty;

        internal ParticipantAccess(
            GameParticipantAPI API,
            RuntimeParticipant Participant)
        {
            _API = API;
            this.Participant = Participant;
        }

        internal void NotifySnapshotChanged()
        {
            SnapshotChanged?.Invoke();
        }


        internal void NotifyMovementPresented(
            MovementPresentation Presentation)
        {
            if (Presentation != null)
            {
                MovementPresented?.Invoke(
                    Presentation
                );
            }
        }

        public bool TryGetSnapshot(
            out ParticipantGameSnapshot Snapshot,
            out string ValidationMessage)
        {
            Snapshot = null;

            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryGetParticipantSnapshot(
                Participant,
                out Snapshot,
                out ValidationMessage);
        }


        public bool TryGetDeploymentView(out GameDeployment.ParticipantView View)
        {
            View = null;
            if (!_API.IsInitialized)
            {
                return false;
            }

            return _API._GameMaster.TryGetDeploymentView(Participant, out View);
        }


        public bool TryGetInitialDeploymentFacing(out UnitFacing Facing)
        {
            Facing = default;
            if (!_API.IsInitialized)
            {
                return false;
            }

            return _API._GameMaster.TryGetInitialDeploymentFacing(Participant, out Facing);
        }


        public bool TryGetDeploymentZoneCoordinates(out Vector2Int[] Coordinates)
        {
            Coordinates = Array.Empty<Vector2Int>();
            if (!_API.IsInitialized)
            {
                return false;
            }

            return _API._GameMaster.TryGetDeploymentZoneCoordinates(Participant, out Coordinates);
        }


        public bool TryGetDeploymentPlacementTiles(string FleetEntryID, out RuntimeUnitTilePlacement[] Placements, out string ValidationMessage)
        {
            Placements = Array.Empty<RuntimeUnitTilePlacement>();
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryGetDeploymentPlacementTiles(Participant, FleetEntryID, out Placements, out ValidationMessage);
        }


        public bool ValidateDeploymentPlacement(string FleetEntryID, Vector2Int MovementPivotCoordinate, UnitFacing Facing, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.ValidateDeploymentPlacement(Participant, FleetEntryID, MovementPivotCoordinate, Facing, out ValidationMessage);
        }


        public bool TrySubmitDeploymentPlacement(string FleetEntryID, Vector2Int MovementPivotCoordinate, UnitFacing Facing, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TrySubmitDeploymentPlacement(Participant, FleetEntryID, MovementPivotCoordinate, Facing, out ValidationMessage);
        }


        public bool TrySelectDeploymentPlacement(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TrySelectDeploymentPlacement(Participant, FleetEntryID, out ValidationMessage);
        }


        public bool TryClearDeploymentSelection(out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryClearDeploymentSelection(Participant, out ValidationMessage);
        }


        public bool TryRotateDeploymentPlacementClockwise(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryRotateDeploymentPlacementClockwise(Participant, FleetEntryID, out ValidationMessage);
        }


        public bool TryRotateDeploymentPlacementCounterClockwise(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryRotateDeploymentPlacementCounterClockwise(Participant, FleetEntryID, out ValidationMessage);
        }


        public bool TryResetDeployment(out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryResetDeployment(Participant, out ValidationMessage);
        }


        public bool TryConfirmDeployment(out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Confirmed =
                _API._GameMaster.TryConfirmDeployment(
                    Participant,
                    out ValidationMessage);

            if (Confirmed)
                _API.NotifySnapshotsChanged();

            return Confirmed;
        }


        public bool TryUseAbility(
            string RuntimeUnitID,
            int AbilitySlotIndex,
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";

                return false;
            }


            bool Used =
                _API._GameMaster.TryUseAbility(
                    Participant,
                    RuntimeUnitID,
                    AbilitySlotIndex,
                    out ValidationMessage
                );


            if (Used)
            {
                _API.NotifySnapshotsChanged();
            }


            return Used;
        }


        public bool TryGetMovementView(
            out GameMovement.ParticipantView View
        )
        {
            View = null;


            if (!_API.IsInitialized)
            {
                return false;
            }


            return
                _API._GameMaster.TryGetMovementView(
                    Participant,
                    out View
                );
        }


        public bool ValidateMovement(
            string RuntimeUnitID,
            Vector2Int DestinationMovementPivotCoordinate,
            UnitFacing DestinationFacing,
            out GameMovement.MovementPlan Plan,
            out string ValidationMessage
        )
        {
            Plan = null;


            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";

                return false;
            }


            return
                _API._GameMaster.ValidateMovement(
                    Participant,
                    RuntimeUnitID,
                    DestinationMovementPivotCoordinate,
                    DestinationFacing,
                    out Plan,
                    out ValidationMessage
                );
        }


        public bool TryGetMovementArea(
            string RuntimeUnitID,
            out Vector2Int[] MovementArea,
            out string ValidationMessage
        )
        {
            MovementArea =
                Array.Empty<Vector2Int>();


            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";

                return false;
            }


            return
                _API._GameMaster.TryGetMovementArea(
                    Participant,
                    RuntimeUnitID,
                    out MovementArea,
                    out ValidationMessage
                );
        }


        public bool TryCommitMovement(
            string RuntimeUnitID,
            Vector2Int DestinationMovementPivotCoordinate,
            UnitFacing DestinationFacing,
            out GameMovement.CommitResult Result,
            out string ValidationMessage
        )
        {
            Result = null;


            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";

                return false;
            }


            bool Committed =
                _API._GameMaster.TryCommitMovement(
                    Participant,
                    RuntimeUnitID,
                    DestinationMovementPivotCoordinate,
                    DestinationFacing,
                    out Result,
                    out ValidationMessage
                );


            if (Committed)
            {
                _API.NotifyMovementPresented(
                    Participant,
                    Result
                );

                _API.NotifySnapshotsChanged();
            }


            return Committed;
        }


        public bool TryEndMovement(
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";

                return false;
            }


            bool Ended =
                _API._GameMaster.TryEndMovement(
                    Participant,
                    out ValidationMessage
                );


            if (Ended)
            {
                _API.NotifySnapshotsChanged();
            }


            return Ended;
        }

    }

    private GameMaster _GameMaster;
    private RuntimeParticipant _ParticipantA;
    private RuntimeParticipant _ParticipantB;
    private ParticipantAccess _ParticipantAAccess;
    private ParticipantAccess _ParticipantBAccess;

    public bool IsInitialized { get; private set; }

    internal bool Initialize(
        GameMaster GameMaster,
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB,
        out string ValidationMessage)
    {
        IsInitialized = false;

        if (GameMaster == null ||
            ParticipantA == null ||
            ParticipantB == null ||
            ReferenceEquals(ParticipantA, ParticipantB))
        {
            ValidationMessage =
                "GameMaster and two distinct runtime participants are required.";
            return false;
        }

        _GameMaster = GameMaster;
        _ParticipantA = ParticipantA;
        _ParticipantB = ParticipantB;
        _ParticipantAAccess = new ParticipantAccess(this, ParticipantA);
        _ParticipantBAccess = new ParticipantAccess(this, ParticipantB);
        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }

    public bool TryGetLocalPlayerAccess(
        out ParticipantAccess Access,
        out string ValidationMessage)
    {
        Access = null;

        if (!IsInitialized)
        {
            ValidationMessage = "Game Participant API is not initialized.";
            return false;
        }

        bool A = IsLocalPlayer(_ParticipantA);
        bool B = IsLocalPlayer(_ParticipantB);

        if (A == B)
        {
            ValidationMessage = A
                ? "More than one LocalPlayer participant exists."
                : "No LocalPlayer participant exists.";
            return false;
        }

        Access = A ? _ParticipantAAccess : _ParticipantBAccess;
        ValidationMessage = string.Empty;
        return true;
    }

    internal bool TryGetParticipantAccess(
        RuntimeParticipant Participant,
        out ParticipantAccess Access)
    {
        Access = null;

        if (!IsInitialized || Participant == null)
            return false;

        if (ReferenceEquals(Participant, _ParticipantA))
            Access = _ParticipantAAccess;
        else if (ReferenceEquals(Participant, _ParticipantB))
            Access = _ParticipantBAccess;

        return Access != null;
    }

    internal void NotifySnapshotsChanged()
    {
        _ParticipantAAccess?.NotifySnapshotChanged();
        _ParticipantBAccess?.NotifySnapshotChanged();
    }


    private void NotifyMovementPresented(
        RuntimeParticipant ActingParticipant,
        GameMovement.CommitResult Result)
    {
        NotifyMovementPresented(
            _ParticipantAAccess,
            ActingParticipant,
            Result,
            Result?.ParticipantAVisibility
        );

        NotifyMovementPresented(
            _ParticipantBAccess,
            ActingParticipant,
            Result,
            Result?.ParticipantBVisibility
        );
    }


    private static void NotifyMovementPresented(
        ParticipantAccess Access,
        RuntimeParticipant ActingParticipant,
        GameMovement.CommitResult Result,
        bool[] Visibility)
    {
        if (Access == null ||
            Result == null ||
            Result.TraveledRoute == null ||
            Result.TraveledRoute.Length == 0)
        {
            return;
        }


        bool IsOwnUnit =
            ReferenceEquals(
                Access.Participant,
                ActingParticipant
            );

        List<MovementPresentationSegment> Segments =
            new List<MovementPresentationSegment>();


        if (IsOwnUnit)
        {
            Segments.Add(
                new MovementPresentationSegment(
                    Result.TraveledRoute
                )
            );
        }
        else
        {
            List<GameMovement.RouteStep> VisibleSteps =
                null;


            for (int i = 0; i < Result.TraveledRoute.Length; i++)
            {
                bool IsVisible =
                    Visibility != null &&
                    i < Visibility.Length &&
                    Visibility[i];


                if (IsVisible)
                {
                    if (VisibleSteps == null)
                    {
                        VisibleSteps =
                            new List<GameMovement.RouteStep>();
                    }


                    VisibleSteps.Add(
                        Result.TraveledRoute[i]
                    );
                }
                else if (VisibleSteps != null)
                {
                    Segments.Add(
                        new MovementPresentationSegment(
                            VisibleSteps.ToArray()
                        )
                    );

                    VisibleSteps =
                        null;
                }
            }


            if (VisibleSteps != null)
            {
                Segments.Add(
                    new MovementPresentationSegment(
                        VisibleSteps.ToArray()
                    )
                );
            }
        }


        if (Segments.Count == 0)
        {
            return;
        }


        bool RemainsVisible =
            IsOwnUnit ||
            (Visibility != null &&
                Visibility.Length > 0 &&
                Visibility[Visibility.Length - 1]);

        Access.NotifyMovementPresented(
            new MovementPresentation(
                IsOwnUnit,
                IsOwnUnit
                    ? Result.RuntimeUnitID
                    : string.Empty,
                Result.DefinitionID,
                Result.InstanceName,
                Result.Tags,
                RemainsVisible,
                Segments.ToArray()
            )
        );
    }

    private static bool IsLocalPlayer(RuntimeParticipant Participant)
    {
        return Participant?.Participant != null &&
            Participant.Participant.ParticipantType ==
                ParticipantType.LocalPlayer;
    }
}




