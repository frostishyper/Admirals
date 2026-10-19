using UnityEngine;

// Attach To Controller_UIAudio GameObject
public class UIAudioManager : MonoBehaviour
{
    // References To The Audio Sources (Child GameObjects)
    [SerializeField] private AudioSource _MasterSource; // Master
    [SerializeField] private AudioSource _MusicSource; // Music
    [SerializeField] private AudioSource _SFXSource; // SFX
    
    public void PlayOnce(AudioClip Sound, AudioChannel Channel)
    {
        switch (Channel)
        {
            case AudioChannel.Master:
                _MasterSource.PlayOneShot(Sound);
                break;
            case AudioChannel.Music:
                _MusicSource.PlayOneShot(Sound);
                break;
            case AudioChannel.SFX:
                _SFXSource.PlayOneShot(Sound);
                break;
        }
    }
}
