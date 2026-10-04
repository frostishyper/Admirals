using UnityEngine;
using UnityEngine.Audio;

// Handles Settings Data 
public class SettingsManager : MonoBehaviour
{
    // Get The Mixer
    [SerializeField] private AudioMixer MainMixer;

    // Runtime Volume Values
    private float _MasterVolume;
    private float _MusicVolume;
    private float _SFXVolume;

    // Runtime Mute States
    private bool _MasterMuted;
    private bool _MusicMuted;
    private bool _SFXMuted;

    // Read Only Access To Audio Values
    // Read-Only Access To Current Audio Settings
    public float MasterVolume { get { return _MasterVolume; } }
    public float MusicVolume { get { return _MusicVolume; } }
    public float SFXVolume { get { return _SFXVolume; } }

    public bool MasterMuted { get { return _MasterMuted; } }
    public bool MusicMuted { get { return _MusicMuted; } }
    public bool SFXMuted { get { return _SFXMuted; } }

    // Runtime Fullscreen State
    private bool _Fullscreen;

    // Read-Only Access To Fullscreen State
    public bool Fullscreen
    {
        get { return _Fullscreen; }
    }

    // Ensures Doesnt Get Destroyed On Scene Change & Settings Persist
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

        int fullscreenState = PlayerPrefs.GetInt("FullscreenData", 1);

        _Fullscreen = (fullscreenState == 1);
        Screen.fullScreen = _Fullscreen;

        // Load Saved Values
        _MasterVolume = PlayerPrefs.GetFloat("MasterVolData", 1f);
        _MusicVolume = PlayerPrefs.GetFloat("MusicVolData", 1f);
        _SFXVolume = PlayerPrefs.GetFloat("SFXVolData", 1f);

        // Load Saved Mute States (1 = Muted, 0 = Unmuted)
        _MasterMuted = PlayerPrefs.GetInt("MasterMuteData", 0) == 1;
        _MusicMuted = PlayerPrefs.GetInt("MusicMuteData", 0) == 1;
        _SFXMuted = PlayerPrefs.GetInt("SFXMuteData", 0) == 1;

        // Apply Audio Settings
        UpdateMixer("MasterVol", _MasterVolume, _MasterMuted);
        UpdateMixer("MusicVol", _MusicVolume, _MusicMuted);
        UpdateMixer("SFXVol", _SFXVolume, _SFXMuted);
    }

    // Set FullScreen
    public void SetFullscreen(bool IsFullscreen)
    {
        _Fullscreen = IsFullscreen;
        
        Screen.fullScreen = _Fullscreen;

        PlayerPrefs.SetInt("FullscreenData", _Fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // Audio Thingz
    public void SetMasterVolume(float vol)
    {
        // Keep Runtime Value Updated
        _MasterVolume = vol;
        // Apply Volume Even If Muted
        UpdateMixer("MasterVol", _MasterVolume, _MasterMuted);
        PlayerPrefs.SetFloat("MasterVolData", _MasterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float vol)
    {
        _MusicVolume = vol;
        UpdateMixer("MusicVol", _MusicVolume, _MusicMuted);
        PlayerPrefs.SetFloat("MusicVolData", _MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float vol)
    {
        _SFXVolume = vol;
        UpdateMixer("SFXVol", _SFXVolume, _SFXMuted);
        PlayerPrefs.SetFloat("SFXVolData", _SFXVolume);
        PlayerPrefs.Save();
    }

    // Muters
    public void SetMasterMuted(bool IsMuted) // Master
    {
        _MasterMuted = IsMuted;

        // Reapply Existing Volume With New Mute State
        UpdateMixer("MasterVol", _MasterVolume, _MasterMuted);

        PlayerPrefs.SetInt("MasterMuteData", _MasterMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicMuted(bool IsMuted) // Music
    {
        _MusicMuted = IsMuted;

        UpdateMixer("MusicVol", _MusicVolume, _MusicMuted);

        PlayerPrefs.SetInt("MusicMuteData", _MusicMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFXMuted(bool IsMuted) // SFX
    {
        _SFXMuted = IsMuted;

        UpdateMixer("SFXVol", _SFXVolume, _SFXMuted);

        PlayerPrefs.SetInt("SFXMuteData", _SFXMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void UpdateMixer(string paramName, float vol, bool IsMuted)
    {
        float EffectiveVolume = IsMuted ? 0.0001f : vol;
        float clamped = Mathf.Clamp(EffectiveVolume, 0.0001f, 1f);
        float decibels = Mathf.Log10(clamped) * 20f;
        MainMixer.SetFloat(paramName, decibels);
    }
}