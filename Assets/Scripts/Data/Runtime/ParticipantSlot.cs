using System;
using UnityEngine;

// Runtime Data For One Participant Slot In A Lobby
[Serializable]
public class ParticipantSlot
{
    // What Kind Of Participant Is In This Slot
    [SerializeField] private ParticipantType _ParticipantType;

    // Name Displayed In The Lobby
    [SerializeField] private string _DisplayName;

    // Participant's Current Rank
    [SerializeField] private PlayerRank _Rank;

    // Portrait Used When Participant Is Facing Left
    [SerializeField] private Sprite _PortraitLeft;

    // Portrait Used When Participant Is Facing Right
    [SerializeField] private Sprite _PortraitRight;

    // Whether This Slot Currently Has A Participant
    [SerializeField] private bool _IsOccupied;

    // Whether This Participant Is Ready
    [SerializeField] private bool _IsReady;


    // Read-Only Access For Other Systems

    public ParticipantType ParticipantType
    {
        get { return _ParticipantType; }
    }

    public string DisplayName
    {
        get { return _DisplayName; }
    }

    public PlayerRank Rank
    {
        get { return _Rank; }
    }

    public Sprite PortraitLeft
    {
        get { return _PortraitLeft; }
    }

    public Sprite PortraitRight
    {
        get { return _PortraitRight; }
    }

    public bool IsOccupied
    {
        get { return _IsOccupied; }
    }

    public bool IsReady
    {
        get { return _IsReady; }
    }


    // Fill This Slot With A Participant
    public void InitializeSlot(
        ParticipantType ParticipantType,
        string DisplayName,
        PlayerRank Rank,
        Sprite PortraitLeft,
        Sprite PortraitRight)
    {
        _ParticipantType = ParticipantType;
        _DisplayName = DisplayName;
        _Rank = Rank;
        _PortraitLeft = PortraitLeft;
        _PortraitRight = PortraitRight;

        _IsOccupied = true;
        _IsReady = false;
    }

    // Create A Separate Copy Of This Participant Slot
    public ParticipantSlot CreateCopy()
    {
        // CREATE A NEW ParticipantSlot
        ParticipantSlot SlotCopy = new ParticipantSlot();

        // Copy The Current Participant Data Into It
        SlotCopy.InitializeSlot(
            _ParticipantType,
            _DisplayName,
            _Rank,
            _PortraitLeft,
            _PortraitRight
        );

        // Copy Current Ready State
        SlotCopy.SetReady(_IsReady);

        return SlotCopy;
    }


    // Change Ready State
    public void SetReady(bool IsReady)
    {
        _IsReady = IsReady;
    }


    // Empty The Slot
    public void ClearSlot()
    {
        _DisplayName = "";
        _Rank = null;
        _PortraitLeft = null;
        _PortraitRight = null;

        _IsOccupied = false;
        _IsReady = false;
    }
}