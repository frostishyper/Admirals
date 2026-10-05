using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach To Card_Participant Root GameObject
public class ParticipantCard : MonoBehaviour
{
    // Participant Portrait
    [SerializeField] private Image _PortraitImage;

    // Participant Name
    [SerializeField] private TMP_Text _NameText;

    // Participant Rank Sprite
    [SerializeField] private Image _RankImage;

    // Ready / Standby Status Text
    [SerializeField] private TMP_Text _StatusText;

    // Status Colors
    [SerializeField] private Color _ReadyColor = Color.green;
    [SerializeField] private Color _StandbyColor = Color.red;


    // Update This Card Using Participant Slot Data
    public void DisplayParticipant(
        ParticipantSlot Participant,
        bool FaceRight)
    {
        // Name
        _NameText.text = Participant.DisplayName;

        // Rank
        if (Participant.Rank != null)
        {
            _RankImage.sprite = Participant.Rank.RankSprite;
        }

        // Portrait Direction
        _PortraitImage.sprite =
            FaceRight
            ? Participant.PortraitRight
            : Participant.PortraitLeft;

        // Ready / Standby
        UpdateReadyState(Participant.IsReady);
    }


    // Update Only The Ready State
    public void UpdateReadyState(bool IsReady)
    {
        _StatusText.text = IsReady ? "Ready" : "Standby";

        _StatusText.color =
            IsReady ? _ReadyColor : _StandbyColor;
    }
}