using System;
using UnityEngine;

// Runtime State For The Current Lobby
[Serializable]
public class LobbyState
{
    // Participant Slots
    [SerializeField] private ParticipantSlot _PlayerSlot1;
    [SerializeField] private ParticipantSlot _PlayerSlot2;

    // Current Match Configuration
    [SerializeField] private MapInfo _SelectedMap;
    [SerializeField] private GameModeInfo _SelectedGameMode;


    // Read-Only Access

    public ParticipantSlot PlayerSlot1
    {
        get { return _PlayerSlot1; }
    }

    public ParticipantSlot PlayerSlot2
    {
        get { return _PlayerSlot2; }
    }

    public MapInfo SelectedMap
    {
        get { return _SelectedMap; }
    }

    public GameModeInfo SelectedGameMode
    {
        get { return _SelectedGameMode; }
    }


    // Match Can Only Start When:
    // - Both Slots Are Occupied
    // - Both Participants Are Ready
    // - A Map Is Selected
    // - A Game Mode Is Selected
    public bool CanStartMatch
    {
        get
        {
            return
                _PlayerSlot1.IsOccupied &&
                _PlayerSlot2.IsOccupied &&
                _PlayerSlot1.IsReady &&
                _PlayerSlot2.IsReady &&
                _SelectedMap != null &&
                _SelectedGameMode != null;
        }
    }


    // Initialize Empty Lobby State
    public void InitializeLobby()
    {
        _PlayerSlot1 = new ParticipantSlot();
        _PlayerSlot2 = new ParticipantSlot();

        _SelectedMap = null;
        _SelectedGameMode = null;
    }


    // Change Selected Map
    public void SetMap(MapInfo NewMap)
    {
        // Do Nothing If The Same Map Is Already Selected
        if (_SelectedMap == NewMap)
        {
            return;
        }

        _SelectedMap = NewMap;

        // Match Configuration Changed
        // Cancel Existing Ready States
        CancelAllReady();
    }


    // Change Selected Game Mode
    public void SetGameMode(GameModeInfo NewGameMode)
    {
        // Do Nothing If The Same Game Mode Is Already Selected
        if (_SelectedGameMode == NewGameMode)
        {
            return;
        }

        _SelectedGameMode = NewGameMode;

        // Match Configuration Changed
        // Cancel Existing Ready States
        CancelAllReady();
    }


    // Change Ready State For Player Slot 1
    public void SetPlayer1Ready(bool IsReady)
    {
        _PlayerSlot1.SetReady(IsReady);
    }


    // Change Ready State For Player Slot 2
    public void SetPlayer2Ready(bool IsReady)
    {
        _PlayerSlot2.SetReady(IsReady);
    }


    // Cancel Ready State For Both Participants
    public void CancelAllReady()
    {
        _PlayerSlot1.SetReady(false);
        _PlayerSlot2.SetReady(false);
    }
}