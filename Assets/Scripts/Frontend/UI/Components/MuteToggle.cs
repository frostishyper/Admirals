using UnityEngine;
using UnityEngine.UI;

// For Segmented_Slider (Mutable) Variant, Attach To Slider_Icon
public class MuteToggle : MonoBehaviour
{
    // Reference To The Toggle That Controls The Mute State
    [SerializeField] private StateToggle _SourceToggle;

    // Referencce To Icons Whos Sprite Will Change
    [SerializeField] private Image _IconImage;

    // Muted & Non-Muted Sprites
    [SerializeField] private Sprite _MutedSprite;
    [SerializeField] private Sprite _NonMutedSprite;

    private void OnEnable()
    {
        // Synchronize The Icon With The Current State Of The Toggle
        UpdateIcon(_SourceToggle.IsEnabled);
        // Subscribe To The Toggle's State Change Event
        _SourceToggle.StateChanged += UpdateIcon;
    }

    private void OnDisable()
    {
        // Unsubscribe From The Toggle's State Change Event
        _SourceToggle.StateChanged -= UpdateIcon;
    }

    // Switch The Icon Sprite Based On The Toggle's State
    private void UpdateIcon(bool IsMuted)
    {
        _IconImage.sprite = IsMuted ? _MutedSprite : _NonMutedSprite;
    }
}
