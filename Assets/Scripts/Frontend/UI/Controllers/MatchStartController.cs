using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach To Controller_MatchStart GameObject
// Finalizes The Lobby And Transitions Into The Game Scene
public class MatchStartController : MonoBehaviour
{
    [Header("Lobby")]
    [SerializeField] private LobbyController _LobbyController;

    [Header("UI")]
    [SerializeField] private Button _StartMatchButton;

    [Header("Scene")]
    [SerializeField] private string _GameSceneName =
        "Game";


    private void Awake()
    {
        if (_StartMatchButton != null)
        {
            _StartMatchButton.onClick.AddListener(
                StartMatch
            );
        }
    }


    private void Update()
    {
        if (_StartMatchButton == null ||
            _LobbyController == null ||
            _LobbyController.CurrentLobby == null)
        {
            return;
        }


        _StartMatchButton.interactable =
            _LobbyController.CurrentLobby.CanStartMatch;
    }


    private void OnDestroy()
    {
        if (_StartMatchButton != null)
        {
            _StartMatchButton.onClick.RemoveListener(
                StartMatch
            );
        }
    }


    private void StartMatch()
    {
        if (_LobbyController == null)
        {
            Debug.LogError(
                "MatchStartController has no LobbyController assigned."
            );

            return;
        }


        if (MatchSession.Instance == null)
        {
            Debug.LogError(
                "No MatchSession exists in the Frontend scene."
            );

            return;
        }


        MatchConfig Match =
            _LobbyController.CreateMatchConfig();


        if (Match == null)
        {
            Debug.LogWarning(
                "Match could not start because the lobby is not ready."
            );

            return;
        }


        MatchSession.Instance.SetActiveMatch(
            Match
        );


        Debug.Log(
            $"Starting Match\n" +
            $"Map: {Match.SelectedMap.MapName}\n" +
            $"Game Mode: {Match.SelectedGameMode.GameModeName}"
        );


        SceneManager.LoadScene(
            _GameSceneName
        );
    }
}