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

    private GameDeployment _GameDeployment;


    private GameplayPhase _CurrentPhase =
        GameplayPhase.None;


    private bool _InitializationComplete;

    private bool _MatchStarted;


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


    public GameplayPhase CurrentPhase =>
        _CurrentPhase;


    public bool InitializationComplete =>
        _InitializationComplete;


    public bool MatchStarted =>
        _MatchStarted;


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
            !InitializeDeploymentState())
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
    public bool TryGetDeploymentView(
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


    public bool TryGetInitialDeploymentFacing(
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


    public bool TryGetDeploymentZoneCoordinates(
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


    public bool TryGetDeploymentPlacementTiles(
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
    public bool ValidateDeploymentPlacement(
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
    public bool TrySubmitDeploymentPlacement(
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


    public bool TrySelectDeploymentPlacement(
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


    public bool TryClearDeploymentSelection(
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
    public bool TryRotateDeploymentPlacementClockwise(
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


    public bool TryRotateDeploymentPlacementCounterClockwise(
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


    public bool TryResetDeployment(
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


        // No RuntimeUnits are created here.
        //
        // GameDeployment references the top-level Fleet Entry states owned by
        // GameUnits and stores only tentative placement data.
        //
        // RuntimeUnits and committed GameMap occupancy are created only when
        // GameMaster later completes a successful Confirm transaction.
        _MatchStarted =
            false;
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

        _InitializationComplete =
            false;

        _MatchStarted =
            false;

        _CurrentPhase =
            GameplayPhase.None;

        enabled =
            false;
    }
}
