using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Active Onboard Utility System That Applies Gameplay Effects
[CreateAssetMenu(
    fileName = "UtilitySystem_New",
    menuName = "Admirals/Utility System Definition"
)]
public class UtilitySystemDefinition : OnboardSystemDefinition
{
    // Determines What Kind Of Unit This System Can Target
    [SerializeField] private EffectTargetType _TargetType;

    // Optional Pattern Used To Determine Targeting Range
    // Null Means This System Has No Pattern-Based Range Requirement
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _TargetPattern;

    // Gameplay Phases During Which This System Can Be Activated
    [SerializeField] private TurnPhase[] _UsablePhases;

    // Effects Applied When This System Successfully Activates
    [ScriptableObjectPicker]
    [SerializeField] private EffectDefinition[] _Effects;


    // Whether Activating This System Consumes Charges
    [SerializeField] private bool _UsesCharges;

    // Whether Consumed Charges Can Return During The Match
    [SerializeField] private bool _CanReplenish;

    // Turns Required Between Charge Replenishments
    // Ignored When CanReplenish Is False
    [SerializeField]
    [Min(1)]
    private int _ReplenishSpeed = 1;

    // Charges Restored Each Time Replenishment Occurs
    // Ignored When CanReplenish Is False
    [SerializeField]
    [Min(1)]
    private int _ReplenishCount = 1;


    public EffectTargetType TargetType
    {
        get { return _TargetType; }
    }

    public PatternDefinition TargetPattern
    {
        get { return _TargetPattern; }
    }

    public TurnPhase[] UsablePhases
    {
        get { return _UsablePhases; }
    }

    public EffectDefinition[] Effects
    {
        get { return _Effects; }
    }

    public bool UsesCharges
    {
        get { return _UsesCharges; }
    }

    public bool CanReplenish
    {
        get { return _CanReplenish; }
    }

    public int ReplenishSpeed
    {
        get { return _ReplenishSpeed; }
    }

    public int ReplenishCount
    {
        get { return _ReplenishCount; }
    }
}