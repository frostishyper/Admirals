using System.Collections.Generic;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Innate Ability That Continuously Provides Modifiers
[CreateAssetMenu(
    fileName = "PassiveAbility_New",
    menuName = "Admirals/Passive Ability Definition"
)]
public class PassiveAbilityDefinition : AbilityDefinition
{
    // Determines Which Units Receive This Ability's Modifiers
    [SerializeField] private EffectTargetType _TargetType;

    // Optional Pattern Used To Limit The Area Affected
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _TargetPattern;

    // Continuous Modifiers Provided While This Ability Is Operational
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