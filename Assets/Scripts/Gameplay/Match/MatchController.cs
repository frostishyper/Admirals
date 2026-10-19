using UnityEngine;

// Attach To Controller_Match GameObject
// Initializes And Owns The Live Match State
public class MatchController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MapController _MapController;

    [SerializeField] private Transform _GameModeRoot;


    private MatchConfig _ActiveMatch;

    private GameModeInfo _ActiveGameMode;

    private GameObject _ActiveGameModeObject;

    private GameModeRules _ActiveGameModeRules;


    private MatchSideState _SideOneState;

    private MatchSideState _SideTwoState;


    private int _NextRuntimeUnitID = 1;


    public MatchConfig ActiveMatch
    {
        get
        {
            return _ActiveMatch;
        }
    }


    public GameModeInfo ActiveGameMode
    {
        get
        {
            return _ActiveGameMode;
        }
    }


    public GameModeRules ActiveGameModeRules
    {
        get
        {
            return _ActiveGameModeRules;
        }
    }


    public MatchSideState SideOneState
    {
        get
        {
            return _SideOneState;
        }
    }


    public MatchSideState SideTwoState
    {
        get
        {
            return _SideTwoState;
        }
    }


    private void Start()
    {
        InitializeMatch();
    }


    private void InitializeMatch()
    {
        if (MatchSession.Instance == null)
        {
            Debug.LogError(
                "Game scene was loaded without an active MatchSession."
            );

            return;
        }


        if (!MatchSession.Instance.HasActiveMatch)
        {
            Debug.LogError(
                "MatchSession exists but contains no active MatchConfig."
            );

            return;
        }


        _ActiveMatch =
            MatchSession.Instance.ActiveMatch;


        if (_ActiveMatch.SelectedMap == null)
        {
            Debug.LogError(
                "Active MatchConfig contains no selected Map."
            );

            return;
        }


        if (_ActiveMatch.SelectedGameMode == null)
        {
            Debug.LogError(
                "Active MatchConfig contains no selected GameMode."
            );

            return;
        }


        _ActiveGameMode =
            _ActiveMatch.SelectedGameMode;


        _MapController.LoadMap(
            _ActiveMatch.SelectedMap
        );


        if (!LoadGameMode(
                _ActiveGameMode))
        {
            return;
        }


        BuildStartingForces();


        Debug.Log(
            $"Match Initialized\n" +
            $"Map: {_ActiveMatch.SelectedMap.MapName}\n" +
            $"Game Mode: {_ActiveGameMode.GameModeName}\n" +
            $"Side One Units: {_SideOneState.UnitCount}\n" +
            $"Side Two Units: {_SideTwoState.UnitCount}\n" +
            $"Victory Condition: {_ActiveGameModeRules.VictoryCondition}"
        );
    }


    private bool LoadGameMode(
        GameModeInfo GameMode
    )
    {
        if (GameMode.GameModeReference == null)
        {
            Debug.LogError(
                $"GameMode '{GameMode.GameModeName}' has no GameModeReference assigned."
            );

            return false;
        }


        _ActiveGameModeObject =
            Instantiate(
                GameMode.GameModeReference,
                _GameModeRoot,
                false
            );


        _ActiveGameModeRules =
            _ActiveGameModeObject.GetComponent<GameModeRules>();


        if (_ActiveGameModeRules == null)
        {
            Debug.LogError(
                $"GameMode prefab '{GameMode.GameModeReference.name}' has no GameModeRules component."
            );

            Destroy(
                _ActiveGameModeObject
            );


            _ActiveGameModeObject =
                null;


            return false;
        }


        Debug.Log(
            $"Loaded Game Mode: {GameMode.GameModeName}"
        );


        return true;
    }


    private void BuildStartingForces()
    {
        _SideOneState =
            new MatchSideState(
                MatchSide.SideOne
            );


        _SideTwoState =
            new MatchSideState(
                MatchSide.SideTwo
            );


        AddRosterToSide(
            _SideOneState,
            _ActiveGameModeRules.SideOneStartingRoster
        );


        AddRosterToSide(
            _SideTwoState,
            _ActiveGameModeRules.SideTwoStartingRoster
        );
    }


    private void AddRosterToSide(
        MatchSideState SideState,
        StartingUnitEntry[] Roster
    )
    {
        if (Roster == null)
        {
            return;
        }


        for (
            int EntryIndex = 0;
            EntryIndex < Roster.Length;
            EntryIndex++
        )
        {
            StartingUnitEntry Entry =
                Roster[EntryIndex];


            if (Entry == null ||
                Entry.Unit == null ||
                Entry.Count <= 0)
            {
                continue;
            }


            for (
                int UnitIndex = 0;
                UnitIndex < Entry.Count;
                UnitIndex++
            )
            {
                UnitRuntimeState Unit =
                    new UnitRuntimeState(
                        _NextRuntimeUnitID,
                        Entry.Unit,
                        SideState.Side
                    );


                _NextRuntimeUnitID++;


                SideState.AddUnit(
                    Unit
                );
            }
        }
    }
}