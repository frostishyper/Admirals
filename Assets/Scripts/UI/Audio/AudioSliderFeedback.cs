using UnityEngine;

// Attach To Controller_AudioSliderFeedback GameObject
// Only Holds The Clip & Channel To Play The Audio Through, And Calls The UIAudio Manager To Play It
public class AudioSliderFeedback : MonoBehaviour
{
    // Reference To The UIAudio Manager
    [SerializeField] private UIAudioManager _UIAudio;

    // Which Mixer-Routed Channel The Audio Should Play Through
    [SerializeField] private AudioChannel _Channel;

    // Audio Clip(s) To Play
    [SerializeField] private AudioClip _SliderAdjustAudio;
    
    public void PlaySliderAdjustAudio()
    {
        if (_SliderAdjustAudio == null)
        {
            return; // No Assigned Clip No Audio Plays
        }
        // Play The Audio Through The UIAudio Manager
        _UIAudio.PlayOnce(_SliderAdjustAudio, _Channel);
    }
}
