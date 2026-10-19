using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative gameplay orchestrator.
//
// GameMaster owns:
// - finalized Match initialization;
// - runtime Participants;
// - phase and Match lifecycle;
// - construction and coordination of authoritative subsystems;
// - participant-aware gameplay input boundaries;
// - future cross-system transactions and phase transitions.
//
// Domain rules are delegated to substantial subordinate systems.
// Presentation and controller objects are never authoritative.
public class GameMaster :
    MonoBehaviour
{
    private enum ConditionEvaluationMoment
    {
        Immediate = 0,
        EndPhase = 1
    }


    private MatchConfig _MatchConfig;

    private Definition_GameMode _GameMode;


    private RuntimeParticipant _ParticipantA;

    private RuntimeParticipant _ParticipantB;


    private GameObject _MapInstance;

    private MapAuthoring _MapAuthoring;

    private GameMap _GameMap;


    private GameUnits _GameUnits;

    private GameUnitEffects _GameUnitEffects;

    private GameDeployment _GameDeployment;

    private GameKnowledge _GameKnowledge;

    private GameMovement _GameMovement;

    private GamePreBattle _GamePreBattle;

    private GameSearch _GameSearch;

    private GameBattle _GameBattle;

    private GameEnd _GameEnd;

    private GameParticipantAPI _ParticipantAPI;

    private readonly List<GameObject> _AIControllerInstances =
        new List<GameObject>();


    private GameplayPhase _CurrentPhase =
        GameplayPhase.None;


    private bool _InitializationComplete;

    private bool _MatchStarted;

    private RuntimeParticipant _FirstPlayer;

    private int _TurnNumber;
    private int _CompletedTurnNumber;

    private int _ParticipantAInitialFleetSize;
    private int _ParticipantBInitialFleetSize;
    private int _ParticipantAScore;
    private int _ParticipantBScore;
    private bool _MatchComplete;
    private RuntimeParticipant _WinningParticipant;
    private bool _MatchDraw;
    private string _MatchResolutionReason = string.Empty;

    // Local privacy handoffs pause phase clocks without changing global
    // Time.timeScale or affecting ordinary Singleplayer matches.
    private bool _PassAndPlayHandoffPaused;


    public MatchConfig MatchConfig =>
        _MatchConfig;


    public Definition_GameMode GameMode =>
        _GameMode;


    public RuntimeParticipant ParticipantA =>
        _ParticipantA;


    public RuntimeParticipant ParticipantB =>
        _ParticipantB;


    public GameObject MapInstance =>
        _MapInstance;


    public MapAuthoring MapAuthoring =>
        _MapAuthoring;


    public GameMap GameMap =>
        _GameMap;


    public GameUnits GameUnits =>
        _GameUnits;


    public GameDeployment GameDeployment =>
        _GameDeployment;


    internal GameKnowledge GameKnowledge =>
        _GameKnowledge;


    public GameMovement GameMovement =>
        _GameMovement;


    public GamePreBattle GamePreBattle =>
        _GamePreBattle;


    public GameSearch GameSearch =>
        _GameSearch;


    public GameBattle GameBattle =>
        _GameBattle;


    public GameEnd GameEnd =>
        _GameEnd;


    public GameParticipantAPI ParticipantAPI =>
        _ParticipantAPI;


    public GameplayPhase CurrentPhase =>
        _CurrentPhase;


    public bool InitializationComplete =>
        _InitializationComplete;


    public bool MatchStarted =>
        _MatchStarted;


    public RuntimeParticipant FirstPlayer =>
        _FirstPlayer;


    public int TurnNumber =>
        _TurnNumber;


    public bool MatchComplete =>
        _MatchComplete;


    public RuntimeParticipant WinningParticipant =>
        _WinningParticipant;


    public bool MatchDraw =>
        _MatchDraw;


    internal void SetPassAndPlayHandoffPaused(bool Paused)
    {
        _PassAndPlayHandoffPaused =
            _MatchConfig != null &&
            _MatchConfig.IsPassAndPlay &&
            Paused;
    }


    private void Start()
    {
        InitializeMatch();
    }


    public bool InitializeMatch()
    {
        if (_InitializationComplete)
        {
            return true;
        }


        if (MatchSession.Instance == null)
        {
            FailInitialization(
                "No MatchSession exists. " +
                "The Game scene must be entered through a finalized Match."
            );

            return false;
        }


        MatchConfig Match =
            MatchSession.Instance.ActiveMatch;


        if (Match == null)
        {
            FailInitialization(
                "MatchSession contains no ActiveMatch."
            );

            return false;
        }


        if (!Match.ValidateGameplayReady(
                out string ValidationMessage))
        {
            FailInitialization(
                $"MatchConfig is invalid: {ValidationMessage}"
            );

            return false;
        }


        _MatchConfig =
            Match;

        _GameMode =
            Match.GameMode;


        if (!InitializeParticipants() ||
            !InitializeMap() ||
            !InitializeUnitState() ||
            !InitializeUnitEffects() ||
            !InitializeDeploymentState() ||
            !InitializeKnowledgeState() ||
            !InitializeMovementState() ||
            !InitializePreBattleState() ||
            !InitializeSearchState() ||
            !InitializeBattleState() ||
            !InitializeEndState() ||
            !InitializeParticipantAPI())
        {
            return false;
        }


        PrepareDeployment();

        _InitializationComplete =
            true;


        if (!InitializeAIControllers(
                out string AIControllerMessage))
        {
            FailInitialization(AIControllerMessage);
            return false;
        }


        Debug.Log(
            $"GameMaster initialized Match '{_GameMode.Name}'. " +
            $"Map: '{_MapInstance.name}'. " +
            $"Runtime Board: {_GameMap.MapSize.x}x{_GameMap.MapSize.y} " +
            $"({_GameMap.TileCount} Tiles). " +
            $"Participant A Fleet: '{_ParticipantA.Fleet.Name}' " +
            $"({_GameDeployment.ParticipantA.TotalCount} deployable Fleet Entries, " +
            $"{_GameUnits.ParticipantA.RemainingFleetSize} total Units including carried). " +
            $"Participant B Fleet: '{_ParticipantB.Fleet.Name}' " +
            $"({_GameDeployment.ParticipantB.TotalCount} deployable Fleet Entries, " +
            $"{_GameUnits.ParticipantB.RemainingFleetSize} total Units including carried). " +
            $"Runtime Units created: 0. " +
            $"Awaiting Deployment.",
            this
        );


        return true;
    }


    // Participant-filtered Deployment output.
    //
    // Human and AI controllers receive only the requested participant's
    // read-only view. The authoritative ParticipantPlan remains internal to
    // GameDeployment.
    internal bool TryGetDeploymentView(
        RuntimeParticipant Participant,
        out GameDeployment.ParticipantView View
    )
    {
        View =
            null;


        return
            _GameDeployment != null &&
            _GameDeployment.TryGetView(
                Participant,
                out View
            );
    }


    internal bool TryGetInitialDeploymentFacing(
        RuntimeParticipant Participant,
        out UnitFacing Facing
    )
    {
        Facing =
            default;


        return
            _GameDeployment != null &&
            _GameDeployment.TryGetInitialFacing(
                Participant,
                out Facing
            );
    }


    internal bool TryGetDeploymentZoneCoordinates(
        RuntimeParticipant Participant,
        out Vector2Int[] Coordinates
    )
    {
        Coordinates =
            Array.Empty<Vector2Int>();


        return
            _GameDeployment != null &&
            _GameDeployment.TryGetDeploymentZoneCoordinates(
                Participant,
                out Coordinates
            );
    }


    internal bool TryGetDeploymentPlacementTiles(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out RuntimeUnitTilePlacement[] Placements,
        out string ValidationMessage
    )
    {
        Placements =
            Array.Empty<RuntimeUnitTilePlacement>();


        if (_GameDeployment == null)
        {
            ValidationMessage =
                "Deployment has not been initialized.";

            return false;
        }


        return
            _GameDeployment.TryGetPlacementTiles(
                Participant,
                FleetEntryID,
                out Placements,
                out ValidationMessage
            );
    }


    // Validates a complete gameplay proposal without mutation.
    //
    // Pointer, drag, HUD and GameObject data never cross this boundary.
    internal bool ValidateDeploymentPlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.ValidatePlacement(
                Participant,
                FleetEntryID,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage
            );
    }


    // Revalidates and applies one tentative placement.
    //
    // The same operation represents initial placement and movement of an
    // already placed tentative Unit.
    internal bool TrySubmitDeploymentPlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TrySubmitPlacement(
                Participant,
                FleetEntryID,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage
            );
    }


    internal bool TrySelectDeploymentPlacement(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TrySelectPlacement(
                Participant,
                FleetEntryID,
                out ValidationMessage
            );
    }


    internal bool TryClearDeploymentSelection(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TryClearSelection(
                Participant,
                out ValidationMessage
            );
    }


    // The Fleet Entry is explicit so human and AI controllers use the same
    // gameplay operation. Human HUD selection is not itself the rules API.
    internal bool TryRotateDeploymentPlacementClockwise(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TryRotatePlacementClockwise(
                Participant,
                FleetEntryID,
                out ValidationMessage
            );
    }


    internal bool TryRotateDeploymentPlacementCounterClockwise(
        RuntimeParticipant Participant,
        string FleetEntryID,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TryRotatePlacementCounterClockwise(
                Participant,
                FleetEntryID,
                out ValidationMessage
            );
    }


    internal bool TryResetDeployment(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameDeployment.TryResetPlan(
                Participant,
                out ValidationMessage
            );
    }


    internal bool TryConfirmDeployment(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!CanAcceptDeploymentInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        if (!_GameDeployment.TryPrepareConfirmation(
                Participant,
                out GameDeployment.ConfirmedPlacement[] ConfirmedPlacements,
                out ValidationMessage) ||
            !_GameUnits.TryPrepareInitialDeployment(
                Participant,
                ConfirmedPlacements,
                out PreparedRuntimeUnitDeployment PreparedUnits,
                out ValidationMessage))
        {
            return false;
        }


        RuntimeUnitPlacementRequest[] MapRequests =
            new RuntimeUnitPlacementRequest[ConfirmedPlacements.Length];


        for (int Index = 0;
            Index < ConfirmedPlacements.Length;
            Index++)
        {
            MapRequests[Index] =
                new RuntimeUnitPlacementRequest(
                    PreparedUnits.Entries[Index].RuntimeUnit,
                    ConfirmedPlacements[Index].Tiles,
                    ConfirmedPlacements[Index].DeploymentCondition
                );
        }


        if (!_GameMap.TryPrepareUnitPlacementBatch(
                MapRequests,
                out PreparedMapUnitDeployment PreparedMap,
                out ValidationMessage))
        {
            return false;
        }


        // Both prepared batches have passed every fallible validation step.
        // These commits contain no remaining rule checks or partial-failure
        // return paths.
        _GameUnits.CommitPreparedInitialDeployment(
            PreparedUnits
        );

        _GameMap.CommitPreparedUnitPlacementBatch(
            PreparedMap
        );

        Participant.SetDeploymentLocked(
            true
        );


        Debug.Log(
            $"'{Participant.Participant.DisplayName}' confirmed Deployment with {ConfirmedPlacements.Length} Runtime Units committed to GameMap.",
            this
        );


        if (_ParticipantA.DeploymentLocked &&
            _ParticipantB.DeploymentLocked)
        {
            BeginMatchAfterDeployment();
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Shared human/AI Ability input. The participant-bound API supplies the
    // caller identity; controllers submit only plain gameplay data.
    internal bool TryUseAbility(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int AbilitySlotIndex,
        out string ValidationMessage
    )
    {
        if (!_InitializationComplete ||
            !_MatchStarted)
        {
            ValidationMessage =
                "The Match is not accepting Ability input.";

            return false;
        }


        if (_CurrentPhase == GameplayPhase.Movement &&
            _GameMovement == null)
        {
            ValidationMessage =
                "Movement state is unavailable.";

            return false;
        }


        if (_CurrentPhase == GameplayPhase.PreBattle &&
            _GamePreBattle == null)
        {
            ValidationMessage =
                "Pre-Battle state is unavailable.";
            return false;
        }


        if (_CurrentPhase == GameplayPhase.Search &&
            _GameSearch == null)
        {
            ValidationMessage =
                "Search state is unavailable.";
            return false;
        }


        if (_CurrentPhase == GameplayPhase.End &&
            _GameEnd == null)
        {
            ValidationMessage =
                "End state is unavailable.";
            return false;
        }


        if (_CurrentPhase == GameplayPhase.PreBattle &&
            !_GamePreBattle.CanUseAbility(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex,
                out ValidationMessage))
        {
            return false;
        }


        if (_CurrentPhase == GameplayPhase.Search &&
            !_GameSearch.CanUseAbility(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex,
                out ValidationMessage))
        {
            return false;
        }


        if (_CurrentPhase == GameplayPhase.End &&
            !_GameEnd.CanUseAbility(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex,
                out ValidationMessage))
        {
            return false;
        }


        if (_CurrentPhase == GameplayPhase.Movement &&
            !_GameMovement.CanAcceptParticipantAction(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        if (!_GameUnits.TryGetRoster(
                Participant,
                out ParticipantUnitRoster Roster) ||
            !Roster.TryGetRuntimeUnit(
                RuntimeUnitID,
                out RuntimeUnit Unit))
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


        if (AbilitySlotIndex < 0 ||
            AbilitySlotIndex >=
                Unit.Abilities.Length)
        {
            ValidationMessage =
                "The requested Ability slot does not exist.";

            return false;
        }


        RuntimeAbilityState RuntimeAbility =
            Unit.Abilities[AbilitySlotIndex];

        Definition_Ability Ability =
            RuntimeAbility != null
                ? RuntimeAbility.Ability
                : null;


        if (Ability == null)
        {
            ValidationMessage =
                "The requested Ability slot is empty.";

            return false;
        }


        if (!Ability.Validate(
                out string AbilityMessage))
        {
            ValidationMessage =
                $"Ability '{Ability.name}' is invalid: {AbilityMessage}";

            return false;
        }


        if ((Ability.UsablePhases &
                _CurrentPhase) == 0)
        {
            ValidationMessage =
                $"Ability '{Ability.Name}' cannot be used during {_CurrentPhase}.";

            return false;
        }


        if ((Ability.ValidTargets &
                GameplayTarget.Self) == 0)
        {
            ValidationMessage =
                $"Ability '{Ability.Name}' is not a self-targeted Ability.";

            return false;
        }


        if (Ability.OutcomeType !=
                AbilityOutcomeType.Actions ||
            Ability.UsesD20Roll ||
            Ability.AbilityOutcome == null)
        {
            ValidationMessage =
                "The requested Ability is not a deterministic Action Ability.";

            return false;
        }


        if (!_GameUnitEffects.TryApplyActions(
                Unit,
                Ability.AbilityOutcome.Actions,
                out _,
                out ValidationMessage))
        {
            return false;
        }


        if (_CurrentPhase == GameplayPhase.PreBattle)
        {
            _GamePreBattle.CommitAbilityUse(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex
            );
        }


        if (_CurrentPhase == GameplayPhase.Search)
        {
            _GameSearch.CommitAbilityUse(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex
            );
        }


        if (_CurrentPhase == GameplayPhase.End)
        {
            _GameEnd.CommitAbilityUse(
                Participant,
                RuntimeUnitID,
                AbilitySlotIndex
            );
        }


        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed after Ability '{Ability.Name}': " +
                KnowledgeMessage,
                this
            );
        }

        EvaluateImmediateMatchConclusion(false);

        if (_MatchComplete && _CurrentPhase == GameplayPhase.End)
            _GameEnd?.TerminatePhase();


        Debug.Log(
            $"'{Participant.Participant.DisplayName}' used " +
            $"'{Ability.Name}' on '{Unit.InstanceName}'. " +
            $"Domain is now {Unit.CurrentDomain}.",
            this
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryGetMovementView(
        RuntimeParticipant Participant,
        out GameMovement.ParticipantView View
    )
    {
        View = null;

        return
            _GameMovement != null &&
            _GameMovement.TryGetView(
                Participant,
                out View
            );
    }


    internal bool ValidateMovement(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        Vector2Int DestinationMovementPivotCoordinate,
        UnitFacing DestinationFacing,
        out GameMovement.MovementPlan Plan,
        out string ValidationMessage
    )
    {
        Plan = null;


        if (!CanAcceptMovementInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameMovement.ValidateMovement(
                Participant,
                RuntimeUnitID,
                DestinationMovementPivotCoordinate,
                DestinationFacing,
                out Plan,
                out ValidationMessage
            );
    }


    internal bool TryGetMovementArea(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        out Vector2Int[] MovementArea,
        out string ValidationMessage
    )
    {
        MovementArea =
            Array.Empty<Vector2Int>();


        if (!CanAcceptMovementInput(
                Participant,
                out ValidationMessage))
        {
            return false;
        }


        return
            _GameMovement.TryGetMovementArea(
                Participant,
                RuntimeUnitID,
                out MovementArea,
                out ValidationMessage
            );
    }


    internal bool TryCommitMovement(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        Vector2Int DestinationMovementPivotCoordinate,
        UnitFacing DestinationFacing,
        out GameMovement.CommitResult Result,
        out string ValidationMessage
    )
    {
        Result = null;


        if (!CanAcceptMovementInput(
                Participant,
                out ValidationMessage) ||
            !_GameMovement.TryCommitMovement(
                Participant,
                RuntimeUnitID,
                DestinationMovementPivotCoordinate,
                DestinationFacing,
                out Result,
                out ValidationMessage))
        {
            return false;
        }


        Debug.Log(
            $"'{Participant.Participant.DisplayName}' committed Movement for " +
            $"Unit '{RuntimeUnitID}'. Outcome: {Result.Outcome}. " +
            $"Final pivot: {Result.FinalMovementPivotCoordinate}; " +
            $"Facing: {Result.FinalFacing}; Move consumed: {Result.MoveConsumed}.",
            this
        );

        EvaluateImmediateMatchConclusion(false);


        return true;
    }


    internal bool TryEndMovement(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!CanAcceptMovementInput(
                Participant,
                out ValidationMessage) ||
            !_GameMovement.TryEndMovement(
                Participant,
                out bool AllParticipantsEnded,
                out ValidationMessage))
        {
            return false;
        }


        Debug.Log(
            $"'{Participant.Participant.DisplayName}' ended Movement for Turn {_TurnNumber}.",
            this
        );


        if (AllParticipantsEnded)
        {
            CompleteMovementPhase();
        }


        ValidationMessage = string.Empty;

        return true;
    }


    internal bool TryGetPreBattleView(
        RuntimeParticipant Participant,
        out GamePreBattle.ParticipantView View
    )
    {
        View = null;

        return _GamePreBattle != null &&
            _GamePreBattle.TryGetView(Participant, out View);
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

        if (!CanAcceptPreBattleInput(Participant, out ValidationMessage))
            return false;

        return _GamePreBattle.TryGetMineDeploymentArea(
            Participant,
            RuntimeUnitID,
            SystemSlotIndex,
            out Coordinates,
            out ValidationMessage
        );
    }


    internal bool ValidateMineDeployment(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        Vector2Int Coordinate,
        out string ValidationMessage
    )
    {
        return CanAcceptPreBattleInput(Participant, out ValidationMessage) &&
            _GamePreBattle.ValidateMineDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                Coordinate,
                out ValidationMessage
            );
    }


    internal bool TryDeployMine(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        Vector2Int Coordinate,
        out string ValidationMessage
    )
    {
        if (!CanAcceptPreBattleInput(Participant, out ValidationMessage) ||
            !_GamePreBattle.TryCommitMineDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                Coordinate,
                out ValidationMessage))
        {
            return false;
        }


        _GameKnowledge.Refresh(_CurrentPhase, out _);

        Debug.Log(
            $"'{Participant.Participant.DisplayName}' deployed a Mine at {Coordinate}.",
            this
        );

        return true;
    }


    internal bool TryGetUnitDeploymentArea(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        UnitFacing Facing,
        out Vector2Int[] Coordinates,
        out string ValidationMessage
    )
    {
        Coordinates = Array.Empty<Vector2Int>();

        if (!CanAcceptPreBattleInput(Participant, out ValidationMessage))
            return false;

        return _GamePreBattle.TryGetUnitDeploymentArea(
            Participant,
            RuntimeUnitID,
            SystemSlotIndex,
            CarriedUnitIndex,
            Facing,
            out Coordinates,
            out ValidationMessage
        );
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
        return CanAcceptPreBattleInput(Participant, out ValidationMessage) &&
            _GamePreBattle.ValidateUnitDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                MovementPivotCoordinate,
                Facing,
                out ValidationMessage
            );
    }


    internal bool TryDeployCarriedUnit(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        int CarriedUnitIndex,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out string ValidationMessage
    )
    {
        if (!CanAcceptPreBattleInput(Participant, out ValidationMessage) ||
            !_GamePreBattle.TryCommitUnitDeployment(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                MovementPivotCoordinate,
                Facing,
                out RuntimeUnit DeployedUnit,
                out ValidationMessage))
        {
            return false;
        }


        _GameKnowledge.Refresh(_CurrentPhase, out _);

        EvaluateImmediateMatchConclusion(false);

        Debug.Log(
            $"'{Participant.Participant.DisplayName}' deployed '{DeployedUnit.InstanceName}' at {MovementPivotCoordinate}.",
            this
        );

        return true;
    }


    internal bool TryEndPreBattle(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!CanAcceptPreBattleInput(Participant, out ValidationMessage) ||
            !_GamePreBattle.TryEndPreBattle(
                Participant,
                out bool AllParticipantsEnded,
                out ValidationMessage))
        {
            return false;
        }


        Debug.Log(
            $"'{Participant.Participant.DisplayName}' ended Pre-Battle for Turn {_TurnNumber}.",
            this
        );


        if (AllParticipantsEnded)
            CompletePreBattlePhase();


        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetSearchView(
        RuntimeParticipant Participant,
        out GameSearch.ParticipantView View)
    {
        View = null;
        return _GameSearch != null &&
            _GameSearch.TryGetView(Participant, out View);
    }


    internal bool TryGetSearchPattern(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        UnitFacing SearchFacing,
        out GameSearch.SearchTile[] Tiles,
        out string ValidationMessage)
    {
        Tiles = Array.Empty<GameSearch.SearchTile>();
        if (!CanAcceptSearchInput(Participant, out ValidationMessage))
            return false;

        return _GameSearch.TryGetPattern(
            Participant,
            RuntimeUnitID,
            SystemSlotIndex,
            SearchFacing,
            out Tiles,
            out ValidationMessage);
    }


    internal bool TryCommitSearch(
        RuntimeParticipant Participant,
        string RuntimeUnitID,
        int SystemSlotIndex,
        UnitFacing SearchFacing,
        out GameSearch.CommitResult Result,
        out string ValidationMessage)
    {
        Result = null;
        if (!CanAcceptSearchInput(Participant, out ValidationMessage) ||
            !_GameSearch.TryCommitSearch(
                Participant,
                RuntimeUnitID,
                SystemSlotIndex,
                SearchFacing,
                out Result,
                out ValidationMessage))
            return false;

        if (!_GameKnowledge.Refresh(_CurrentPhase, out string KnowledgeMessage))
            Debug.LogError(
                $"GameKnowledge refresh failed after Search: {KnowledgeMessage}",
                this);

        Debug.Log(
            $"'{Participant.Participant.DisplayName}' committed a Search with " +
            $"Unit '{RuntimeUnitID}', System slot {SystemSlotIndex}.",
            this);
        return true;
    }


    internal bool TryEndSearch(
        RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!CanAcceptSearchInput(Participant, out ValidationMessage) ||
            !_GameSearch.TryEndSearch(
                Participant,
                out bool AllParticipantsEnded,
                out ValidationMessage))
            return false;

        Debug.Log(
            $"'{Participant.Participant.DisplayName}' ended Search for Turn {_TurnNumber}.",
            this);
        if (AllParticipantsEnded)
            CompleteSearchPhase();
        return true;
    }


    internal bool TryGetBattleView(RuntimeParticipant Participant,
        out GameBattle.ParticipantView View)
    {
        View = null;
        return _GameBattle != null &&
            _GameBattle.TryGetView(Participant, out View);
    }


    internal bool TryGetFireControlArea(RuntimeParticipant Participant,
        string RuntimeUnitID, Domain Domain,
        out Vector2Int[] Coordinates, out string ValidationMessage)
    {
        Coordinates = Array.Empty<Vector2Int>();
        if (!CanAcceptBattleInput(Participant, out ValidationMessage))
            return false;
        return _GameBattle.TryGetFireControlArea(Participant,
            RuntimeUnitID, Domain, out Coordinates, out ValidationMessage);
    }


    internal bool TryGetBattleTarget(RuntimeParticipant Participant,
        string RuntimeUnitID, Vector2Int Coordinate,
        out GameBattle.TargetView Target, out string ValidationMessage)
    {
        Target = null;
        if (!CanAcceptBattleInput(Participant, out ValidationMessage))
            return false;
        return _GameBattle.TryGetTarget(Participant,
            RuntimeUnitID, Coordinate, out Target, out ValidationMessage);
    }


    internal bool TryBeginEngagement(RuntimeParticipant Participant,
        string RuntimeUnitID, Vector2Int TargetCoordinate,
        out string ValidationMessage)
    {
        if (!CanAcceptBattleInput(Participant, out ValidationMessage) ||
            !_GameBattle.TryBeginEngagement(Participant,
                RuntimeUnitID, TargetCoordinate, out ValidationMessage))
            return false;
        return true;
    }


    internal bool TryCancelEngagement(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        ValidationMessage = "The Match is not accepting engagement cancellation.";
        if (_MatchComplete || _CurrentPhase != GameplayPhase.Battle ||
            _GameBattle == null ||
            !_GameBattle.TryCancelEngagement(Participant, out ValidationMessage))
            return false;
        return true;
    }


    internal bool TryLockBattleAttack(RuntimeParticipant Participant,
        GameBattle.CombatActionKind Kind, int SlotIndex,
        out string ValidationMessage)
    {
        ValidationMessage = "Battle is not accepting an attack lock-in.";
        return !_MatchComplete &&
            _CurrentPhase == GameplayPhase.Battle && _GameBattle != null &&
            _GameBattle.TryLockAttack(Participant, Kind, SlotIndex,
                out ValidationMessage);
    }


    internal bool TryLockBattleDefense(RuntimeParticipant Participant,
        GameBattle.CombatActionKind DefenseKind, int DefenseSlotIndex,
        string SupportRuntimeUnitID, GameBattle.CombatActionKind SupportKind,
        int SupportSlotIndex, out GameBattle.ResolutionView Resolution,
        out string ValidationMessage)
    {
        Resolution = null;
        ValidationMessage = "Battle is not accepting a defense lock-in.";
        if (_MatchComplete || _CurrentPhase != GameplayPhase.Battle ||
            _GameBattle == null ||
            !_GameBattle.TryLockDefense(Participant, DefenseKind,
                DefenseSlotIndex, SupportRuntimeUnitID, SupportKind,
                SupportSlotIndex, out Resolution, out ValidationMessage))
            return false;
        EvaluateImmediateMatchConclusion(true);
        if (!_GameKnowledge.Refresh(_CurrentPhase, out string KnowledgeMessage))
            Debug.LogError($"Knowledge refresh failed after combat: {KnowledgeMessage}", this);
        return true;
    }


    internal bool TryContinueEngagement(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        ValidationMessage = "The engagement is not ready to continue.";
        if (_CurrentPhase != GameplayPhase.Battle || _GameBattle == null ||
            !_GameBattle.TryContinueEngagement(Participant, out ValidationMessage))
            return false;

        if (_MatchComplete)
        {
            _CurrentPhase = GameplayPhase.End;
            if (!_GameKnowledge.Refresh(_CurrentPhase,
                    out string KnowledgeMessage))
            {
                Debug.LogError(
                    $"GameKnowledge refresh failed after the final engagement: " +
                    KnowledgeMessage,
                    this);
            }
        }

        return true;
    }


    internal bool TryEndBattle(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!CanAcceptBattleInput(Participant, out ValidationMessage) ||
            !_GameBattle.TryEndBattle(Participant,
                out bool AllParticipantsEnded, out ValidationMessage))
            return false;
        if (AllParticipantsEnded)
            CompleteBattlePhase();
        return true;
    }


    internal bool TryGetEndView(RuntimeParticipant Participant,
        out GameEnd.ParticipantView View)
    {
        View = null;
        return _GameEnd != null &&
            _GameEnd.TryGetView(Participant, out View);
    }


    internal bool TryFinishEndPhase(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!CanAcceptEndInput(Participant, out ValidationMessage) ||
            !_GameEnd.TryFinishParticipant(Participant,
                out bool AllParticipantsFinished,
                out ValidationMessage))
            return false;

        Debug.Log(
            $"'{Participant.Participant.DisplayName}' finished End for Turn {_TurnNumber}.",
            this);

        if (AllParticipantsFinished)
            CompleteEndPhase();

        ValidationMessage = string.Empty;
        return true;
    }


    private void Update()
    {
        if (!_InitializationComplete ||
            _PassAndPlayHandoffPaused)
        {
            return;
        }


        RuntimeParticipant TimedOutParticipant;
        bool AllParticipantsEnded;


        if (_CurrentPhase == GameplayPhase.Movement &&
            _GameMovement != null &&
            _GameMovement.Tick(
                Time.deltaTime,
                out TimedOutParticipant,
                out AllParticipantsEnded))
        {
            Debug.Log(
                $"'{TimedOutParticipant.Participant.DisplayName}' ran out of Movement time on Turn {_TurnNumber}.",
                this
            );


            if (AllParticipantsEnded)
                CompleteMovementPhase();


            _ParticipantAPI?.NotifySnapshotsChanged();
            return;
        }


        if (_CurrentPhase == GameplayPhase.Search &&
            _GameSearch != null &&
            _GameSearch.Tick(
                Time.deltaTime,
                out TimedOutParticipant,
                out AllParticipantsEnded))
        {
            Debug.Log(
                $"'{TimedOutParticipant.Participant.DisplayName}' ran out of Search time on Turn {_TurnNumber}.",
                this);

            if (AllParticipantsEnded)
                CompleteSearchPhase();

            _ParticipantAPI?.NotifySnapshotsChanged();
            return;
        }


        if (_CurrentPhase == GameplayPhase.Battle &&
            _GameBattle != null &&
            _GameBattle.Tick(
                Time.deltaTime,
                out TimedOutParticipant,
                out AllParticipantsEnded))
        {
            Debug.Log(
                $"'{TimedOutParticipant.Participant.DisplayName}' ran out of Battle time on Turn {_TurnNumber}.",
                this);

            if (AllParticipantsEnded)
                CompleteBattlePhase();

            _ParticipantAPI?.NotifySnapshotsChanged();
            return;
        }


        if (_CurrentPhase == GameplayPhase.End &&
            _GameEnd != null &&
            _GameEnd.Tick(
                Time.deltaTime,
                out TimedOutParticipant,
                out AllParticipantsEnded))
        {
            Debug.Log(
                $"'{TimedOutParticipant.Participant.DisplayName}' ran out of End time on Turn {_TurnNumber}.",
                this);

            if (AllParticipantsEnded)
                CompleteEndPhase();

            _ParticipantAPI?.NotifySnapshotsChanged();
            return;
        }


        if (_CurrentPhase != GameplayPhase.PreBattle ||
            _GamePreBattle == null ||
            !_GamePreBattle.Tick(
                Time.deltaTime,
                out TimedOutParticipant,
                out AllParticipantsEnded))
        {
            return;
        }


        Debug.Log(
            $"'{TimedOutParticipant.Participant.DisplayName}' ran out of Pre-Battle time on Turn {_TurnNumber}.",
            this
        );


        if (AllParticipantsEnded)
        {
            CompletePreBattlePhase();
        }


        _ParticipantAPI?.NotifySnapshotsChanged();
    }


    private void CompleteMovementPhase()
    {
        _CurrentPhase = GameplayPhase.PreBattle;


        float PreBattleSeconds = 60f;


        if (_GameMode.TryGetPhaseTimeLimit(
                GameplayPhase.PreBattle,
                out float AuthoredPreBattleSeconds))
        {
            PreBattleSeconds = AuthoredPreBattleSeconds;
        }


        if (!_GamePreBattle.BeginPhase(
                _FirstPlayer,
                PreBattleSeconds,
                out string PreBattleMessage))
        {
            Debug.LogError(
                $"Pre-Battle phase initialization failed: {PreBattleMessage}",
                this
            );
        }


        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed after Movement: {KnowledgeMessage}",
                this
            );
        }


        Debug.Log(
            $"Both participants ended Movement. Turn {_TurnNumber} entered Pre-Battle.",
            this
        );
    }


    private void CompletePreBattlePhase()
    {
        _CurrentPhase = GameplayPhase.Search;


        float SearchSeconds = 90f;

        if (_GameMode.TryGetPhaseTimeLimit(
                GameplayPhase.Search,
                out float AuthoredSearchSeconds))
        {
            SearchSeconds = AuthoredSearchSeconds;
        }

        if (!_GameSearch.BeginPhase(
                _FirstPlayer,
                SearchSeconds,
                out string SearchMessage))
        {
            Debug.LogError(
                $"Search phase initialization failed: {SearchMessage}",
                this);
        }


        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed after Pre-Battle: {KnowledgeMessage}",
                this
            );
        }


        Debug.Log(
            $"Both participants ended Pre-Battle. Turn {_TurnNumber} entered Search.",
            this
        );
    }


    private void CompleteSearchPhase()
    {
        _CurrentPhase = GameplayPhase.Battle;

        float BattleSeconds = 90f;
        if (_GameMode.TryGetPhaseTimeLimit(
                GameplayPhase.Battle,
                out float AuthoredBattleSeconds))
            BattleSeconds = AuthoredBattleSeconds;

        if (!_GameBattle.BeginPhase(
                _FirstPlayer,
                BattleSeconds,
                out string BattleMessage))
        {
            Debug.LogError(
                $"Battle phase initialization failed: {BattleMessage}",
                this);
        }

        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed after Search: {KnowledgeMessage}",
                this);
        }

        Debug.Log(
            $"Both participants ended Search. Turn {_TurnNumber} entered Battle.",
            this);
    }


    private void CompleteBattlePhase()
    {
        _CurrentPhase = GameplayPhase.End;

        if (!_GameUnitEffects.TryResolveEndPhase(
                out GameUnitEffects.EndPhaseReport Report,
                out string UpkeepMessage))
        {
            Debug.LogError(
                $"End-phase upkeep failed: {UpkeepMessage}",
                this);
        }

        RefreshScores();
        EvaluateImmediateMatchConclusion(false);

        if (!_MatchComplete)
        {
            float EndSeconds = 60f;
            if (_GameMode.TryGetPhaseTimeLimit(
                    GameplayPhase.End,
                    out float AuthoredEndSeconds))
                EndSeconds = AuthoredEndSeconds;

            if (!_GameEnd.BeginPhase(
                    _FirstPlayer,
                    EndSeconds,
                    out string EndMessage))
            {
                Debug.LogError(
                    $"End phase initialization failed: {EndMessage}",
                    this);
            }
        }

        if (!_GameKnowledge.Refresh(_CurrentPhase, out string KnowledgeMessage))
            Debug.LogError(
                $"GameKnowledge refresh failed after Battle: {KnowledgeMessage}",
                this);

        if (_MatchComplete)
        {
            Debug.Log(
                $"Match concluded during End upkeep on Turn {_TurnNumber}: " +
                _MatchResolutionReason,
                this);
        }
        else
        {
            Debug.Log(
                $"Both participants ended Battle. Turn {_TurnNumber} entered End. " +
                $"Upkeep: {Report?.UnitsProcessed ?? 0} Units, " +
                $"{Report?.UnitsDestroyed ?? 0} destroyed, " +
                $"{Report?.WeaponChargesRestored ?? 0} Weapon charges and " +
                $"{Report?.SystemChargesRestored ?? 0} System charges restored.",
                this);
        }
    }


    private void CompleteEndPhase()
    {
        _CompletedTurnNumber = _TurnNumber;
        RefreshScores();

        bool EvaluationSucceeded = TryEvaluateMatchConclusion(
            ConditionEvaluationMoment.EndPhase,
            out bool MatchConcluded,
            out string EvaluationMessage);

        if (!EvaluationSucceeded)
        {
            Debug.LogError(
                $"GameMode condition evaluation failed: {EvaluationMessage}",
                this);
            return;
        }

        if (MatchConcluded)
        {
            if (!_GameKnowledge.Refresh(_CurrentPhase,
                    out string KnowledgeMessage))
            {
                Debug.LogError(
                    $"GameKnowledge refresh failed after End: {KnowledgeMessage}",
                    this);
            }

            Debug.Log(
                $"Match concluded at the end of Turn {_TurnNumber}: " +
                _MatchResolutionReason,
                this);
            return;
        }

        BeginNextTurn();
    }


    private void BeginNextTurn()
    {
        RuntimeParticipant NextFirstPlayer = ReferenceEquals(
                _FirstPlayer, _ParticipantA)
            ? _ParticipantB
            : _ParticipantA;
        int NextTurnNumber = _TurnNumber + 1;

        float MovementSeconds = 60f;
        if (_GameMode.TryGetPhaseTimeLimit(
                GameplayPhase.Movement,
                out float AuthoredMovementSeconds))
        {
            MovementSeconds = AuthoredMovementSeconds;
        }

        if (!_GameMovement.BeginPhase(
                NextFirstPlayer,
                MovementSeconds,
                out string MovementMessage))
        {
            Debug.LogError(
                $"Turn {NextTurnNumber} Movement initialization failed: " +
                MovementMessage,
                this);
            return;
        }

        _FirstPlayer = NextFirstPlayer;
        _TurnNumber = NextTurnNumber;
        _CurrentPhase = GameplayPhase.Movement;

        // Search contacts and searched-tile visibility last through the full
        // turn, including End, then expire before the next Movement begins.
        _GameKnowledge.ClearTurnSearchKnowledge();

        if (!_GameKnowledge.Refresh(_CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed for Turn {_TurnNumber}: " +
                KnowledgeMessage,
                this);
        }

        Debug.Log(
            $"Turn {_CompletedTurnNumber} completed. Turn {_TurnNumber} " +
            $"entered Movement with " +
            $"'{_FirstPlayer.Participant.DisplayName}' holding first-player priority.",
            this);
    }


    private void RefreshScores()
    {
        _ParticipantAScore = 0;
        _ParticipantBScore = 0;

        EnemyUnitDestroyedScoringRule Rule =
            _GameMode?.EnemyUnitDestroyedScoring;
        if (Rule == null || !Rule.Enabled || _GameUnits == null)
            return;

        int ParticipantALosses = Mathf.Max(0,
            _ParticipantAInitialFleetSize -
            _GameUnits.ParticipantA.RemainingFleetSize);
        int ParticipantBLosses = Mathf.Max(0,
            _ParticipantBInitialFleetSize -
            _GameUnits.ParticipantB.RemainingFleetSize);

        _ParticipantAScore = ParticipantBLosses * Rule.PointsPerUnit;
        _ParticipantBScore = ParticipantALosses * Rule.PointsPerUnit;
    }


    private ParticipantMatchStateSnapshot CreateParticipantMatchState(
        RuntimeParticipant Participant)
    {
        bool IsParticipantA = ReferenceEquals(Participant, _ParticipantA);
        int OwnScore = IsParticipantA
            ? _ParticipantAScore
            : _ParticipantBScore;
        int OpponentScore = IsParticipantA
            ? _ParticipantBScore
            : _ParticipantAScore;

        ParticipantMatchOutcome Outcome = ParticipantMatchOutcome.Ongoing;
        if (_MatchComplete)
        {
            if (_MatchDraw)
                Outcome = ParticipantMatchOutcome.Draw;
            else if (ReferenceEquals(Participant, _WinningParticipant))
                Outcome = ParticipantMatchOutcome.Victory;
            else
                Outcome = ParticipantMatchOutcome.Defeat;
        }

        return new ParticipantMatchStateSnapshot(
            _MatchComplete,
            Outcome,
            OwnScore,
            OpponentScore,
            _CompletedTurnNumber,
            _MatchResolutionReason);
    }


    private bool TryEvaluateMatchConclusion(
        ConditionEvaluationMoment EvaluationMoment,
        out bool MatchConcluded,
        out string ValidationMessage)
    {
        MatchConcluded = _MatchComplete;
        if (_MatchComplete)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        if (_GameMode == null || _GameUnits == null ||
            _ParticipantA == null || _ParticipantB == null)
        {
            ValidationMessage =
                "GameMode, Unit state, and both participants are required.";
            return false;
        }

        if (!TryEvaluateAnyConditionGroup(_ParticipantA,
                EvaluationMoment,
                _GameMode.VictoryGroups, out bool AVictory,
                out string AVictoryGroup, out ValidationMessage) ||
            !TryEvaluateAnyConditionGroup(_ParticipantA,
                EvaluationMoment,
                _GameMode.DefeatGroups, out bool ADefeat,
                out string ADefeatGroup, out ValidationMessage) ||
            !TryEvaluateAnyConditionGroup(_ParticipantB,
                EvaluationMoment,
                _GameMode.VictoryGroups, out bool BVictory,
                out string BVictoryGroup, out ValidationMessage) ||
            !TryEvaluateAnyConditionGroup(_ParticipantB,
                EvaluationMoment,
                _GameMode.DefeatGroups, out bool BDefeat,
                out string BDefeatGroup, out ValidationMessage))
        {
            return false;
        }

        bool ParticipantAWins = AVictory || BDefeat;
        bool ParticipantBWins = BVictory || ADefeat;
        if (!ParticipantAWins && !ParticipantBWins)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        _MatchComplete = true;
        MatchConcluded = true;

        string AReason = AVictory
            ? $"Victory group '{AVictoryGroup}'"
            : $"opponent Defeat group '{BDefeatGroup}'";
        string BReason = BVictory
            ? $"Victory group '{BVictoryGroup}'"
            : $"opponent Defeat group '{ADefeatGroup}'";

        if (ParticipantAWins && ParticipantBWins)
        {
            _MatchDraw = true;
            _WinningParticipant = null;
            _MatchResolutionReason =
                $"Draw: both participants produced a winning result " +
                $"({AReason}; {BReason}).";
        }
        else
        {
            _MatchDraw = false;
            _WinningParticipant = ParticipantAWins
                ? _ParticipantA
                : _ParticipantB;
            string Reason = ParticipantAWins ? AReason : BReason;
            _MatchResolutionReason =
                $"'{_WinningParticipant.Participant.DisplayName}' won via {Reason}.";
        }

        ValidationMessage = string.Empty;
        return true;
    }


    private bool TryEvaluateAnyConditionGroup(
        RuntimeParticipant Perspective,
        ConditionEvaluationMoment EvaluationMoment,
        GameModeConditionGroup[] Groups,
        out bool Satisfied,
        out string SatisfiedGroupName,
        out string ValidationMessage)
    {
        Satisfied = false;
        SatisfiedGroupName = string.Empty;
        ValidationMessage = string.Empty;

        if (Groups == null || Groups.Length == 0)
            return true;

        for (int i = 0; i < Groups.Length; i++)
        {
            GameModeConditionGroup Group = Groups[i];
            if (Group == null)
            {
                ValidationMessage = $"Condition Group {i} is missing.";
                return false;
            }

            bool IsEndPhaseGroup =
                HasEntries(Group.TurnConditions) ||
                HasEntries(Group.ScoreAtTurnLimitConditions);
            if (IsEndPhaseGroup !=
                (EvaluationMoment == ConditionEvaluationMoment.EndPhase))
            {
                continue;
            }

            if (!TryEvaluateConditionGroup(Perspective, Group,
                    out bool GroupSatisfied, out ValidationMessage))
                return false;

            if (!GroupSatisfied)
                continue;

            Satisfied = true;
            SatisfiedGroupName = string.IsNullOrWhiteSpace(Group.Name)
                ? $"Group {i}"
                : Group.Name;
            return true;
        }

        return true;
    }


    private bool TryEvaluateConditionGroup(
        RuntimeParticipant Perspective,
        GameModeConditionGroup Group,
        out bool Satisfied,
        out string ValidationMessage)
    {
        Satisfied = false;

        FleetSizeCondition[] FleetConditions =
            Group.FleetSizeConditions ?? Array.Empty<FleetSizeCondition>();
        for (int i = 0; i < FleetConditions.Length; i++)
        {
            FleetSizeCondition Condition = FleetConditions[i];
            if (Condition == null ||
                !TryResolveConditionSubject(Perspective, Condition.Subject,
                    out RuntimeParticipant Subject) ||
                !_GameUnits.TryGetRoster(Subject,
                    out ParticipantUnitRoster Roster))
            {
                ValidationMessage =
                    $"Fleet Size Condition {i} could not resolve its Subject.";
                return false;
            }

            if (!CompareNumeric(Roster.RemainingFleetSize,
                    Condition.Comparison, Condition.Value))
            {
                ValidationMessage = string.Empty;
                return true;
            }
        }

        DesignatedUnitCondition[] DesignatedConditions =
            Group.DesignatedUnitConditions ??
            Array.Empty<DesignatedUnitCondition>();
        for (int i = 0; i < DesignatedConditions.Length; i++)
        {
            if (!TryEvaluateDesignatedUnitCondition(Perspective,
                    DesignatedConditions[i], out bool ConditionSatisfied,
                    out ValidationMessage))
                return false;
            if (!ConditionSatisfied)
                return true;
        }

        ScoreCondition[] ScoreConditions =
            Group.ScoreConditions ?? Array.Empty<ScoreCondition>();
        for (int i = 0; i < ScoreConditions.Length; i++)
        {
            ScoreCondition Condition = ScoreConditions[i];
            if (Condition == null ||
                !TryResolveConditionSubject(Perspective, Condition.Subject,
                    out RuntimeParticipant Subject))
            {
                ValidationMessage =
                    $"Score Condition {i} could not resolve its Subject.";
                return false;
            }

            if (!CompareNumeric(GetParticipantScore(Subject),
                    Condition.Comparison, Condition.Value))
            {
                ValidationMessage = string.Empty;
                return true;
            }
        }

        TurnCondition[] TurnConditions =
            Group.TurnConditions ?? Array.Empty<TurnCondition>();
        for (int i = 0; i < TurnConditions.Length; i++)
        {
            TurnCondition Condition = TurnConditions[i];
            if (Condition == null)
            {
                ValidationMessage = $"Turn Condition {i} is missing.";
                return false;
            }

            if (!CompareNumeric(_TurnNumber,
                    Condition.Comparison, Condition.Turn))
            {
                ValidationMessage = string.Empty;
                return true;
            }
        }

        ScoreAtTurnLimitCondition[] LimitConditions =
            Group.ScoreAtTurnLimitConditions ??
            Array.Empty<ScoreAtTurnLimitCondition>();
        for (int i = 0; i < LimitConditions.Length; i++)
        {
            ScoreAtTurnLimitCondition Condition = LimitConditions[i];
            if (Condition == null)
            {
                ValidationMessage =
                    $"Score At Turn Limit Condition {i} is missing.";
                return false;
            }

            int TurnsPastInitial = _TurnNumber - Condition.InitialTurnLimit;
            bool IsCheckpoint = TurnsPastInitial >= 0 &&
                TurnsPastInitial % Condition.TieExtensionTurns == 0;
            if (!IsCheckpoint)
            {
                ValidationMessage = string.Empty;
                return true;
            }

            int OwnScore = GetParticipantScore(Perspective);
            RuntimeParticipant Opponent = ReferenceEquals(
                Perspective, _ParticipantA) ? _ParticipantB : _ParticipantA;
            int OpponentScore = GetParticipantScore(Opponent);
            bool RelationshipSatisfied = Condition.Relationship ==
                    ScoreRelationship.HigherThanOpponent
                ? OwnScore > OpponentScore
                : OwnScore < OpponentScore;
            if (!RelationshipSatisfied)
            {
                ValidationMessage = string.Empty;
                return true;
            }
        }

        Satisfied = true;
        ValidationMessage = string.Empty;
        return true;
    }


    private bool TryEvaluateDesignatedUnitCondition(
        RuntimeParticipant Perspective,
        DesignatedUnitCondition Condition,
        out bool Satisfied,
        out string ValidationMessage)
    {
        Satisfied = false;
        if (Condition == null ||
            !TryResolveConditionSubject(Perspective, Condition.Subject,
                out RuntimeParticipant Subject) ||
            !_GameUnits.TryGetRoster(Subject,
                out ParticipantUnitRoster Roster))
        {
            ValidationMessage =
                "Designated Unit Condition could not resolve its Subject.";
            return false;
        }

        GameModeSideFleet Side = ReferenceEquals(Subject, _ParticipantA)
            ? _GameMode.SideA
            : _GameMode.SideB;
        string EntryID = Side?.AdmiralEntryID;
        if (string.IsNullOrWhiteSpace(EntryID))
        {
            ValidationMessage =
                "A Designated Unit Condition requires an authored designated Fleet Entry.";
            return false;
        }

        RuntimeUnit DesignatedUnit = null;
        for (int i = 0; i < Roster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Candidate = Roster.RuntimeUnits[i];
            if (Candidate != null && string.Equals(
                    Candidate.SourceFleetEntryID, EntryID,
                    StringComparison.Ordinal))
            {
                DesignatedUnit = Candidate;
                break;
            }
        }

        if (DesignatedUnit == null)
        {
            ValidationMessage =
                $"Designated Fleet Entry '{EntryID}' has no Runtime Unit.";
            return false;
        }

        Satisfied = Condition.RequiredState == DesignatedUnitState.Alive
            ? DesignatedUnit.IsActive
            : DesignatedUnit.LifeState == RuntimeUnitLifeState.Destroyed;
        ValidationMessage = string.Empty;
        return true;
    }


    private bool TryResolveConditionSubject(
        RuntimeParticipant Perspective,
        ConditionSubject Subject,
        out RuntimeParticipant Resolved)
    {
        Resolved = null;
        if (Perspective == null || Subject == ConditionSubject.None)
            return false;

        if (Subject == ConditionSubject.Self)
            Resolved = Perspective;
        else if (Subject == ConditionSubject.Opponent)
            Resolved = ReferenceEquals(Perspective, _ParticipantA)
                ? _ParticipantB
                : _ParticipantA;

        return Resolved != null;
    }


    private int GetParticipantScore(RuntimeParticipant Participant)
    {
        return ReferenceEquals(Participant, _ParticipantA)
            ? _ParticipantAScore
            : _ParticipantBScore;
    }


    private static bool CompareNumeric(int Actual,
        NumericComparison Comparison, int Expected)
    {
        if (Comparison == NumericComparison.AtOrBelow)
            return Actual <= Expected;
        if (Comparison == NumericComparison.AtOrAbove)
            return Actual >= Expected;
        return false;
    }


    private void EvaluateImmediateMatchConclusion(
        bool KeepResolvedEngagementVisible)
    {
        if (_MatchComplete)
            return;

        GameplayPhase TriggerPhase = _CurrentPhase;
        RefreshScores();
        if (!TryEvaluateMatchConclusion(
                ConditionEvaluationMoment.Immediate,
                out bool MatchConcluded,
                out string EvaluationMessage))
        {
            Debug.LogError(
                $"Immediate GameMode condition evaluation failed: " +
                EvaluationMessage,
                this);
            return;
        }

        if (!MatchConcluded)
            return;

        if (!KeepResolvedEngagementVisible)
            _CurrentPhase = GameplayPhase.End;

        if (!_GameKnowledge.Refresh(_CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed after immediate match conclusion: " +
                KnowledgeMessage,
                this);
        }

        Debug.Log(
            $"Match concluded immediately during {TriggerPhase} on Turn " +
            $"{_TurnNumber}: {_MatchResolutionReason}",
            this);
    }


    private static bool HasEntries<T>(T[] Entries)
    {
        return Entries != null && Entries.Length > 0;
    }


    private bool CanAcceptDeploymentInput(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!_InitializationComplete ||
            _CurrentPhase != GameplayPhase.Deployment)
        {
            ValidationMessage =
                "The Match is not accepting Deployment input.";

            return false;
        }


        if (_GameDeployment == null ||
            !_GameDeployment.TryGetPlan(
                Participant,
                out _))
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


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool CanAcceptMovementInput(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!_InitializationComplete ||
            !_MatchStarted ||
            _CurrentPhase != GameplayPhase.Movement ||
            _GameMovement == null)
        {
            ValidationMessage =
                "The Match is not accepting Movement input.";

            return false;
        }


        if (Participant == null ||
            (!ReferenceEquals(Participant, _ParticipantA) &&
             !ReferenceEquals(Participant, _ParticipantB)))
        {
            ValidationMessage =
                "Participant does not belong to this Match.";

            return false;
        }


        ValidationMessage = string.Empty;

        return true;
    }


    private bool CanAcceptPreBattleInput(
        RuntimeParticipant Participant,
        out string ValidationMessage
    )
    {
        if (!_InitializationComplete ||
            !_MatchStarted ||
            _CurrentPhase != GameplayPhase.PreBattle ||
            _GamePreBattle == null)
        {
            ValidationMessage =
                "The Match is not accepting Pre-Battle input.";

            return false;
        }


        return _GamePreBattle.CanAcceptParticipantAction(
            Participant,
            out ValidationMessage
        );
    }


    private bool CanAcceptSearchInput(
        RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!_InitializationComplete ||
            !_MatchStarted ||
            _CurrentPhase != GameplayPhase.Search ||
            _GameSearch == null)
        {
            ValidationMessage =
                "The Match is not accepting Search input.";
            return false;
        }

        return _GameSearch.CanAcceptParticipantAction(
            Participant,
            out ValidationMessage);
    }


    private bool CanAcceptBattleInput(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!_InitializationComplete || !_MatchStarted || _MatchComplete ||
            _CurrentPhase != GameplayPhase.Battle || _GameBattle == null)
        {
            ValidationMessage = "The Match is not accepting Battle input.";
            return false;
        }
        return _GameBattle.CanAcceptParticipantAction(
            Participant, out ValidationMessage);
    }


    private bool CanAcceptEndInput(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!_InitializationComplete || !_MatchStarted || _MatchComplete ||
            _CurrentPhase != GameplayPhase.End || _GameEnd == null)
        {
            ValidationMessage = "The Match is not accepting End input.";
            return false;
        }

        return _GameEnd.CanAcceptParticipantAction(
            Participant, out ValidationMessage);
    }


    private bool InitializeParticipants()
    {
        if (_MatchConfig.PlayerSlot1 == null)
        {
            FailInitialization(
                "Participant A snapshot is missing."
            );

            return false;
        }


        if (_MatchConfig.PlayerSlot2 == null)
        {
            FailInitialization(
                "Participant B snapshot is missing."
            );

            return false;
        }


        if (_MatchConfig.Player1Fleet == null)
        {
            FailInitialization(
                "Participant A has no resolved Fleet."
            );

            return false;
        }


        if (_MatchConfig.Player2Fleet == null)
        {
            FailInitialization(
                "Participant B has no resolved Fleet."
            );

            return false;
        }


        _ParticipantA =
            new RuntimeParticipant(
                _MatchConfig.PlayerSlot1,
                _MatchConfig.Player1Fleet
            );

        _ParticipantB =
            new RuntimeParticipant(
                _MatchConfig.PlayerSlot2,
                _MatchConfig.Player2Fleet
            );


        return true;
    }


    private bool InitializeMap()
    {
        GameObject MapPrefab =
            _MatchConfig.MapPrefab;


        if (MapPrefab == null)
        {
            FailInitialization(
                "MatchConfig has no Map prefab."
            );

            return false;
        }


        _MapInstance =
            Instantiate(
                MapPrefab,
                Vector3.zero,
                Quaternion.identity
            );


        if (_MapInstance == null)
        {
            FailInitialization(
                $"Failed to instantiate Map prefab '{MapPrefab.name}'."
            );

            return false;
        }


        _MapInstance.name =
            $"{MapPrefab.name}_Runtime";


        _MapAuthoring =
            _MapInstance.GetComponent<MapAuthoring>();


        if (_MapAuthoring == null)
        {
            Destroy(
                _MapInstance
            );

            _MapInstance =
                null;


            FailInitialization(
                $"Runtime Map '{MapPrefab.name}' has no MapAuthoring component."
            );

            return false;
        }


        _GameMap =
            _MapInstance.GetComponent<GameMap>();


        if (_GameMap == null)
        {
            _GameMap =
                _MapInstance.AddComponent<GameMap>();
        }


        if (!_GameMap.Initialize(
                _MapAuthoring,
                out string ValidationMessage))
        {
            Destroy(
                _MapInstance
            );

            _MapInstance =
                null;

            _MapAuthoring =
                null;

            _GameMap =
                null;


            FailInitialization(
                $"Runtime Map initialization failed: {ValidationMessage}"
            );

            return false;
        }


        return true;
    }


    private bool InitializeUnitState()
    {
        _GameUnits =
            new GameUnits();


        if (!_GameUnits.Initialize(
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameUnits =
                null;


            FailInitialization(
                $"Unit State initialization failed: {ValidationMessage}"
            );

            return false;
        }

        _ParticipantAInitialFleetSize =
            _GameUnits.ParticipantA.RemainingFleetSize;
        _ParticipantBInitialFleetSize =
            _GameUnits.ParticipantB.RemainingFleetSize;
        _ParticipantAScore = 0;
        _ParticipantBScore = 0;


        return true;
    }


    private bool InitializeUnitEffects()
    {
        _GameUnitEffects =
            new GameUnitEffects();


        if (!_GameUnitEffects.Initialize(
                _GameMap,
                _GameUnits,
                out string ValidationMessage))
        {
            _GameUnitEffects =
                null;


            FailInitialization(
                $"Unit Effects initialization failed: {ValidationMessage}"
            );

            return false;
        }


        return true;
    }


    private bool InitializeDeploymentState()
    {
        _GameDeployment =
            new GameDeployment();


        if (!_GameDeployment.Initialize(
                _GameMap,
                _GameUnits.ParticipantA,
                _GameUnits.ParticipantB,
                out string ValidationMessage))
        {
            _GameDeployment =
                null;


            FailInitialization(
                $"Deployment State initialization failed: {ValidationMessage}"
            );

            return false;
        }


        return true;
    }


    private bool InitializeKnowledgeState()
    {
        _GameKnowledge = new GameKnowledge();

        if (!_GameKnowledge.Initialize(
                _GameMap,
                _GameUnits,
                _GameDeployment,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameKnowledge = null;
            FailInitialization(
                $"Knowledge State initialization failed: {ValidationMessage}");
            return false;
        }

        return true;
    }


    private bool InitializeMovementState()
    {
        _GameMovement = new GameMovement();


        if (!_GameMovement.Initialize(
                _GameMap,
                _GameUnits,
                _GameKnowledge,
                _GameUnitEffects,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameMovement = null;

            FailInitialization(
                $"Movement State initialization failed: {ValidationMessage}"
            );

            return false;
        }


        return true;
    }


    private bool InitializePreBattleState()
    {
        _GamePreBattle = new GamePreBattle();


        if (!_GamePreBattle.Initialize(
                _GameMap,
                _GameUnits,
                _GameUnitEffects,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GamePreBattle = null;
            FailInitialization(
                $"Pre-Battle State initialization failed: {ValidationMessage}"
            );
            return false;
        }


        return true;
    }


    private bool InitializeSearchState()
    {
        _GameSearch = new GameSearch();

        if (!_GameSearch.Initialize(
                _GameMap,
                _GameUnits,
                _GameKnowledge,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameSearch = null;
            FailInitialization(
                $"Search State initialization failed: {ValidationMessage}");
            return false;
        }

        return true;
    }


    private bool InitializeBattleState()
    {
        _GameBattle = new GameBattle();
        if (!_GameBattle.Initialize(
                _GameMap,
                _GameUnits,
                _GameKnowledge,
                _GameUnitEffects,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameBattle = null;
            FailInitialization(
                $"Battle State initialization failed: {ValidationMessage}");
            return false;
        }
        return true;
    }


    private bool InitializeEndState()
    {
        _GameEnd = new GameEnd();
        if (!_GameEnd.Initialize(
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _GameEnd = null;
            FailInitialization(
                $"End State initialization failed: {ValidationMessage}");
            return false;
        }
        return true;
    }


    internal bool TryGetParticipantSnapshot(
        RuntimeParticipant Participant,
        out ParticipantGameSnapshot Snapshot,
        out string ValidationMessage)
    {
        Snapshot = null;

        if (!_InitializationComplete || _GameKnowledge == null)
        {
            ValidationMessage =
                "Participant snapshot is unavailable before Match initialization.";
            return false;
        }

        GameMovement.ParticipantView MovementView =
            null;

        GamePreBattle.ParticipantView PreBattleView =
            null;

        GameSearch.ParticipantView SearchView =
            null;

        GameBattle.ParticipantView BattleView =
            null;

        GameEnd.ParticipantView EndView =
            null;


        _GameMovement?.TryGetView(
            Participant,
            out MovementView
        );


        _GamePreBattle?.TryGetView(
            Participant,
            out PreBattleView
        );

        _GameSearch?.TryGetView(
            Participant,
            out SearchView
        );

        _GameBattle?.TryGetView(
            Participant,
            out BattleView
        );

        _GameEnd?.TryGetView(
            Participant,
            out EndView
        );

        RefreshScores();

        ParticipantMatchStateSnapshot MatchState =
            CreateParticipantMatchState(Participant);


        return _GameKnowledge.TryCreateSnapshot(
            Participant,
            _CurrentPhase,
            _TurnNumber,
            _MatchStarted,
            _FirstPlayer,
            MatchState,
            MovementView,
            PreBattleView,
            SearchView,
            BattleView,
            EndView,
            out Snapshot,
            out ValidationMessage);
    }


    private bool InitializeParticipantAPI()
    {
        _ParticipantAPI = new GameParticipantAPI();

        if (!_ParticipantAPI.Initialize(
                this,
                _ParticipantA,
                _ParticipantB,
                out string ValidationMessage))
        {
            _ParticipantAPI = null;

            FailInitialization(
                $"Participant API initialization failed: {ValidationMessage}"
            );

            return false;
        }

        return true;
    }


    private bool InitializeAIControllers(
        out string ValidationMessage)
    {
        _AIControllerInstances.Clear();

        if (!InitializeAIController(
                _ParticipantA,
                out ValidationMessage) ||
            !InitializeAIController(
                _ParticipantB,
                out ValidationMessage))
        {
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    private bool InitializeAIController(
        RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (Participant?.Participant == null ||
            Participant.Participant.ParticipantType != ParticipantType.AI)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        ParticipantInfo SourceInfo =
            Participant.Participant.SourceInfo;
        GameObject ControllerPrefab =
            SourceInfo != null
                ? SourceInfo.AIControllerPrefab
                : null;

        if (ControllerPrefab == null)
        {
            ValidationMessage =
                $"AI participant '{Participant.Participant.DisplayName}' has no controller prefab.";
            return false;
        }

        if (!_ParticipantAPI.TryGetParticipantAccess(
                Participant,
                out GameParticipantAPI.ParticipantAccess Access))
        {
            ValidationMessage =
                $"AI participant '{Participant.Participant.DisplayName}' has no participant API access.";
            return false;
        }

        GameObject ControllerInstance = Instantiate(
            ControllerPrefab,
            transform,
            false);
        ControllerInstance.name =
            $"AI_{Participant.Participant.DisplayName}";

        ParticipantAIController Controller =
            ControllerInstance.GetComponent<ParticipantAIController>();

        if (Controller == null)
        {
            ValidationMessage =
                $"AI controller prefab '{ControllerPrefab.name}' has no ParticipantAIController component.";
            Destroy(ControllerInstance);
            return false;
        }

        if (!Controller.Initialize(Access, out ValidationMessage))
        {
            Destroy(ControllerInstance);
            return false;
        }

        _AIControllerInstances.Add(ControllerInstance);
        ValidationMessage = string.Empty;
        return true;
    }


    private void PrepareDeployment()
    {
        _CurrentPhase =
            GameplayPhase.Deployment;


        _ParticipantA.SetDeploymentLocked(
            false
        );

        _ParticipantB.SetDeploymentLocked(
            false
        );


        _GameDeployment.ResetAll();


        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            FailInitialization(KnowledgeMessage);
            return;
        }


        // No RuntimeUnits are created here.
        //
        // GameDeployment references the top-level Fleet Entry states owned by
        // GameUnits and stores only tentative placement data.
        //
        // RuntimeUnits and committed GameMap occupancy are created only when
        // GameMaster later completes a successful Confirm transaction.
        _MatchStarted =
            false;

        _FirstPlayer =
            null;

        _TurnNumber =
            0;

        _CompletedTurnNumber =
            0;

        _MatchComplete = false;
        _WinningParticipant = null;
        _MatchDraw = false;
        _MatchResolutionReason = string.Empty;
        _ParticipantAScore = 0;
        _ParticipantBScore = 0;
    }


    private void BeginMatchAfterDeployment()
    {
        _FirstPlayer =
            UnityEngine.Random.Range(
                0,
                2
            ) == 0
                ? _ParticipantA
                : _ParticipantB;

        _TurnNumber =
            1;

        _MatchStarted =
            true;

        _CurrentPhase =
            GameplayPhase.Movement;


        float MovementSeconds = 60f;


        if (_GameMode.TryGetPhaseTimeLimit(
                GameplayPhase.Movement,
                out float AuthoredMovementSeconds))
        {
            MovementSeconds = AuthoredMovementSeconds;
        }


        if (!_GameMovement.BeginPhase(
                _FirstPlayer,
                MovementSeconds,
                out string MovementMessage))
        {
            Debug.LogError(
                $"Movement phase initialization failed: {MovementMessage}",
                this
            );

            return;
        }


        if (!_GameKnowledge.Refresh(
                _CurrentPhase,
                out string KnowledgeMessage))
        {
            Debug.LogError(
                $"GameKnowledge refresh failed: {KnowledgeMessage}",
                this);
        }

        EvaluateImmediateMatchConclusion(false);


        Debug.Log(
            $"Deployment complete. '{_FirstPlayer.Participant.DisplayName}' has first-player priority for Turn {_TurnNumber}.",
            this
        );
    }


    private void FailInitialization(
        string Message
    )
    {
        Debug.LogError(
            $"GameMaster initialization failed: {Message}",
            this
        );


        for (int i = 0; i < _AIControllerInstances.Count; i++)
        {
            if (_AIControllerInstances[i] != null)
                Destroy(_AIControllerInstances[i]);
        }

        _AIControllerInstances.Clear();


        _GameDeployment =
            null;

        _GameUnitEffects =
            null;

        _GameKnowledge =
            null;

        _GameMovement =
            null;

        _GamePreBattle =
            null;

        _GameSearch =
            null;

        _GameBattle =
            null;

        _GameEnd =
            null;

        _ParticipantAPI =
            null;

        _InitializationComplete =
            false;

        _MatchStarted =
            false;

        _FirstPlayer =
            null;

        _TurnNumber =
            0;

        _CompletedTurnNumber =
            0;

        _CurrentPhase =
            GameplayPhase.None;

        _MatchComplete = false;
        _WinningParticipant = null;
        _MatchDraw = false;
        _MatchResolutionReason = string.Empty;
        _ParticipantAInitialFleetSize = 0;
        _ParticipantBInitialFleetSize = 0;
        _ParticipantAScore = 0;
        _ParticipantBScore = 0;

        enabled =
            false;
    }
}





