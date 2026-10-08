using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard System Used To Defend Against Incoming Attacks
[CreateAssetMenu(
    fileName = "DefensiveSystem_New",
    menuName = "Admirals/Defensive System Definition"
)]
public class DefensiveSystemDefinition : OnboardSystemDefinition
{
    // Area In Which This System Can Protect Allied Units
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _DefensePattern;

    // Whether This System Can Protect The Unit It Is Installed On
    [SerializeField] private bool _CanDefendSelf = true;

    // Whether This System Can Protect Other Friendly Units In Range
    [SerializeField] private bool _CanDefendAllies;

    // Incoming Weapon Types This System Is Eligible To Defend Against
    [ScriptableObjectPicker]
    [SerializeField] private WeaponTypeDefinition[] _DefendableWeaponTypes;

    // Allows This System To Respond Even When The Incoming Attack
    // Normally Carries The BypassDefenses Property
    [SerializeField] private bool _CanRespondToDefenseBypass;

    // Turns Required Before Charges Are Replenished
    [SerializeField]
    [Min(0)]
    private int _ReplenishSpeed;

    // Charges Restored Each Time Replenishment Occurs
    [SerializeField]
    [Min(0)]
    private int _ReplenishCount;

    // D20 Outcomes Supplied By This Defensive System
    [SerializeField] private DefenseRollRange[] _RollRanges;


    public PatternDefinition DefensePattern
    {
        get { return _DefensePattern; }
    }

    public bool CanDefendSelf
    {
        get { return _CanDefendSelf; }
    }

    public bool CanDefendAllies
    {
        get { return _CanDefendAllies; }
    }

    public WeaponTypeDefinition[] DefendableWeaponTypes
    {
        get { return _DefendableWeaponTypes; }
    }

    public bool CanRespondToDefenseBypass
    {
        get { return _CanRespondToDefenseBypass; }
    }

    public int ReplenishSpeed
    {
        get { return _ReplenishSpeed; }
    }

    public int ReplenishCount
    {
        get { return _ReplenishCount; }
    }

    public DefenseRollRange[] RollRanges
    {
        get { return _RollRanges; }
    }


    // Returns Whether This System Is Designed To Respond
    // To The Given Weapon Type
    public bool CanDefendAgainst(
        WeaponTypeDefinition WeaponType)
    {
        if (WeaponType == null ||
            _DefendableWeaponTypes == null ||
            _DefendableWeaponTypes.Length == 0)
        {
            return false;
        }


        foreach (WeaponTypeDefinition DefendableType
                in _DefendableWeaponTypes)
        {
            if (DefendableType == WeaponType)
            {
                return true;
            }
        }


        return false;

        
    }
}