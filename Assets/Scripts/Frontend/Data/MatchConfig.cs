using System;
using UnityEngine;


// Serializable Snapshot Of One Finalized Match.
// Created By The Lobby And Consumed By The Game.
[Serializable]
public class MatchConfig
{
    [SerializeField]
    private ParticipantSlot _PlayerSlot1;

    [SerializeField]
    private ParticipantSlot _PlayerSlot2;

    [SerializeField]
    private MapInfo _SelectedMap;

    [SerializeField]
    private GameModeInfo _SelectedGameMode;


    [SerializeField]
    private GameObject _MapPrefab;

    [SerializeField]
    private Definition_GameMode _GameMode;

    [SerializeField]
    private Definition_Fleet _Player1Fleet;

    [SerializeField]
    private Definition_Fleet _Player2Fleet;


    public ParticipantSlot PlayerSlot1
    {
        get
        {
            return _PlayerSlot1;
        }
    }


    public ParticipantSlot PlayerSlot2
    {
        get
        {
            return _PlayerSlot2;
        }
    }


    public MapInfo SelectedMap
    {
        get
        {
            return _SelectedMap;
        }
    }


    public GameModeInfo SelectedGameMode
    {
        get
        {
            return _SelectedGameMode;
        }
    }


    public GameObject MapPrefab
    {
        get
        {
            return _MapPrefab;
        }
    }


    public Definition_GameMode GameMode
    {
        get
        {
            return _GameMode;
        }
    }


    public Definition_Fleet Player1Fleet
    {
        get
        {
            return _Player1Fleet;
        }
    }


    public Definition_Fleet Player2Fleet
    {
        get
        {
            return _Player2Fleet;
        }
    }


    // Takes the frontend Lobby snapshot and resolves the gameplay-side
    // assets the Game will actually consume.
    public void InitializeMatch(
        LobbyState Lobby
    )
    {
        if (Lobby == null)
        {
            Clear();

            return;
        }


        _PlayerSlot1 =
            Lobby.PlayerSlot1 != null
                ? Lobby.PlayerSlot1.CreateCopy()
                : null;

        _PlayerSlot2 =
            Lobby.PlayerSlot2 != null
                ? Lobby.PlayerSlot2.CreateCopy()
                : null;


        _SelectedMap =
            Lobby.SelectedMap;

        _SelectedGameMode =
            Lobby.SelectedGameMode;


        _MapPrefab =
            _SelectedMap != null
                ? _SelectedMap.MapReference
                : null;


        _GameMode =
            _SelectedGameMode != null
                ? _SelectedGameMode.GameModeReference
                : null;


        _Player1Fleet =
            null;

        _Player2Fleet =
            null;


        if (_GameMode == null)
        {
            return;
        }


        if (_GameMode.FleetSource ==
            FleetSourceMode.GameModeProvided)
        {
            if (_GameMode.SideA != null)
            {
                _Player1Fleet =
                    _GameMode.SideA.ProvidedFleet;
            }


            if (_GameMode.SideB != null)
            {
                _Player2Fleet =
                    _GameMode.SideB.ProvidedFleet;
            }
        }
    }


    public bool ValidateGameplayReady(
        out string ValidationMessage
    )
    {
        if (_PlayerSlot1 == null)
        {
            ValidationMessage =
                "Player Slot 1 is missing.";

            return false;
        }


        if (_PlayerSlot2 == null)
        {
            ValidationMessage =
                "Player Slot 2 is missing.";

            return false;
        }


        if (_SelectedMap == null)
        {
            ValidationMessage =
                "No MapInfo was selected.";

            return false;
        }


        if (_MapPrefab == null)
        {
            ValidationMessage =
                $"MapInfo '{_SelectedMap.MapName}' has no Map prefab reference.";

            return false;
        }


        MapAuthoring Map =
            _MapPrefab.GetComponent<MapAuthoring>();


        if (Map == null)
        {
            ValidationMessage =
                $"Map prefab '{_MapPrefab.name}' has no MapAuthoring component.";

            return false;
        }


        if (!Map.ValidateMap(
                out string MapValidationMessage))
        {
            ValidationMessage =
                $"Map '{_MapPrefab.name}' is invalid: {MapValidationMessage}";

            return false;
        }


        if (_SelectedGameMode == null)
        {
            ValidationMessage =
                "No GameModeInfo was selected.";

            return false;
        }


        if (_GameMode == null)
        {
            ValidationMessage =
                $"GameModeInfo '{_SelectedGameMode.GameModeName}' has no Definition_GameMode reference.";

            return false;
        }


        if (!_GameMode.Validate(
                out string GameModeValidationMessage))
        {
            ValidationMessage =
                $"GameMode '{_GameMode.Name}' is invalid: {GameModeValidationMessage}";

            return false;
        }


        if (_GameMode.FleetSource !=
            FleetSourceMode.GameModeProvided)
        {
            ValidationMessage =
                $"Fleet Source '{_GameMode.FleetSource}' is not implemented yet.";

            return false;
        }


        if (_Player1Fleet == null)
        {
            ValidationMessage =
                "Player 1 has no resolved Fleet.";

            return false;
        }


        if (_Player2Fleet == null)
        {
            ValidationMessage =
                "Player 2 has no resolved Fleet.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private void Clear()
    {
        _PlayerSlot1 =
            null;

        _PlayerSlot2 =
            null;

        _SelectedMap =
            null;

        _SelectedGameMode =
            null;

        _MapPrefab =
            null;

        _GameMode =
            null;

        _Player1Fleet =
            null;

        _Player2Fleet =
            null;
    }
}