using UnityEngine;
using UnityEngine.UI;

// Connects The Lobby UI To The Lobby Runtime Systems
// Attach To Screen_Singleplayer GameObject
public class LobbyScreenController : MonoBehaviour
{
    // Main Lobby Logic / Runtime State
    [SerializeField] private LobbyController _LobbyController;

    // Participant Cards
    [SerializeField] private ParticipantCard _PlayerCard;
    [SerializeField] private ParticipantCard _OpponentCard;

    // Match Setting Selectors
    [SerializeField] private CycleSelector _MapSelector;
    [SerializeField] private CycleSelector _GameModeSelector;

    // Ready Controls
    [SerializeField] private Button _ReadyBTN;
    [SerializeField] private Button _CancelBTN;


    private void OnEnable()
    {
        // Map Selector
        _MapSelector.PreviousClicked += OnPreviousMap;
        _MapSelector.NextClicked += OnNextMap;

        // Game Mode Selector
        _GameModeSelector.PreviousClicked += OnPreviousGameMode;
        _GameModeSelector.NextClicked += OnNextGameMode;

        // Ready Controls
        _ReadyBTN.onClick.AddListener(OnReady);
        _CancelBTN.onClick.AddListener(OnCancelReady);
    }


    private void Start()
    {
        // LobbyController Has Already Created The Runtime Lobby In Awake
        // So Now Display Its Current State
        RefreshScreen();
    }


    private void OnDisable()
    {
        // Map Selector
        _MapSelector.PreviousClicked -= OnPreviousMap;
        _MapSelector.NextClicked -= OnNextMap;

        // Game Mode Selector
        _GameModeSelector.PreviousClicked -= OnPreviousGameMode;
        _GameModeSelector.NextClicked -= OnNextGameMode;

        // Ready Controls
        _ReadyBTN.onClick.RemoveListener(OnReady);
        _CancelBTN.onClick.RemoveListener(OnCancelReady);
    }


    // --------------------
    // MAP
    // --------------------

    private void OnPreviousMap()
    {
        _LobbyController.SelectPreviousMap();

        // Changing Match Configuration Cancels Player Ready State
        // So Refresh Everything
        RefreshScreen();
    }


    private void OnNextMap()
    {
        _LobbyController.SelectNextMap();

        RefreshScreen();
    }


    // --------------------
    // GAME MODE
    // --------------------

    private void OnPreviousGameMode()
    {
        _LobbyController.SelectPreviousGameMode();

        RefreshScreen();
    }


    private void OnNextGameMode()
    {
        _LobbyController.SelectNextGameMode();

        RefreshScreen();
    }


    // --------------------
    // READY STATE
    // --------------------

    private void OnReady()
    {
        _LobbyController.SetPlayerReady(true);

        RefreshScreen();
    }


    private void OnCancelReady()
    {
        _LobbyController.SetPlayerReady(false);

        RefreshScreen();
    }


    // --------------------
    // DISPLAY
    // --------------------

    private void RefreshScreen()
    {
        // REUSE The Current Runtime Lobby
        LobbyState Lobby = _LobbyController.CurrentLobby;

        // Safety Check
        if (Lobby == null)
        {
            return;
        }


        // LEFT Participant Faces RIGHT
        _PlayerCard.DisplayParticipant(
            Lobby.PlayerSlot1,
            true
        );


        // RIGHT Participant Faces LEFT
        _OpponentCard.DisplayParticipant(
            Lobby.PlayerSlot2,
            false
        );


        // Display Selected Map
        if (Lobby.SelectedMap != null)
        {
            _MapSelector.SetValue(
                Lobby.SelectedMap.MapName
            );
        }


        // Display Selected Game Mode
        if (Lobby.SelectedGameMode != null)
        {
            _GameModeSelector.SetValue(
                Lobby.SelectedGameMode.GameModeName
            );
        }


        // Ready / Cancel Buttons
        bool PlayerIsReady =
            Lobby.PlayerSlot1.IsReady;

        // If Player Is NOT Ready:
        // Show Ready, Hide Cancel
        _ReadyBTN.gameObject.SetActive(!PlayerIsReady);

        // If Player IS Ready:
        // Hide Ready, Show Cancel
        _CancelBTN.gameObject.SetActive(PlayerIsReady);
    }
}