using System;
using UnityEngine;
using UnityEngine.UI;

public class StateToggle : MonoBehaviour
{
    // BTN That Changes The State Of The Toggle
    [SerializeField] private Button _ToggleBTN;

    // Allow Other To Read Its State
    public bool IsEnabled { get; private set; }

    // State Change Event (Subscribable)
    public event Action<bool> StateChanged;

    // When User Changes The State
    public event Action<bool> UserToggled;

    private void Awake()
    {
        // Add Listener To The Toggle Button
        _ToggleBTN.onClick.AddListener (ToggleState);
    }

    // Intitialize State From Outside
    public void IntitializeStateToggle(bool NewState)
    {
        IsEnabled = NewState;
        // Update Visuals During Initialization
        StateChanged?.Invoke(IsEnabled);
    }

    // Switch The State Of The Toggle (On/Off)
    private void ToggleState()
    {
        // Intilialize The State Of The Toggle
        IntitializeStateToggle(!IsEnabled);
        // State Change Invoked So Notify Subscribers
        UserToggled?.Invoke(IsEnabled);
    }
}
