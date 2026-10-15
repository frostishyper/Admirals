using System.Collections.Generic;
using UnityEngine;


// Authoritative tentative state for Match Deployment.
//
// This does not own RuntimeUnits.
// It references the existing top-level Fleet Entry states in GameUnits.
//
// RuntimeUnits are still created only when GameMaster commits
// a participant's confirmed Deployment.
public sealed class GameDeployment
{
    public sealed class Placement
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


        internal void SetFacing(
            UnitFacing Facing
        )
        {
            _Facing =
                Facing;


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


    public sealed class ParticipantPlan
    {
        private readonly List<Placement> _Placements =
            new List<Placement>();


        private readonly Dictionary<string, Placement> _PlacementsByID =
            new Dictionary<string, Placement>();


        public ParticipantUnitRoster UnitRoster
        {
            get;
        }


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


                for (
                    int Index = 0;
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


                for (
                    int Index = 0;
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


            for (
                int Index = 0;
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


        public bool TrySelect(
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


        public void ClearSelection()
        {
            SelectedPlacement =
                null;
        }


        // GameMaster / Deployment interaction calls this only after
        // the candidate footprint has passed legality validation.
        public bool TrySetPlacement(
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


        public bool TrySetFacing(
            string FleetEntryID,
            UnitFacing Facing
        )
        {
            if (!TryGetPlacement(
                    FleetEntryID,
                    out Placement FoundPlacement))
            {
                return false;
            }


            FoundPlacement.SetFacing(
                Facing
            );


            SelectedPlacement =
                FoundPlacement;


            return true;
        }


        public bool TryClearPlacement(
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


            if (SelectedPlacement ==
                FoundPlacement)
            {
                SelectedPlacement =
                    null;
            }


            return true;
        }


        public void Reset()
        {
            for (
                int Index = 0;
                Index < _Placements.Count;
                Index++)
            {
                _Placements[Index].Reset();
            }


            SelectedPlacement =
                null;
        }
    }


    public ParticipantPlan ParticipantA
    {
        get;
        private set;
    }


    public ParticipantPlan ParticipantB
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
        ParticipantUnitRoster ParticipantAUnitRoster,
        ParticipantUnitRoster ParticipantBUnitRoster,
        out string ValidationMessage
    )
    {
        IsInitialized =
            false;


        ParticipantA =
            new ParticipantPlan(
                ParticipantAUnitRoster
            );


        if (!ParticipantA.Initialize(
                out ValidationMessage))
        {
            ParticipantA =
                null;


            ParticipantB =
                null;


            return false;
        }


        ParticipantB =
            new ParticipantPlan(
                ParticipantBUnitRoster
            );


        if (!ParticipantB.Initialize(
                out ValidationMessage))
        {
            ParticipantA =
                null;


            ParticipantB =
                null;


            return false;
        }


        IsInitialized =
            true;


        ValidationMessage =
            string.Empty;


        return true;
    }
}