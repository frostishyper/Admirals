using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Ability Explicitly Activated By A Unit
[CreateAssetMenu(
    fileName = "ActiveAbility_New",
    menuName = "Admirals/Active Ability Definition"
)]
public class ActiveAbilityDefinition : AbilityDefinition
{
    // Determines What Kind Of Unit This Ability Can Target
    [SerializeField] private EffectTargetType _TargetType;

    // Optional Pattern Used To Determine Targeting Range
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _TargetPattern;

    // Gameplay Phases During Which This Ability Can Be Activated
    [SerializeField] private TurnPhase[] _UsablePhases;

    // Determines Whether The Ability Is Automatic,
    // Percentage-Based, Or Uses A D20
    [SerializeField] private AbilityResolutionType _ResolutionType;

    // Used Only By Percentage Resolution
    [SerializeField]
    [Range(0f, 100f)]
    private float _SuccessChance = 100f;

    // Used By Automatic And Percentage Resolution
    [SerializeField] private EffectApplication[] _EffectApplications;

    // Used Only By D20 Resolution
    [SerializeField] private AbilityRollRange[] _RollRanges;


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

    public AbilityResolutionType ResolutionType
    {
        get { return _ResolutionType; }
    }

    public float SuccessChance
    {
        get { return _SuccessChance; }
    }

    public EffectApplication[] EffectApplications
    {
        get { return _EffectApplications; }
    }

    public AbilityRollRange[] RollRanges
    {
        get { return _RollRanges; }
    }
}