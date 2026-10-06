using UnityEngine;

// ScriptableObject Base Definition - Not Attached To A GameObject
// Provides Shared Data For All Onboard Systems
public abstract class OnboardSystemDefinition : ScriptableObject
{
    // Display Name Of The Onboard System
    [SerializeField] private string _SystemName;

    // Unique ID Used To Identify The Onboard System
    [SerializeField] private string _SystemID;

    // Manufacturer Of The Onboard System
    [SerializeField] private string _Manufacturer;

    // Universal Tier Assigned To The Onboard System
    [SerializeField] private TierDefinition _Tier;


    // Read-Only Access For Other Systems
    public string SystemName
    {
        get { return _SystemName; }
    }

    public string SystemID
    {
        get { return _SystemID; }
    }

    public string Manufacturer
    {
        get { return _Manufacturer; }
    }

    public TierDefinition Tier
    {
        get { return _Tier; }
    }
}