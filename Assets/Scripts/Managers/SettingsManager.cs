using UnityEngine;
using UnityEngine.Audio;

// Handles Settings Data 
public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer MainMixer;

    // Ensures Doesnt Get Destroyed On Scene Change & Settings Persist
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

        int fullscreenState = PlayerPrefs.GetInt("FullscreenData", 1);
        Screen.fullScreen = (fullscreenState == 1);

        float master = PlayerPrefs.GetFloat("MasterVolData", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolData", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolData", 1f);

        UpdateMixer("MasterVol", master);
        UpdateMixer("MusicVol", music);
        UpdateMixer("SFXVol", sfx);
    }

    // FullScreen
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("FullscreenData", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // Audio Thingz
    public void SetMasterVolume(float vol)
    {
        UpdateMixer("MasterVol", vol);
        PlayerPrefs.SetFloat("MasterVolData", vol);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float vol)
    {
        UpdateMixer("MusicVol", vol);
        PlayerPrefs.SetFloat("MusicVolData", vol);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float vol)
    {
        UpdateMixer("SFXVol", vol);
        PlayerPrefs.SetFloat("SFXVolData", vol);
        PlayerPrefs.Save();
    }

    private void UpdateMixer(string paramName, float vol)
    {
        float clamped = Mathf.Clamp(vol, 0.0001f, 1f);
        float decibels = Mathf.Log10(clamped) * 20f;
        MainMixer.SetFloat(paramName, decibels);
    }
}