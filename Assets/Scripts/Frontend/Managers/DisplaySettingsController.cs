using UnityEngine;

// Attach To Controller_DisplaySettings GameObject
public class DisplaySettingsController : MonoBehaviour
{
    // Reference To Settings Manager
    [SerializeField] private SettingsManager _SettingsManager;

    // Reference To The Fullscreen Toggle State
    [SerializeField] private StateToggle _FullscreenToggle;


    private void OnEnable()
    {
        // Subscribe Only To Actual Player Interaction
        _FullscreenToggle.UserToggled += OnFullscreenChanged;
    }


    private void OnDisable()
    {
        // Unsubscribe From Player Interaction
        _FullscreenToggle.UserToggled -= OnFullscreenChanged;
    }


    private void Start()
    {
        // Initialize The Checkbox From The SettingsManager's Current State
        _FullscreenToggle.IntitializeStateToggle(_SettingsManager.Fullscreen);
    }


    private void OnFullscreenChanged(bool IsFullscreen)
    {
        // Apply And Save The New Fullscreen Setting
        _SettingsManager.SetFullscreen(IsFullscreen);
    }
}