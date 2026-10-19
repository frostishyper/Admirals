using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard Sensor That Can Provide Search, Fire Control, Or Both
[CreateAssetMenu(
    fileName = "SensorSystem_New",
    menuName = "Admirals/Sensor System Definition"
)]
public class SensorSystemDefinition : OnboardSystemDefinition
{
    // Tile Pattern Used By This Sensor
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _Pattern;

    // Allows This Sensor To Be Used During The Search Phase
    [SerializeField] private bool _CanSearch;

    // Allows This Sensor To Provide Fire Control During Battle
    [SerializeField] private bool _CanFireControl;

    // Domains This Sensor Can Detect When Used For Search
    [ScriptableObjectPicker]
    [SerializeField] private Domains[] _DetectableDomains;


    // Read-Only Access For Other Systems
    public PatternDefinition Pattern
    {
        get { return _Pattern; }
    }

    public bool CanSearch
    {
        get { return _CanSearch; }
    }

    public bool CanFireControl
    {
        get { return _CanFireControl; }
    }

    public Domains[] DetectableDomains
    {
        get { return _DetectableDomains; }
    }
}