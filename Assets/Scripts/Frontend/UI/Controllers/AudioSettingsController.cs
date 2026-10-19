using UnityEngine;

// Attach To Controller_AudioSettings GameObject
public class AudioSettingsController : MonoBehaviour
{
    // Settings Manager Reference
    [SerializeField] private SettingsManager _SettingsManager;

    // Reference To The Sliders & Toggles
    // Master
    [SerializeField] private SegmentedSlider _MasterSlider;
    [SerializeField] private StateToggle _MasterMuteToggle;
    [SerializeField] private AudioSliderFeedback _MasterSliderFeedback;

    // Music

    [SerializeField] private SegmentedSlider _MusicSlider;
    [SerializeField] private StateToggle _MusicMuteToggle;
    [SerializeField] private AudioSliderFeedback _MusicSliderFeedback;
    // SFX
    [SerializeField] private SegmentedSlider _SFXSlider;
    [SerializeField] private StateToggle _SFXMuteToggle;
    [SerializeField] private AudioSliderFeedback _SFXSliderFeedback;

    private void OnEnable()
    {
        // Subscribers To Slider & Toggle Events

        _MasterSlider.SliderUpdated += OnMasterVolumeChanged;
        _MasterMuteToggle.UserToggled += OnMasterMuteChanged;

        _MusicSlider.SliderUpdated += OnMusicVolumeChanged;
        _MusicMuteToggle.UserToggled += OnMusicMuteChanged;

        _SFXSlider.SliderUpdated += OnSFXVolumeChanged;
        _SFXMuteToggle.UserToggled += OnSFXMuteChanged;
    }

    private void OnDisable()
    {
        // Unsubscribers From Events

        _MasterSlider.SliderUpdated -= OnMasterVolumeChanged;
        _MasterMuteToggle.UserToggled -= OnMasterMuteChanged;

        _MusicSlider.SliderUpdated -= OnMusicVolumeChanged;
        _MusicMuteToggle.UserToggled -= OnMusicMuteChanged;

        _SFXSlider.SliderUpdated -= OnSFXVolumeChanged;
        _SFXMuteToggle.UserToggled -= OnSFXMuteChanged;
    }

    private void Start()
    {
        // Sync On Start To Ensure UI Reflects Current Settings
        SyncFromSettings();
    }

    // Synchronize UI With Current Settings
    private void SyncFromSettings()
    {
        // SettingsManager stores volume as 0.1 - 1.0.
        // SegmentedSlider uses 10 - 100.

        int MasterValue =
            Mathf.RoundToInt(_SettingsManager.MasterVolume * 100f);

        int MusicValue =
            Mathf.RoundToInt(_SettingsManager.MusicVolume * 100f);

        int SFXValue =
            Mathf.RoundToInt(_SettingsManager.SFXVolume * 100f);

        // Set EXISTING sliders without firing SliderUpdated.
        _MasterSlider.IntializeSliderValue(MasterValue);
        _MusicSlider.IntializeSliderValue(MusicValue);
        _SFXSlider.IntializeSliderValue(SFXValue);

        // Set EXISTING toggle state without firing ToggleChanged.
        _MasterMuteToggle.IntitializeStateToggle(_SettingsManager.MasterMuted);
        _MusicMuteToggle.IntitializeStateToggle(_SettingsManager.MusicMuted);
        _SFXMuteToggle.IntitializeStateToggle(_SettingsManager.SFXMuted);
    }

    // Master
    private void OnMasterVolumeChanged(int SliderValue)
    {
        // Cconvert Slider Value To Normalized Volume 
        float NormalizedVolume = SliderValue / 100f;
        // Call EXISTING SettingsManager method.
        _SettingsManager.SetMasterVolume(NormalizedVolume);
        // Play Audio 
        _MasterSliderFeedback.PlaySliderAdjustAudio();
    }

    private void OnMasterMuteChanged(bool IsMuted)
    {
        // IsMuted comes from the EXISTING StateToggle event.
        _SettingsManager.SetMasterMuted(IsMuted);
    }

    // Music
    private void OnMusicVolumeChanged(int SliderValue)
    {
        float NormalizedVolume = SliderValue / 100f;
        _SettingsManager.SetMusicVolume(NormalizedVolume);
        _MusicSliderFeedback.PlaySliderAdjustAudio();
    }


    private void OnMusicMuteChanged(bool IsMuted)
    {
        _SettingsManager.SetMusicMuted(IsMuted);
    }

    // SFX
    private void OnSFXVolumeChanged(int SliderValue)
    {
        float NormalizedVolume = SliderValue / 100f;

        _SettingsManager.SetSFXVolume(NormalizedVolume);
        _SFXSliderFeedback.PlaySliderAdjustAudio();
    }


    private void OnSFXMuteChanged(bool IsMuted)
    {
        _SettingsManager.SetSFXMuted(IsMuted);
    }
}
