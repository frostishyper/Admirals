using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Ability Used As A Defensive Response During Combat
[CreateAssetMenu(
    fileName = "DefensiveAbility_New",
    menuName = "Admirals/Defensive Ability Definition"
)]
public class DefensiveAbilityDefinition : AbilityDefinition
{
    // Gameplay Phases During Which This Ability Can Be Used
    [SerializeField] private TurnPhase[] _UsablePhases;

    // Whether This Ability Can Defend Its Owning Unit
    [SerializeField] private bool _CanDefendSelf = true;

    // Whether This Ability Can Defend Friendly Units
    [SerializeField] private bool _CanDefendAllies;

    // Optional Coverage Pattern Used When Defending Friendly Units
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _DefensePattern;

    // Allows This Ability To Respond Even When The Incoming Attack
    // Carries The BypassDefenses Property
    [SerializeField] private bool _CanRespondToDefenseBypass;

    // Defensive D20 Outcomes Supplied To Combat Resolution
    [SerializeField] private DefenseRollRange[] _RollRanges;


    public TurnPhase[] UsablePhases
    {
        get { return _UsablePhases; }
    }

    public bool CanDefendSelf
    {
        get { return _CanDefendSelf; }
    }

    public bool CanDefendAllies
    {
        get { return _CanDefendAllies; }
    }

    public PatternDefinition DefensePattern
    {
        get { return _DefensePattern; }
    }

    public bool CanRespondToDefenseBypass
    {
        get { return _CanRespondToDefenseBypass; }
    }

    public DefenseRollRange[] RollRanges
    {
        get { return _RollRanges; }
    }
}