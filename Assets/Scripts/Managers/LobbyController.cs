using UnityEngine;

// Controls The Current Lobby And Its Runtime State
public class LobbyController : MonoBehaviour
{
    // Available Maps & Game Modes
    [SerializeField] private MapCatalog _MapCatalog;
    [SerializeField] private GameModeCatalog _GameModeCatalog;

    // Singleplayer Participant Templates
    [SerializeField] private ParticipantInfo _PlayerInfo;
    [SerializeField] private ParticipantInfo _AIInfo;


    // Current Selection Indexes
    private int _MapIndex;
    private int _GameModeIndex;


    // Current Runtime Lobby State
    public LobbyState CurrentLobby { get; private set; }


    private void Awake()
    {
        InitializeLobby();
    }


    // Create And Configure The Singleplayer Lobby
    private void InitializeLobby()
    {
        // CREATE A NEW Runtime Lobby
        CurrentLobby = new LobbyState();

        // Initialize Its Two Empty Participant Slots
        CurrentLobby.InitializeLobby();


        // Fill Player 1 Slot From EXISTING ParticipantInfo Asset
        CurrentLobby.PlayerSlot1.InitializeSlot(
            _PlayerInfo.ParticipantType,
            _PlayerInfo.DisplayName,
            _PlayerInfo.DefaultRank,
            _PlayerInfo.PortraitLeft,
            _PlayerInfo.PortraitRight
        );


        // Fill Player 2 Slot From EXISTING AI ParticipantInfo Asset
        CurrentLobby.PlayerSlot2.InitializeSlot(
            _AIInfo.ParticipantType,
            _AIInfo.DisplayName,
            _AIInfo.DefaultRank,
            _AIInfo.PortraitLeft,
            _AIInfo.PortraitRight
        );


        // Start At First Available Map
        _MapIndex = 0;

        if (_MapCatalog.Maps.Length > 0)
        {
            CurrentLobby.SetMap(_MapCatalog.Maps[_MapIndex]);
        }


        // Start At First Available Game Mode
        _GameModeIndex = 0;

        if (_GameModeCatalog.GameModes.Length > 0)
        {
            CurrentLobby.SetGameMode(
                _GameModeCatalog.GameModes[_GameModeIndex]
            );
        }


        // AI Is Automatically Ready In Singleplayer
        CurrentLobby.SetPlayer2Ready(true);
    }


    // Select Next Available Map
    public void SelectNextMap()
    {
        if (_MapCatalog.Maps.Length == 0)
        {
            return;
        }

        _MapIndex++;

        // Wrap Back To First Map
        if (_MapIndex >= _MapCatalog.Maps.Length)
        {
            _MapIndex = 0;
        }

        CurrentLobby.SetMap(_MapCatalog.Maps[_MapIndex]);

        // SetMap Cancels Both Ready States.
        // AI Is Automatically Ready Again In Singleplayer.
        CurrentLobby.SetPlayer2Ready(true);
    }


    // Select Previous Available Map
    public void SelectPreviousMap()
    {
        if (_MapCatalog.Maps.Length == 0)
        {
            return;
        }

        _MapIndex--;

        // Wrap To Last Map
        if (_MapIndex < 0)
        {
            _MapIndex = _MapCatalog.Maps.Length - 1;
        }

        CurrentLobby.SetMap(_MapCatalog.Maps[_MapIndex]);

        // Restore Automatic AI Ready State
        CurrentLobby.SetPlayer2Ready(true);
    }


    // Select Next Available Game Mode
    public void SelectNextGameMode()
    {
        if (_GameModeCatalog.GameModes.Length == 0)
        {
            return;
        }

        _GameModeIndex++;

        // Wrap Back To First Game Mode
        if (_GameModeIndex >= _GameModeCatalog.GameModes.Length)
        {
            _GameModeIndex = 0;
        }

        CurrentLobby.SetGameMode(
            _GameModeCatalog.GameModes[_GameModeIndex]
        );

        // Restore Automatic AI Ready State
        CurrentLobby.SetPlayer2Ready(true);
    }


    // Select Previous Available Game Mode
    public void SelectPreviousGameMode()
    {
        if (_GameModeCatalog.GameModes.Length == 0)
        {
            return;
        }

        _GameModeIndex--;

        // Wrap To Last Game Mode
        if (_GameModeIndex < 0)
        {
            _GameModeIndex =
                _GameModeCatalog.GameModes.Length - 1;
        }

        CurrentLobby.SetGameMode(
            _GameModeCatalog.GameModes[_GameModeIndex]
        );

        // Restore Automatic AI Ready State
        CurrentLobby.SetPlayer2Ready(true);
    }

    // Create A Final Match Configuration If The Lobby Is Ready
    public MatchConfig CreateMatchConfig()
    {
        // Do Not Create A Match Until Lobby Requirements Are Met
        if (!CurrentLobby.CanStartMatch)
        {
            return null;
        }

        // CREATE A NEW Final Match Configuration
        MatchConfig NewMatch = new MatchConfig();

        // Copy Current Lobby Configuration Into It
        NewMatch.InitializeMatch(CurrentLobby);

        return NewMatch;
    }


    // Ready / Cancel Ready For The Local Player
    public void SetPlayerReady(bool IsReady)
    {
        CurrentLobby.SetPlayer1Ready(IsReady);
    }
}