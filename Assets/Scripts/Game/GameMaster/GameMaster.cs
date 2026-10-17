using System;
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

    private GameParticipantAPI _ParticipantAPI;


    private GameplayPhase _CurrentPhase =
        GameplayPhase.None;


    private bool _InitializationComplete;

    private bool _MatchStarted;

    private RuntimeParticipant _FirstPlayer;

    private int _TurnNumber;


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
            !InitializeParticipantAPI())
        {
            return false;
        }


        PrepareDeployment();

        _InitializationComplete =
            true;


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


        if (!_GameUnitEffects.TryApplyTagActions(
                Unit,
                Ability.AbilityOutcome.Actions,
                out ValidationMessage))
        {
            return false;
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


    private void Update()
    {
        if (!_InitializationComplete ||
            _CurrentPhase != GameplayPhase.Movement ||
            _GameMovement == null ||
            !_GameMovement.Tick(
                Time.deltaTime,
                out RuntimeParticipant TimedOutParticipant,
                out bool AllParticipantsEnded))
        {
            return;
        }


        Debug.Log(
            $"'{TimedOutParticipant.Participant.DisplayName}' ran out of Movement time on Turn {_TurnNumber}.",
            this
        );


        if (AllParticipantsEnded)
        {
            CompleteMovementPhase();
        }


        _ParticipantAPI?.NotifySnapshotsChanged();
    }


    private void CompleteMovementPhase()
    {
        _CurrentPhase = GameplayPhase.PreBattle;


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


        return true;
    }


    private bool InitializeUnitEffects()
    {
        _GameUnitEffects =
            new GameUnitEffects();


        if (!_GameUnitEffects.Initialize(
                _GameMap,
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


        _GameMovement?.TryGetView(
            Participant,
            out MovementView
        );


        return _GameKnowledge.TryCreateSnapshot(
            Participant,
            _CurrentPhase,
            _TurnNumber,
            _MatchStarted,
            _FirstPlayer,
            MovementView,
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


        _GameDeployment =
            null;

        _GameUnitEffects =
            null;

        _GameKnowledge =
            null;

        _GameMovement =
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

        _CurrentPhase =
            GameplayPhase.None;

        enabled =
            false;
    }
}





