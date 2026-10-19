using System.Collections.Generic;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard System That Continuously Provides Modifiers While Operational
[CreateAssetMenu(
    fileName = "PassiveSystem_New",
    menuName = "Admirals/Passive System Definition"
)]
public class PassiveSystemDefinition : OnboardSystemDefinition
{
    // Determines Which Units Receive This System's Modifiers
    //
    // Self
    // → Owning Unit Only
    //
    // FriendlyUnit
    // → Owning Unit And All Allied Units
    //
    // EnemyUnit
    // → All Enemy Units
    //
    // AnyUnit
    // → All Units Regardless Of Side
    [SerializeField] private EffectTargetType _TargetType;

    // Optional Pattern Used To Limit The Area Affected By This System
    // Null Means The Passive Has No Pattern-Based Range Requirement
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _TargetPattern;

    // Continuous Modifiers Provided While This System Is Operational
    // Stored Directly Inside This PassiveSystemDefinition Asset
    [SerializeReference]
    [SubclassSelector]
    private List<EffectModifier> _Modifiers = new();


    public EffectTargetType TargetType
    {
        get { return _TargetType; }
    }

    public PatternDefinition TargetPattern
    {
        get { return _TargetPattern; }
    }

    public IReadOnlyList<EffectModifier> Modifiers
    {
        get { return _Modifiers; }
    }
}