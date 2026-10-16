using System;
using UnityEngine;


// Participant-facing observation and input boundary.
// Owns no gameplay truth; GameMaster remains authoritative.
public sealed class GameParticipantAPI
{
    public sealed class ParticipantAccess
    {
        private readonly GameParticipantAPI _API;

        public event Action SnapshotChanged;

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

    private void NotifySnapshotsChanged()
    {
        _ParticipantAAccess?.NotifySnapshotChanged();
        _ParticipantBAccess?.NotifySnapshotChanged();
    }

    private static bool IsLocalPlayer(RuntimeParticipant Participant)
    {
        return Participant?.Participant != null &&
            Participant.Participant.ParticipantType ==
                ParticipantType.LocalPlayer;
    }
}




