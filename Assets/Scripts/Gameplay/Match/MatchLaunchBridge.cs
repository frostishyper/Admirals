using UnityEngine;
using UnityEngine.SceneManagement;

// Attach To Screen_Singleplayer GameObject
// Bridges A Completed Frontend Lobby Into The Game Scene
public class MatchLaunchBridge : MonoBehaviour
{
    [Header("Lobby")]
    [SerializeField] private LobbyController _LobbyController;

    [Header("Scene")]
    [SerializeField] private string _GameSceneName = "Game";


    private bool _WasStartable;
    private bool _IsLaunching;


    private void OnEnable()
    {
        _WasStartable = false;
        _IsLaunching = false;
    }


    private void Update()
    {
        if (_IsLaunching)
        {
            return;
        }


        if (_LobbyController == null)
        {
            return;
        }


        if (_LobbyController.CurrentLobby == null)
        {
            return;
        }


        bool CanStartMatch =
            _LobbyController.CurrentLobby.CanStartMatch;


        if (CanStartMatch &&
            !_WasStartable)
        {
            LaunchMatch();
        }


        _WasStartable =
            CanStartMatch;
    }


    private void LaunchMatch()
    {
        MatchConfig Match =
            _LobbyController.CreateMatchConfig();


        if (Match == null)
        {
            Debug.LogError(
                "MatchLaunchBridge could not create MatchConfig."
            );

            return;
        }


        if (MatchSession.Instance == null)
        {
            Debug.LogError(
                "MatchLaunchBridge could not find MatchSession."
            );

            return;
        }


        _IsLaunching = true;


        MatchSession.Instance.SetActiveMatch(
            Match
        );


        Debug.Log(
            "Frontend Match Complete - Loading Game Scene."
        );


        SceneManager.LoadScene(
            _GameSceneName,
            LoadSceneMode.Single
        );
    }
}