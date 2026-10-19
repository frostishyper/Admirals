using System;
using System.Collections.Generic;
using UnityEngine;


// Participant-facing observation and input boundary.
// Owns no gameplay truth; GameMaster remains authoritative.
public sealed class GameParticipantAPI
{
    public enum ParticipantCommandKind
    {
        None = 0,
        PlaceDeployment = 1,
        ResetDeployment = 2,
        ConfirmDeployment = 3,
        UseAbility = 4,
        CommitMovement = 5,
        EndMovement = 6,
        DeployMine = 7,
        DeployCarriedUnit = 8,
        EndPreBattle = 9,
        CommitSearch = 10,
        EndSearch = 11,
        BeginEngagement = 12,
        CancelEngagement = 13,
        LockBattleAttack = 14,
        LockBattleDefense = 15,
        ContinueEngagement = 16,
        EndBattle = 17,
        FinishEndPhase = 18
    }


    // Plain participant input produced from one observed snapshot. It contains
    // no participant identity: the ParticipantAccess receiving it supplies the
    // actor, and GameMaster remains responsible for authoritative validation.
    public sealed class ParticipantCommand
    {
        public ParticipantCommandKind Kind { get; }
        public GameplayPhase ExpectedPhase { get; }
        public int ExpectedTurnNumber { get; }
        public string RuntimeUnitID { get; }
        public string FleetEntryID { get; }
        public Vector2Int Coordinate { get; }
        public UnitFacing Facing { get; }
        public int SlotIndex { get; }
        public int CarriedUnitIndex { get; }
        public GameBattle.CombatActionKind ActionKind { get; }
        public string SupportRuntimeUnitID { get; }
        public GameBattle.CombatActionKind SupportActionKind { get; }
        public int SupportSlotIndex { get; }

        private ParticipantCommand(ParticipantCommandKind Kind,
            ParticipantGameSnapshot Observation, string RuntimeUnitID = "",
            string FleetEntryID = "", Vector2Int Coordinate = default,
            UnitFacing Facing = default, int SlotIndex = -1,
            int CarriedUnitIndex = -1,
            GameBattle.CombatActionKind ActionKind =
                GameBattle.CombatActionKind.None,
            string SupportRuntimeUnitID = "",
            GameBattle.CombatActionKind SupportActionKind =
                GameBattle.CombatActionKind.None,
            int SupportSlotIndex = -1)
        {
            this.Kind = Kind;
            ExpectedPhase = Observation?.CurrentPhase ?? GameplayPhase.None;
            ExpectedTurnNumber = Observation?.TurnNumber ?? -1;
            this.RuntimeUnitID = RuntimeUnitID ?? string.Empty;
            this.FleetEntryID = FleetEntryID ?? string.Empty;
            this.Coordinate = Coordinate;
            this.Facing = Facing;
            this.SlotIndex = SlotIndex;
            this.CarriedUnitIndex = CarriedUnitIndex;
            this.ActionKind = ActionKind;
            this.SupportRuntimeUnitID = SupportRuntimeUnitID ?? string.Empty;
            this.SupportActionKind = SupportActionKind;
            this.SupportSlotIndex = SupportSlotIndex;
        }

        public static ParticipantCommand PlaceDeployment(
            ParticipantGameSnapshot Observation, string FleetEntryID,
            Vector2Int MovementPivotCoordinate, UnitFacing Facing) =>
            new ParticipantCommand(ParticipantCommandKind.PlaceDeployment,
                Observation, FleetEntryID: FleetEntryID,
                Coordinate: MovementPivotCoordinate, Facing: Facing);

        public static ParticipantCommand ResetDeployment(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.ResetDeployment,
                Observation);

        public static ParticipantCommand ConfirmDeployment(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.ConfirmDeployment,
                Observation);

        public static ParticipantCommand UseAbility(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            int AbilitySlotIndex) =>
            new ParticipantCommand(ParticipantCommandKind.UseAbility,
                Observation, RuntimeUnitID: RuntimeUnitID,
                SlotIndex: AbilitySlotIndex);

        public static ParticipantCommand CommitMovement(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            Vector2Int DestinationMovementPivotCoordinate,
            UnitFacing DestinationFacing) =>
            new ParticipantCommand(ParticipantCommandKind.CommitMovement,
                Observation, RuntimeUnitID: RuntimeUnitID,
                Coordinate: DestinationMovementPivotCoordinate,
                Facing: DestinationFacing);

        public static ParticipantCommand EndMovement(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.EndMovement,
                Observation);

        public static ParticipantCommand DeployMine(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            int SystemSlotIndex, Vector2Int Coordinate) =>
            new ParticipantCommand(ParticipantCommandKind.DeployMine,
                Observation, RuntimeUnitID: RuntimeUnitID,
                Coordinate: Coordinate, SlotIndex: SystemSlotIndex);

        public static ParticipantCommand DeployCarriedUnit(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            int SystemSlotIndex, int CarriedUnitIndex,
            Vector2Int MovementPivotCoordinate, UnitFacing Facing) =>
            new ParticipantCommand(ParticipantCommandKind.DeployCarriedUnit,
                Observation, RuntimeUnitID: RuntimeUnitID,
                Coordinate: MovementPivotCoordinate, Facing: Facing,
                SlotIndex: SystemSlotIndex,
                CarriedUnitIndex: CarriedUnitIndex);

        public static ParticipantCommand EndPreBattle(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.EndPreBattle,
                Observation);

        public static ParticipantCommand CommitSearch(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            int SystemSlotIndex, UnitFacing SearchFacing) =>
            new ParticipantCommand(ParticipantCommandKind.CommitSearch,
                Observation, RuntimeUnitID: RuntimeUnitID,
                Facing: SearchFacing, SlotIndex: SystemSlotIndex);

        public static ParticipantCommand EndSearch(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.EndSearch,
                Observation);

        public static ParticipantCommand BeginEngagement(
            ParticipantGameSnapshot Observation, string RuntimeUnitID,
            Vector2Int TargetCoordinate) =>
            new ParticipantCommand(ParticipantCommandKind.BeginEngagement,
                Observation, RuntimeUnitID: RuntimeUnitID,
                Coordinate: TargetCoordinate);

        public static ParticipantCommand CancelEngagement(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.CancelEngagement,
                Observation);

        public static ParticipantCommand LockBattleAttack(
            ParticipantGameSnapshot Observation,
            GameBattle.CombatActionKind Kind, int SlotIndex) =>
            new ParticipantCommand(ParticipantCommandKind.LockBattleAttack,
                Observation, ActionKind: Kind, SlotIndex: SlotIndex);

        public static ParticipantCommand LockBattleDefense(
            ParticipantGameSnapshot Observation,
            GameBattle.CombatActionKind DefenseKind, int DefenseSlotIndex,
            string SupportRuntimeUnitID,
            GameBattle.CombatActionKind SupportKind, int SupportSlotIndex) =>
            new ParticipantCommand(ParticipantCommandKind.LockBattleDefense,
                Observation, ActionKind: DefenseKind,
                SlotIndex: DefenseSlotIndex,
                SupportRuntimeUnitID: SupportRuntimeUnitID,
                SupportActionKind: SupportKind,
                SupportSlotIndex: SupportSlotIndex);

        public static ParticipantCommand ContinueEngagement(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.ContinueEngagement,
                Observation);

        public static ParticipantCommand EndBattle(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.EndBattle,
                Observation);

        public static ParticipantCommand FinishEndPhase(
            ParticipantGameSnapshot Observation) =>
            new ParticipantCommand(ParticipantCommandKind.FinishEndPhase,
                Observation);
    }


    public sealed class ParticipantCommandResult
    {
        public ParticipantCommand Command { get; }
        public bool Accepted { get; }
        public bool RejectedAsStale { get; }
        public string ValidationMessage { get; }
        public GameplayPhase CurrentPhase { get; }
        public int CurrentTurnNumber { get; }
        public GameMovement.CommitResult Movement { get; }
        public GameSearch.CommitResult Search { get; }
        public GameBattle.ResolutionView BattleResolution { get; }

        internal ParticipantCommandResult(ParticipantCommand Command,
            bool Accepted, bool RejectedAsStale, string ValidationMessage,
            GameplayPhase CurrentPhase, int CurrentTurnNumber,
            GameMovement.CommitResult Movement = null,
            GameSearch.CommitResult Search = null,
            GameBattle.ResolutionView BattleResolution = null)
        {
            this.Command = Command;
            this.Accepted = Accepted;
            this.RejectedAsStale = RejectedAsStale;
            this.ValidationMessage = ValidationMessage ?? string.Empty;
            this.CurrentPhase = CurrentPhase;
            this.CurrentTurnNumber = CurrentTurnNumber;
            this.Movement = Movement;
            this.Search = Search;
            this.BattleResolution = BattleResolution;
        }
    }


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
        public event Action<GameSearch.CommitResult> SearchPresented;

        public RuntimeParticipant Participant { get; }

        public ParticipantType ParticipantType =>
            Participant?.Participant != null
                ? Participant.Participant.ParticipantType
                : default;

        public Definition_Fleet Fleet =>
            Participant?.Fleet;

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


        internal void NotifySearchPresented(GameSearch.CommitResult Result)
        {
            if (Result != null)
                SearchPresented?.Invoke(Result);
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


        // Unified command entry point for AI, networking, and other non-UI
        // controllers. Dispatches into the same typed methods used by the human
        // controller, so no command has a separate validation/rules path.
        public ParticipantCommandResult SubmitCommand(
            ParticipantCommand Command)
        {
            if (Command == null)
            {
                return new ParticipantCommandResult(null, false, false,
                    "Participant command is required.", GameplayPhase.None,
                    -1);
            }

            if (!TryGetSnapshot(out ParticipantGameSnapshot Before,
                    out string SnapshotMessage))
            {
                return new ParticipantCommandResult(Command, false, false,
                    SnapshotMessage, GameplayPhase.None, -1);
            }

            if (Command.ExpectedPhase != Before.CurrentPhase ||
                Command.ExpectedTurnNumber != Before.TurnNumber)
            {
                return new ParticipantCommandResult(Command, false, true,
                    "The command was produced from a stale phase or turn observation.",
                    Before.CurrentPhase, Before.TurnNumber);
            }

            bool Accepted;
            string Message;
            GameMovement.CommitResult Movement = null;
            GameSearch.CommitResult Search = null;
            GameBattle.ResolutionView BattleResolution = null;

            switch (Command.Kind)
            {
                case ParticipantCommandKind.PlaceDeployment:
                    Accepted = TrySubmitDeploymentPlacement(
                        Command.FleetEntryID, Command.Coordinate,
                        Command.Facing, out Message);
                    break;

                case ParticipantCommandKind.ResetDeployment:
                    Accepted = TryResetDeployment(out Message);
                    break;

                case ParticipantCommandKind.ConfirmDeployment:
                    Accepted = TryConfirmDeployment(out Message);
                    break;

                case ParticipantCommandKind.UseAbility:
                    Accepted = TryUseAbility(Command.RuntimeUnitID,
                        Command.SlotIndex, out Message);
                    break;

                case ParticipantCommandKind.CommitMovement:
                    Accepted = TryCommitMovement(Command.RuntimeUnitID,
                        Command.Coordinate, Command.Facing, out Movement,
                        out Message);
                    break;

                case ParticipantCommandKind.EndMovement:
                    Accepted = TryEndMovement(out Message);
                    break;

                case ParticipantCommandKind.DeployMine:
                    Accepted = TryDeployMine(Command.RuntimeUnitID,
                        Command.SlotIndex, Command.Coordinate, out Message);
                    break;

                case ParticipantCommandKind.DeployCarriedUnit:
                    Accepted = TryDeployCarriedUnit(Command.RuntimeUnitID,
                        Command.SlotIndex, Command.CarriedUnitIndex,
                        Command.Coordinate, Command.Facing, out Message);
                    break;

                case ParticipantCommandKind.EndPreBattle:
                    Accepted = TryEndPreBattle(out Message);
                    break;

                case ParticipantCommandKind.CommitSearch:
                    Accepted = TryCommitSearch(Command.RuntimeUnitID,
                        Command.SlotIndex, Command.Facing, out Search,
                        out Message);
                    break;

                case ParticipantCommandKind.EndSearch:
                    Accepted = TryEndSearch(out Message);
                    break;

                case ParticipantCommandKind.BeginEngagement:
                    Accepted = TryBeginEngagement(Command.RuntimeUnitID,
                        Command.Coordinate, out Message);
                    break;

                case ParticipantCommandKind.CancelEngagement:
                    Accepted = TryCancelEngagement(out Message);
                    break;

                case ParticipantCommandKind.LockBattleAttack:
                    Accepted = TryLockBattleAttack(Command.ActionKind,
                        Command.SlotIndex, out Message);
                    break;

                case ParticipantCommandKind.LockBattleDefense:
                    Accepted = TryLockBattleDefense(Command.ActionKind,
                        Command.SlotIndex, Command.SupportRuntimeUnitID,
                        Command.SupportActionKind, Command.SupportSlotIndex,
                        out BattleResolution, out Message);
                    break;

                case ParticipantCommandKind.ContinueEngagement:
                    Accepted = TryContinueEngagement(out Message);
                    break;

                case ParticipantCommandKind.EndBattle:
                    Accepted = TryEndBattle(out Message);
                    break;

                case ParticipantCommandKind.FinishEndPhase:
                    Accepted = TryFinishEndPhase(out Message);
                    break;

                default:
                    Accepted = false;
                    Message = "Participant command kind is not supported.";
                    break;
            }

            GameplayPhase PhaseAfter = Before.CurrentPhase;
            int TurnAfter = Before.TurnNumber;
            if (TryGetSnapshot(out ParticipantGameSnapshot After, out _))
            {
                PhaseAfter = After.CurrentPhase;
                TurnAfter = After.TurnNumber;
            }

            return new ParticipantCommandResult(Command, Accepted, false,
                Message, PhaseAfter, TurnAfter, Movement, Search,
                BattleResolution);
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

            bool Submitted = _API._GameMaster.TrySubmitDeploymentPlacement(
                Participant, FleetEntryID, MovementPivotCoordinate, Facing,
                out ValidationMessage);
            if (Submitted)
                _API.NotifySnapshotsChanged();
            return Submitted;
        }


        public bool TrySelectDeploymentPlacement(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Selected = _API._GameMaster.TrySelectDeploymentPlacement(
                Participant, FleetEntryID, out ValidationMessage);
            if (Selected)
                _API.NotifySnapshotsChanged();
            return Selected;
        }


        public bool TryClearDeploymentSelection(out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Cleared = _API._GameMaster.TryClearDeploymentSelection(
                Participant, out ValidationMessage);
            if (Cleared)
                _API.NotifySnapshotsChanged();
            return Cleared;
        }


        public bool TryRotateDeploymentPlacementClockwise(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Rotated = _API._GameMaster.TryRotateDeploymentPlacementClockwise(
                Participant, FleetEntryID, out ValidationMessage);
            if (Rotated)
                _API.NotifySnapshotsChanged();
            return Rotated;
        }


        public bool TryRotateDeploymentPlacementCounterClockwise(string FleetEntryID, out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Rotated = _API._GameMaster.TryRotateDeploymentPlacementCounterClockwise(
                Participant, FleetEntryID, out ValidationMessage);
            if (Rotated)
                _API.NotifySnapshotsChanged();
            return Rotated;
        }


        public bool TryResetDeployment(out string ValidationMessage)
        {
            
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }

            bool Reset = _API._GameMaster.TryResetDeployment(
                Participant, out ValidationMessage);
            if (Reset)
                _API.NotifySnapshotsChanged();
            return Reset;
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


        public bool TryGetPreBattleView(
            out GamePreBattle.ParticipantView View
        )
        {
            View = null;

            if (!_API.IsInitialized)
                return false;

            return _API._GameMaster.TryGetPreBattleView(
                Participant,
                out View
            );
        }


        public bool TryGetSearchView(out GameSearch.ParticipantView View)
        {
            View = null;
            return _API.IsInitialized &&
                _API._GameMaster.TryGetSearchView(Participant, out View);
        }


        public bool TryGetSearchPattern(
            string RuntimeUnitID,
            int SystemSlotIndex,
            UnitFacing SearchFacing,
            out GameSearch.SearchTile[] Tiles,
            out string ValidationMessage)
        {
            Tiles = Array.Empty<GameSearch.SearchTile>();
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryGetSearchPattern(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                SearchFacing,
                out Tiles,
                out ValidationMessage);
        }


        public bool TryCommitSearch(
            string RuntimeUnitID,
            int SystemSlotIndex,
            UnitFacing SearchFacing,
            out GameSearch.CommitResult Result,
            out string ValidationMessage)
        {
            Result = null;
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Committed = _API._GameMaster.TryCommitSearch(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                SearchFacing,
                out Result,
                out ValidationMessage);

            if (Committed)
            {
                NotifySearchPresented(Result);
                _API.NotifySnapshotsChanged();
            }
            return Committed;
        }


        public bool TryEndSearch(out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Ended = _API._GameMaster.TryEndSearch(
                Participant,
                out ValidationMessage);
            if (Ended)
                _API.NotifySnapshotsChanged();
            return Ended;
        }


        public bool TryGetMineDeploymentArea(
            string RuntimeUnitID,
            int SystemSlotIndex,
            out Vector2Int[] Coordinates,
            out string ValidationMessage
        )
        {
            Coordinates = Array.Empty<Vector2Int>();

            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryGetMineDeploymentArea(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                out Coordinates,
                out ValidationMessage
            );
        }


        public bool ValidateMineDeployment(
            string RuntimeUnitID,
            int SystemSlotIndex,
            Vector2Int Coordinate,
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.ValidateMineDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                Coordinate,
                out ValidationMessage
            );
        }


        public bool TryDeployMine(
            string RuntimeUnitID,
            int SystemSlotIndex,
            Vector2Int Coordinate,
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Deployed = _API._GameMaster.TryDeployMine(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                Coordinate,
                out ValidationMessage
            );

            if (Deployed)
                _API.NotifySnapshotsChanged();

            return Deployed;
        }


        public bool TryGetUnitDeploymentArea(
            string RuntimeUnitID,
            int SystemSlotIndex,
            int CarriedUnitIndex,
            UnitFacing Facing,
            out Vector2Int[] Coordinates,
            out string ValidationMessage
        )
        {
            Coordinates = Array.Empty<Vector2Int>();

            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.TryGetUnitDeploymentArea(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                Facing,
                out Coordinates,
                out ValidationMessage
            );
        }


        public bool ValidateUnitDeployment(
            string RuntimeUnitID,
            int SystemSlotIndex,
            int CarriedUnitIndex,
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing,
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            return _API._GameMaster.ValidateUnitDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage
            );
        }


        public bool TryDeployCarriedUnit(
            string RuntimeUnitID,
            int SystemSlotIndex,
            int CarriedUnitIndex,
            Vector2Int MovementPivotCoordinate,
            UnitFacing Facing,
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Deployed = _API._GameMaster.TryDeployCarriedUnit(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage
            );

            if (Deployed)
                _API.NotifySnapshotsChanged();

            return Deployed;
        }


        public bool TryEndPreBattle(
            out string ValidationMessage
        )
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Ended = _API._GameMaster.TryEndPreBattle(
                Participant,
                out ValidationMessage
            );

            if (Ended)
                _API.NotifySnapshotsChanged();

            return Ended;
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


        public bool TryGetBattleView(out GameBattle.ParticipantView View)
        {
            View = null;
            return _API.IsInitialized &&
                _API._GameMaster.TryGetBattleView(Participant, out View);
        }


        public bool TryGetFireControlArea(string RuntimeUnitID, Domain Domain,
            out Vector2Int[] Coordinates, out string ValidationMessage)
        {
            Coordinates = Array.Empty<Vector2Int>();
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }
            return _API._GameMaster.TryGetFireControlArea(Participant,
                RuntimeUnitID, Domain, out Coordinates, out ValidationMessage);
        }


        public bool TryGetBattleTarget(string RuntimeUnitID,
            Vector2Int Coordinate, out GameBattle.TargetView Target,
            out string ValidationMessage)
        {
            Target = null;
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }
            return _API._GameMaster.TryGetBattleTarget(Participant,
                RuntimeUnitID, Coordinate, out Target, out ValidationMessage);
        }


        public bool TryBeginEngagement(string RuntimeUnitID,
            Vector2Int TargetCoordinate, out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }
            bool Began = _API._GameMaster.TryBeginEngagement(Participant,
                RuntimeUnitID, TargetCoordinate, out ValidationMessage);
            if (Began)
                _API.NotifySnapshotsChanged();
            return Began;
        }


        public bool TryCancelEngagement(out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }
            bool Cancelled = _API._GameMaster.TryCancelEngagement(
                Participant, out ValidationMessage);
            if (Cancelled)
                _API.NotifySnapshotsChanged();
            return Cancelled;
        }


        public bool TryLockBattleAttack(GameBattle.CombatActionKind Kind,
            int SlotIndex, out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }
            bool Locked = _API._GameMaster.TryLockBattleAttack(
                Participant, Kind, SlotIndex, out ValidationMessage);
            if (Locked)
                _API.NotifySnapshotsChanged();
            return Locked;
        }


        public bool TryLockBattleDefense(GameBattle.CombatActionKind DefenseKind,
            int DefenseSlotIndex, string SupportRuntimeUnitID,
            GameBattle.CombatActionKind SupportKind, int SupportSlotIndex,
            out GameBattle.ResolutionView Resolution,
            out string ValidationMessage)
        {
            Resolution = null;
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }
            bool Locked = _API._GameMaster.TryLockBattleDefense(Participant,
                DefenseKind, DefenseSlotIndex, SupportRuntimeUnitID,
                SupportKind, SupportSlotIndex, out Resolution,
                out ValidationMessage);
            if (Locked)
                _API.NotifySnapshotsChanged();
            return Locked;
        }


        public bool TryContinueEngagement(out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage = "Participant gameplay access is not initialized.";
                return false;
            }
            bool Continued = _API._GameMaster.TryContinueEngagement(
                Participant, out ValidationMessage);
            if (Continued)
                _API.NotifySnapshotsChanged();
            return Continued;
        }


        public bool TryEndBattle(out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }
            bool Ended = _API._GameMaster.TryEndBattle(
                Participant, out ValidationMessage);
            if (Ended)
                _API.NotifySnapshotsChanged();
            return Ended;
        }


        public bool TryGetEndView(out GameEnd.ParticipantView View)
        {
            View = null;
            return _API.IsInitialized &&
                _API._GameMaster.TryGetEndView(Participant, out View);
        }


        public bool TryFinishEndPhase(out string ValidationMessage)
        {
            if (!_API.IsInitialized)
            {
                ValidationMessage =
                    "Participant gameplay access is not initialized.";
                return false;
            }

            bool Finished = _API._GameMaster.TryFinishEndPhase(
                Participant, out ValidationMessage);
            if (Finished)
                _API.NotifySnapshotsChanged();
            return Finished;
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


    // Controller bootstrap for the currently supported local-vs-AI match.
    // Once acquired, AI code should retain only this participant-bound access
    // object and must not select a participant on individual gameplay calls.
    public bool TryGetAIPlayerAccess(
        out ParticipantAccess Access,
        out string ValidationMessage)
    {
        Access = null;

        if (!IsInitialized)
        {
            ValidationMessage = "Game Participant API is not initialized.";
            return false;
        }

        bool A = IsAIPlayer(_ParticipantA);
        bool B = IsAIPlayer(_ParticipantB);

        if (A == B)
        {
            ValidationMessage = A
                ? "More than one AI participant exists; the controller host must bind an exact participant."
                : "No AI participant exists.";
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


    private static bool IsAIPlayer(RuntimeParticipant Participant)
    {
        return Participant != null &&
            Participant.Participant != null &&
            Participant.Participant.ParticipantType ==
                ParticipantType.AI;
    }
}




