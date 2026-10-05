using UnityEngine;

// Static / Authored Information For A Participant
[CreateAssetMenu(
    fileName = "Participant_New",
    menuName = "Admirals/Participant Info"
)]
public class ParticipantInfo : ScriptableObject
{
    // What Kind Of Participant This Is
    [SerializeField] private ParticipantType _ParticipantType;

    // Default Display Name
    [SerializeField] private string _DisplayName;

    // Default Rank
    [SerializeField] private PlayerRank _DefaultRank;

    // Portrait Facing Left
    [SerializeField] private Sprite _PortraitLeft;

    // Portrait Facing Right
    [SerializeField] private Sprite _PortraitRight;


    // Read-Only Access For Other Systems

    public ParticipantType ParticipantType
    {
        get { return _ParticipantType; }
    }

    public string DisplayName
    {
        get { return _DisplayName; }
    }

    public PlayerRank DefaultRank
    {
        get { return _DefaultRank; }
    }

    public Sprite PortraitLeft
    {
        get { return _PortraitLeft; }
    }

    public Sprite PortraitRight
    {
        get { return _PortraitRight; }
    }
}