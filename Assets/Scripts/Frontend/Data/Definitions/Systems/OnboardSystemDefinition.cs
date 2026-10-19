using UnityEngine;

// ScriptableObject Base Definition - Not Attached To A GameObject
// Provides Shared Data And Operational Requirements For All Onboard Systems
public abstract class OnboardSystemDefinition : ScriptableObject
{
    // Display Name Of The Onboard System
    [SerializeField] private string _SystemName;

    // Unique ID Used To Identify The Onboard System
    [SerializeField] private string _SystemID;

    // Manufacturer Of The Onboard System
    [ScriptableObjectPicker]
    [SerializeField] private Manufacturers _Manufacturer;

    // Universal Tier Assigned To The Onboard System
    [ScriptableObjectPicker]
    [SerializeField] private TierDefinition _Tier;

    // Type / Category Of This Onboard System
    [ScriptableObjectPicker]
    [SerializeField] private SystemTypeDefinition _SystemType;


    // Domains In Which This System Is Allowed To Operate
    // Empty Means The System Has No Domain Restriction
    [ScriptableObjectPicker]
    [SerializeField] private Domains[] _UsableDomains;

    // Gameplay States That Must Be Present For This System To Operate
    // Empty Means The System Has No Required Tags
    [SerializeField] private GameplayTag[] _RequiredTags;

    // Gameplay States That Prevent This System From Operating
    // Empty Means The System Has No Blocked Tags
    [SerializeField] private GameplayTag[] _BlockedTags;


    // Read-Only Access For Other Systems
    public string SystemName
    {
        get { return _SystemName; }
    }

    public string SystemID
    {
        get { return _SystemID; }
    }

    public Manufacturers Manufacturer
    {
        get { return _Manufacturer; }
    }

    public TierDefinition Tier
    {
        get { return _Tier; }
    }

    public SystemTypeDefinition SystemType
    {
        get { return _SystemType; }
    }

    public Domains[] UsableDomains
    {
        get { return _UsableDomains; }
    }

    public GameplayTag[] RequiredTags
    {
        get { return _RequiredTags; }
    }

    public GameplayTag[] BlockedTags
    {
        get { return _BlockedTags; }
    }
}