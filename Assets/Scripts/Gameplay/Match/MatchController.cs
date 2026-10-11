using UnityEngine;

// Attach To Controller_Match GameObject
// Initializes The Game Scene From The Frontend Match Configuration
public class MatchController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MapController _MapController;

    [SerializeField] private Transform _GameModeRoot;


    private MatchConfig _ActiveMatch;

    private GameModeInfo _ActiveGameMode;

    private GameObject _ActiveGameModeObject;


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


    public GameObject ActiveGameModeObject
    {
        get
        {
            return _ActiveGameModeObject;
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


        LoadGameMode(
            _ActiveGameMode
        );


        Debug.Log(
            $"Match Initialized\n" +
            $"Map: {_ActiveMatch.SelectedMap.MapName}\n" +
            $"Game Mode: {_ActiveGameMode.GameModeName}"
        );
    }


    private void LoadGameMode(
        GameModeInfo GameMode
    )
    {
        if (GameMode.GameModeReference == null)
        {
            Debug.LogWarning(
                $"GameMode '{GameMode.GameModeName}' has no GameModeReference assigned."
            );

            return;
        }


        _ActiveGameModeObject =
            Instantiate(
                GameMode.GameModeReference,
                _GameModeRoot,
                false
            );


        Debug.Log(
            $"Loaded Game Mode: {GameMode.GameModeName}"
        );
    }
}