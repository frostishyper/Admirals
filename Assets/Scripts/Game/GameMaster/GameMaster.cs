using UnityEngine;


// Runtime representation of one Match participant.
//
// Runtime Units themselves are owned by GameUnits.
// This object identifies the Participant and the Fleet assigned to them.
public class RuntimeParticipant
{
    public ParticipantSlot Participant
    {
        get;
        private set;
    }


    public Definition_Fleet Fleet
    {
        get;
        private set;
    }


    public bool DeploymentLocked
    {
        get;
        private set;
    }


    public RuntimeParticipant(
        ParticipantSlot Participant,
        Definition_Fleet Fleet
    )
    {
        this.Participant =
            Participant;


        this.Fleet =
            Fleet;


        DeploymentLocked =
            false;
    }


    internal void SetDeploymentLocked(
        bool Locked
    )
    {
        DeploymentLocked =
            Locked;
    }
}


// Authoritative gameplay orchestrator.
//
// Current responsibility:
// - receive finalized Match
// - establish Participants
// - instantiate authored Map
// - create authoritative GameMap
// - establish separate Unit-state rosters
// - establish tentative Deployment state
// - prepare Deployment
//
// RuntimeUnits are not created until a participant confirms
// a valid Deployment.
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


    public MatchConfig MatchConfig
    {
        get
        {
            return _MatchConfig;
        }
    }


    public Definition_GameMode GameMode
    {
        get
        {
            return _GameMode;
        }
    }


    public RuntimeParticipant ParticipantA
    {
        get
        {
            return _ParticipantA;
        }
    }


    public RuntimeParticipant ParticipantB
    {
        get
        {
            return _ParticipantB;
        }
    }


    public GameObject MapInstance
    {
        get
        {
            return _MapInstance;
        }
    }


    public MapAuthoring MapAuthoring
    {
        get
        {
            return _MapAuthoring;
        }
    }


    public GameMap GameMap
    {
        get
        {
            return _GameMap;
        }
    }


    public GameUnits GameUnits
    {
        get
        {
            return _GameUnits;
        }
    }


    public GameDeployment GameDeployment
    {
        get
        {
            return _GameDeployment;
        }
    }


    public GameplayPhase CurrentPhase
    {
        get
        {
            return _CurrentPhase;
        }
    }


    public bool InitializationComplete
    {
        get
        {
            return _InitializationComplete;
        }
    }


    public bool MatchStarted
    {
        get
        {
            return _MatchStarted;
        }
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


        if (MatchSession.Instance ==
            null)
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


        if (!InitializeParticipants())
        {
            return false;
        }


        if (!InitializeMap())
        {
            return false;
        }


        if (!InitializeUnitState())
        {
            return false;
        }


        if (!InitializeDeploymentState())
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


    private bool InitializeParticipants()
    {
        if (_MatchConfig.PlayerSlot1 ==
            null)
        {
            FailInitialization(
                "Participant A snapshot is missing."
            );


            return false;
        }


        if (_MatchConfig.PlayerSlot2 ==
            null)
        {
            FailInitialization(
                "Participant B snapshot is missing."
            );


            return false;
        }


        if (_MatchConfig.Player1Fleet ==
            null)
        {
            FailInitialization(
                "Participant A has no resolved Fleet."
            );


            return false;
        }


        if (_MatchConfig.Player2Fleet ==
            null)
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


        if (MapPrefab ==
            null)
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


        if (_MapInstance ==
            null)
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


        if (_MapAuthoring ==
            null)
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


        if (_GameMap ==
            null)
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


        _GameDeployment.ParticipantA.Reset();

        _GameDeployment.ParticipantB.Reset();


        // No RuntimeUnits are created here.
        //
        // GameDeployment references the same top-level Fleet Entry states
        // already owned by GameUnits and stores only tentative placement data.
        //
        // RuntimeUnits are created only when GameMaster successfully commits
        // a participant's confirmed Deployment.
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