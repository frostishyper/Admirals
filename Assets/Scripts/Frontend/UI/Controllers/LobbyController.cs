using UnityEngine;

// Controls The Current Lobby And Its Runtime State
// Attach To Screen_Singleplayer GameObject
public class LobbyController : MonoBehaviour
{
    // Available Maps & Game Modes
    [SerializeField] private MapCatalog _MapCatalog;
    [SerializeField] private GameModeCatalog _GameModeCatalog;

    // Singleplayer Participant Templates
    [SerializeField] private ParticipantInfo _PlayerInfo;
    [SerializeField] private ParticipantInfo _AIInfo;

    [Header("Singleplayer AI Selection")]
    [SerializeField] private ParticipantInfo[] _AvailableAIInfos;


    // Current Selection Indexes
    private int _MapIndex;
    private int _GameModeIndex;
    private int _AIIndex;


    // Current Runtime Lobby State
    public LobbyState CurrentLobby { get; private set; }

    public bool SupportsAISelection =>
        FindFirstValidAIIndex() >= 0;

    public ParticipantInfo SelectedAIInfo
    {
        get
        {
            if (IsValidAIIndex(_AIIndex))
            {
                return _AvailableAIInfos[_AIIndex];
            }

            return _AIInfo;
        }
    }


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
            _PlayerInfo.PortraitRight,
            _PlayerInfo
        );


        // Resolve The Initial AI Selection, Then Fill Player 2 From It.
        InitializeAISelection();
        ApplySelectedOpponent();


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


    // Select Next Authored AI Participant
    public void SelectNextAI()
    {
        SelectAI(1);
    }


    // Select Previous Authored AI Participant
    public void SelectPreviousAI()
    {
        SelectAI(-1);
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


    private void InitializeAISelection()
    {
        _AIIndex = -1;

        if (_AvailableAIInfos == null)
        {
            return;
        }

        for (int i = 0; i < _AvailableAIInfos.Length; i++)
        {
            if (_AvailableAIInfos[i] == _AIInfo &&
                IsValidAIIndex(i))
            {
                _AIIndex = i;
                return;
            }
        }

        _AIIndex = FindFirstValidAIIndex();
    }


    private void SelectAI(int Direction)
    {
        if (!SupportsAISelection)
        {
            return;
        }

        int StartIndex = IsValidAIIndex(_AIIndex)
            ? _AIIndex
            : FindFirstValidAIIndex();

        int CandidateIndex = StartIndex;

        for (int i = 0; i < _AvailableAIInfos.Length; i++)
        {
            CandidateIndex =
                (CandidateIndex + Direction + _AvailableAIInfos.Length) %
                _AvailableAIInfos.Length;

            if (IsValidAIIndex(CandidateIndex))
            {
                _AIIndex = CandidateIndex;
                ApplySelectedOpponent();
                return;
            }
        }
    }


    private void ApplySelectedOpponent()
    {
        ParticipantInfo OpponentInfo = SelectedAIInfo;

        if (OpponentInfo == null)
        {
            return;
        }

        CurrentLobby.CancelAllReady();

        CurrentLobby.PlayerSlot2.InitializeSlot(
            OpponentInfo.ParticipantType,
            OpponentInfo.DisplayName,
            OpponentInfo.DefaultRank,
            OpponentInfo.PortraitLeft,
            OpponentInfo.PortraitRight,
            OpponentInfo
        );

        // The computer-controlled seat does not wait on a lobby ready action.
        CurrentLobby.SetPlayer2Ready(true);
    }


    private int FindFirstValidAIIndex()
    {
        if (_AvailableAIInfos == null)
        {
            return -1;
        }

        for (int i = 0; i < _AvailableAIInfos.Length; i++)
        {
            if (IsValidAIIndex(i))
            {
                return i;
            }
        }

        return -1;
    }


    private bool IsValidAIIndex(int Index)
    {
        return
            _AvailableAIInfos != null &&
            Index >= 0 &&
            Index < _AvailableAIInfos.Length &&
            _AvailableAIInfos[Index] != null &&
            _AvailableAIInfos[Index].ParticipantType == ParticipantType.AI;
    }
}
