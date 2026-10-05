using UnityEngine;
using UnityEngine.UI;

// Attach To Toggle_Checkbox Game Object
public class Checkbox : MonoBehaviour
{
    // Reference To The Toggle That Controls The Checkbox State
    [SerializeField] private StateToggle _SourceToggle;

    // Reference To Checkbox Image
    [SerializeField] private Image _CheckboxImage;

    // Enabled & Disabled Sprites
    [SerializeField] private Sprite _EnabledSprite;
    [SerializeField] private Sprite _DisabledSprite;

    private void OnEnable()
    {
        // Synchronize The Checkbox With The Current State Of The Toggle
        UpdateCheckbox(_SourceToggle.IsEnabled);

        // Subscribe To The Toggle's State Change Event
        _SourceToggle.StateChanged += UpdateCheckbox;
    }

    private void OnDisable()
    {
        // Unsubscribe From The Toggle's State Change Event
        _SourceToggle.StateChanged -= UpdateCheckbox;
    }

    // Switch The Checkbox Sprite Based On The Toggle's State
    private void UpdateCheckbox(bool IsEnabled)
    {
        _CheckboxImage.sprite = IsEnabled ? _EnabledSprite : _DisabledSprite;
    }
}