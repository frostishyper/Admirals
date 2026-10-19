using System;
using UnityEngine;

// Finalized Configuration For A Match
[Serializable]
public class MatchConfig
{
    // Finalized Participants
    [SerializeField] private ParticipantSlot _PlayerSlot1;
    [SerializeField] private ParticipantSlot _PlayerSlot2;

    // Finalized Map & Game Mode
    [SerializeField] private MapInfo _SelectedMap;
    [SerializeField] private GameModeInfo _SelectedGameMode;


    // Read-Only Access For Game Systems

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


    // Create Final Match Configuration From The Current Lobby
    public void InitializeMatch(LobbyState Lobby)
    {
        // COPY Participant Data So MatchConfig Does Not Share
        // The Same Mutable ParticipantSlot Objects As LobbyState
        _PlayerSlot1 = Lobby.PlayerSlot1.CreateCopy();
        _PlayerSlot2 = Lobby.PlayerSlot2.CreateCopy();

        // MapInfo & GameModeInfo Are Authored Data Assets
        // So Keeping References To Them Is Fine
        _SelectedMap = Lobby.SelectedMap;
        _SelectedGameMode = Lobby.SelectedGameMode;
    }
}