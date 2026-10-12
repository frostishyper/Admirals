using System;
using System.Collections.Generic;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemSearchCapability
{
    [SerializeField]
    private bool _Enabled;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemFireControlCapability
{
    [SerializeField]
    private bool _Enabled;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemPassiveCapability
{
    [SerializeField]
    private bool _Enabled;

    [SerializeField]
    private GameplayTarget _Targets;

    [SerializeField]
    private GameplayModifier[] _Modifiers;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }


    public GameplayTarget Targets
    {
        get
        {
            return _Targets;
        }
    }


    public GameplayModifier[] Modifiers
    {
        get
        {
            return _Modifiers;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!_Enabled)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_Targets ==
            GameplayTarget.None)
        {
            ValidationMessage =
                "Passive Capability has no Targets.";

            return false;
        }


        if (_Modifiers == null ||
            _Modifiers.Length == 0)
        {
            ValidationMessage =
                "Passive Capability has no Modifiers.";

            return false;
        }


        for (
            int i = 0;
            i < _Modifiers.Length;
            i++
        )
        {
            if (_Modifiers[i] == null)
            {
                ValidationMessage =
                    $"Passive Modifier {i} is empty.";

                return false;
            }


            if (!_Modifiers[i].Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class DefenseOutcome
{
    [SerializeField]
    private string _Name;

    [SerializeField]
    private int _AttackRollModifier;

    [SerializeField]
    private int _DamageModifier;

    [SerializeField]
    private bool _NegatesAttack;

    [SerializeField]
    private bool _StripGuaranteedHit;

    [SerializeField]
    private bool _StripBypassDefense;

    [SerializeField]
    private GameplayAction[] _Actions;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public int AttackRollModifier
    {
        get
        {
            return _AttackRollModifier;
        }
    }


    public int DamageModifier
    {
        get
        {
            return _DamageModifier;
        }
    }


    public bool NegatesAttack
    {
        get
        {
            return _NegatesAttack;
        }
    }


    public bool StripGuaranteedHit
    {
        get
        {
            return _StripGuaranteedHit;
        }
    }


    public bool StripBypassDefense
    {
        get
        {
            return _StripBypassDefense;
        }
    }


    public GameplayAction[] Actions
    {
        get
        {
            return _Actions;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Defense Outcome Name is empty.";

            return false;
        }


        if (_Actions != null)
        {
            for (
                int i = 0;
                i < _Actions.Length;
                i++
            )
            {
                if (_Actions[i] == null)
                {
                    ValidationMessage =
                        $"Defense Outcome '{_Name}' has an empty Action.";

                    return false;
                }


                if (!_Actions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class DefenseD20Outcome :
    D20RollRange
{
    [SerializeField]
    private DefenseOutcome _Outcome;


    public DefenseOutcome Outcome
    {
        get
        {
            return _Outcome;
        }
    }


    // Validates both this D20 range and its Defense-specific Outcome.
    public bool ValidateOutcome(
        out string ValidationMessage
    )
    {
        if (!ValidateRange(
                out ValidationMessage))
        {
            return false;
        }


        if (_Outcome == null)
        {
            ValidationMessage =
                "Defense D20 Outcome has no Outcome.";

            return false;
        }


        if (!_Outcome.Validate(
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemDefenseCapability
{
    [SerializeField]
    private bool _Enabled;

    [SerializeField]
    private bool _CanDefendSelf;

    [SerializeField]
    private bool _CanDefendAllies;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_WeaponType[] _DefendableWeaponTypes;

    [SerializeField]
    private bool _UsesD20Roll;

    [SerializeField]
    private DefenseOutcome _DefenseOutcome;

    [SerializeField]
    private DefenseD20Outcome[] _D20Outcomes;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }


    public bool CanDefendSelf
    {
        get
        {
            return _CanDefendSelf;
        }
    }


    public bool CanDefendAllies
    {
        get
        {
            return _CanDefendAllies;
        }
    }


    public Definition_WeaponType[] DefendableWeaponTypes
    {
        get
        {
            return _DefendableWeaponTypes;
        }
    }


    public bool UsesD20Roll
    {
        get
        {
            return _UsesD20Roll;
        }
    }


    public DefenseOutcome DefenseOutcome
    {
        get
        {
            return _DefenseOutcome;
        }
    }


    public DefenseD20Outcome[] D20Outcomes
    {
        get
        {
            return _D20Outcomes;
        }
    }


    // A Weapon is defendable when any of its Weapon Types
    // matches one supported by this defensive System.
    public bool CanDefendAgainst(
        Definition_Weapon IncomingWeapon
    )
    {
        if (!_Enabled ||
            IncomingWeapon == null ||
            _DefendableWeaponTypes == null ||
            IncomingWeapon.Types == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _DefendableWeaponTypes.Length;
            i++
        )
        {
            Definition_WeaponType DefendableType =
                _DefendableWeaponTypes[i];


            if (DefendableType == null)
            {
                continue;
            }


            if (IncomingWeapon.HasWeaponType(
                    DefendableType))
            {
                return true;
            }
        }


        return false;
    }


    public bool TryGetDefenseOutcome(
        int RawRoll,
        out DefenseOutcome FoundOutcome,
        int RollModifier = 0
    )
    {
        FoundOutcome =
            null;


        if (!_Enabled)
        {
            return false;
        }


        if (!_UsesD20Roll)
        {
            if (_DefenseOutcome == null)
            {
                return false;
            }


            FoundOutcome =
                _DefenseOutcome;

            return true;
        }


        if (!D20Roll.TryGetOutcome(
                _D20Outcomes,
                RawRoll,
                out DefenseD20Outcome D20Outcome,
                RollModifier))
        {
            return false;
        }


        if (D20Outcome == null ||
            D20Outcome.Outcome == null)
        {
            return false;
        }


        FoundOutcome =
            D20Outcome.Outcome;

        return true;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!_Enabled)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (!_CanDefendSelf &&
            !_CanDefendAllies)
        {
            ValidationMessage =
                "Defense Capability cannot defend Self or Allies.";

            return false;
        }


        if (_DefendableWeaponTypes == null ||
            _DefendableWeaponTypes.Length == 0)
        {
            ValidationMessage =
                "Defense Capability has no Defendable Weapon Types.";

            return false;
        }


        HashSet<Definition_WeaponType> UsedWeaponTypes =
            new HashSet<Definition_WeaponType>();


        for (
            int i = 0;
            i < _DefendableWeaponTypes.Length;
            i++
        )
        {
            Definition_WeaponType WeaponType =
                _DefendableWeaponTypes[i];


            if (WeaponType == null)
            {
                ValidationMessage =
                    $"Defendable Weapon Type {i} is empty.";

                return false;
            }


            if (!UsedWeaponTypes.Add(
                    WeaponType))
            {
                ValidationMessage =
                    $"Defendable Weapon Type '{WeaponType.name}' is assigned more than once.";

                return false;
            }
        }


        if (_UsesD20Roll)
        {
            if (_D20Outcomes == null ||
                _D20Outcomes.Length == 0)
            {
                ValidationMessage =
                    "Defense Capability uses a D20 Roll but has no D20 Outcomes.";

                return false;
            }


            if (!D20Roll.Validate(
                    _D20Outcomes,
                    out ValidationMessage))
            {
                return false;
            }


            for (
                int i = 0;
                i < _D20Outcomes.Length;
                i++
            )
            {
                if (_D20Outcomes[i] == null)
                {
                    ValidationMessage =
                        $"Defense D20 Outcome {i} is empty.";

                    return false;
                }


                if (!_D20Outcomes[i].ValidateOutcome(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }
        else
        {
            if (_DefenseOutcome == null)
            {
                ValidationMessage =
                    "Defense Capability has no Defense Outcome.";

                return false;
            }


            if (!_DefenseOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemUnitDeploymentCapability
{
    [SerializeField]
    private bool _Enabled;

    [Min(0)]
    [SerializeField]
    private int _CarrySlots;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_UnitType[] _EligibleUnitTypes;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }


    public int CarrySlots
    {
        get
        {
            return _CarrySlots;
        }
    }


    public Definition_UnitType[] EligibleUnitTypes
    {
        get
        {
            return _EligibleUnitTypes;
        }
    }


    public bool IsEligibleFor(
        Definition_UnitType[] UnitTypes
    )
    {
        if (!_Enabled ||
            UnitTypes == null ||
            _EligibleUnitTypes == null)
        {
            return false;
        }


        for (
            int UnitIndex = 0;
            UnitIndex < UnitTypes.Length;
            UnitIndex++
        )
        {
            for (
                int EligibleIndex = 0;
                EligibleIndex < _EligibleUnitTypes.Length;
                EligibleIndex++
            )
            {
                if (UnitTypes[UnitIndex] != null &&
                    UnitTypes[UnitIndex] ==
                    _EligibleUnitTypes[EligibleIndex])
                {
                    return true;
                }
            }
        }


        return false;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!_Enabled)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_CarrySlots < 1)
        {
            ValidationMessage =
                "Unit Deployment Capability has no Carry Slots.";

            return false;
        }


        if (_EligibleUnitTypes == null ||
            _EligibleUnitTypes.Length == 0)
        {
            ValidationMessage =
                "Unit Deployment Capability has no Eligible Unit Types.";

            return false;
        }


        HashSet<Definition_UnitType> UsedUnitTypes =
            new HashSet<Definition_UnitType>();


        for (
            int i = 0;
            i < _EligibleUnitTypes.Length;
            i++
        )
        {
            if (_EligibleUnitTypes[i] == null)
            {
                ValidationMessage =
                    $"Unit Deployment Eligible Unit Type {i} is empty.";

                return false;
            }


            if (!UsedUnitTypes.Add(
                    _EligibleUnitTypes[i]))
            {
                ValidationMessage =
                    $"Unit Deployment Eligible Unit Type '{_EligibleUnitTypes[i].name}' is assigned more than once.";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class MineOutcome
{
    [SerializeField]
    private string _Name;

    [Min(0)]
    [SerializeField]
    private int _Damage;

    [SerializeField]
    private GameplayAction[] _Actions;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public int Damage
    {
        get
        {
            return _Damage;
        }
    }


    public GameplayAction[] Actions
    {
        get
        {
            return _Actions;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Mine Outcome Name is empty.";

            return false;
        }


        if (_Damage < 0)
        {
            ValidationMessage =
                $"Mine Outcome '{_Name}' has invalid Damage.";

            return false;
        }


        if (_Actions != null)
        {
            for (
                int i = 0;
                i < _Actions.Length;
                i++
            )
            {
                if (_Actions[i] == null)
                {
                    ValidationMessage =
                        $"Mine Outcome '{_Name}' has an empty Action.";

                    return false;
                }


                if (!_Actions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class MineD20Outcome :
    D20RollRange
{
    [SerializeField]
    private MineOutcome _Outcome;


    public MineOutcome Outcome
    {
        get
        {
            return _Outcome;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!ValidateRange(
                out ValidationMessage))
        {
            return false;
        }


        if (_Outcome == null)
        {
            ValidationMessage =
                "Mine D20 Outcome has no Outcome.";

            return false;
        }


        if (!_Outcome.Validate(
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class SystemMineDeploymentCapability
{
    [SerializeField]
    private bool _Enabled;

    [SerializeField]
    private Domain _TriggerDomains =
        Domain.Surface |
        Domain.Subsurface;

    [SerializeField]
    private bool _UsesD20Roll;

    [SerializeField]
    private MineOutcome _MineOutcome;

    [SerializeField]
    private MineD20Outcome[] _D20Outcomes;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }


    public Domain TriggerDomains
    {
        get
        {
            return _TriggerDomains;
        }
    }


    public bool UsesD20Roll
    {
        get
        {
            return _UsesD20Roll;
        }
    }


    public MineOutcome MineOutcome
    {
        get
        {
            return _MineOutcome;
        }
    }


    public MineD20Outcome[] D20Outcomes
    {
        get
        {
            return _D20Outcomes;
        }
    }


    public bool TryGetMineOutcome(
        int RawRoll,
        out MineOutcome FoundOutcome,
        int RollModifier = 0
    )
    {
        FoundOutcome =
            null;


        if (!_Enabled)
        {
            return false;
        }


        if (!_UsesD20Roll)
        {
            if (_MineOutcome == null)
            {
                return false;
            }


            FoundOutcome =
                _MineOutcome;

            return true;
        }


        if (!D20Roll.TryGetOutcome(
                _D20Outcomes,
                RawRoll,
                out MineD20Outcome D20Outcome,
                RollModifier))
        {
            return false;
        }


        if (D20Outcome == null ||
            D20Outcome.Outcome == null)
        {
            return false;
        }


        FoundOutcome =
            D20Outcome.Outcome;

        return true;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!_Enabled)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_TriggerDomains ==
            Domain.None)
        {
            ValidationMessage =
                "Mine Deployment Capability has no Trigger Domains.";

            return false;
        }


        if (_UsesD20Roll)
        {
            if (_D20Outcomes == null ||
                _D20Outcomes.Length == 0)
            {
                ValidationMessage =
                    "Mine Deployment Capability uses a D20 Roll but has no D20 Outcomes.";

                return false;
            }


            if (!D20Roll.Validate(
                    _D20Outcomes,
                    out ValidationMessage))
            {
                return false;
            }


            for (
                int i = 0;
                i < _D20Outcomes.Length;
                i++
            )
            {
                if (_D20Outcomes[i] == null)
                {
                    ValidationMessage =
                        $"Mine D20 Outcome {i} is empty.";

                    return false;
                }


                if (!_D20Outcomes[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }
        else
        {
            if (_MineOutcome == null)
            {
                ValidationMessage =
                    "Mine Deployment Capability has no Mine Outcome.";

                return false;
            }


            if (!_MineOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Reusable System.
[CreateAssetMenu(
    fileName = "System_New",
    menuName = "Admirals/System"
)]
public class Definition_System :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Manufacturer _Manufacturer;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_SystemType _Type;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Tier _Tier;


    [Header("Compatibility")]
    [SerializeField]
    private SystemSlotType _Slot;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_UnitType[] _EligibleUnitTypes;


    [Header("Operation")]
    [SerializeField]
    private Domain _Domains;

    [SerializeField]
    private GameplayPhase _OperationalPhases;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Pattern _Pattern;


    [Header("Charges")]
    [SerializeField]
    private bool _UsesCharges;

    [Min(0)]
    [SerializeField]
    private int _ReplenishRate;

    [Min(0)]
    [SerializeField]
    private int _ReplenishInterval;


    [Header("Capabilities")]
    [SerializeField]
    private SystemSearchCapability _Search;

    [SerializeField]
    private SystemFireControlCapability _FireControl;

    [SerializeField]
    private SystemPassiveCapability _Passive;

    [SerializeField]
    private SystemDefenseCapability _Defense;

    [SerializeField]
    private SystemUnitDeploymentCapability _UnitDeployment;

    [SerializeField]
    private SystemMineDeploymentCapability _MineDeployment;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public string ID
    {
        get
        {
            return _ID;
        }
    }


    public Definition_Manufacturer Manufacturer
    {
        get
        {
            return _Manufacturer;
        }
    }


    public Definition_SystemType Type
    {
        get
        {
            return _Type;
        }
    }


    public Definition_Tier Tier
    {
        get
        {
            return _Tier;
        }
    }


    public SystemSlotType Slot
    {
        get
        {
            return _Slot;
        }
    }


    public Definition_UnitType[] EligibleUnitTypes
    {
        get
        {
            return _EligibleUnitTypes;
        }
    }


    public Domain Domains
    {
        get
        {
            return _Domains;
        }
    }


    public GameplayPhase OperationalPhases
    {
        get
        {
            return _OperationalPhases;
        }
    }


    public Definition_Pattern Pattern
    {
        get
        {
            return _Pattern;
        }
    }


    public bool UsesCharges
    {
        get
        {
            return _UsesCharges;
        }
    }


    public int ReplenishRate
    {
        get
        {
            return _ReplenishRate;
        }
    }


    public int ReplenishInterval
    {
        get
        {
            return _ReplenishInterval;
        }
    }


    public SystemSearchCapability Search
    {
        get
        {
            return _Search;
        }
    }


    public SystemFireControlCapability FireControl
    {
        get
        {
            return _FireControl;
        }
    }


    public SystemPassiveCapability Passive
    {
        get
        {
            return _Passive;
        }
    }


    public SystemDefenseCapability Defense
    {
        get
        {
            return _Defense;
        }
    }


    public SystemUnitDeploymentCapability UnitDeployment
    {
        get
        {
            return _UnitDeployment;
        }
    }


    public SystemMineDeploymentCapability MineDeployment
    {
        get
        {
            return _MineDeployment;
        }
    }


    public bool CanDefendAgainst(
        Definition_Weapon IncomingWeapon
    )
    {
        if (_Defense == null)
        {
            return false;
        }


        return
            _Defense.CanDefendAgainst(
                IncomingWeapon
            );
    }


    public bool IsEligibleFor(
        Definition_UnitType[] UnitTypes
    )
    {
        if (UnitTypes == null ||
            _EligibleUnitTypes == null)
        {
            return false;
        }


        for (
            int UnitIndex = 0;
            UnitIndex < UnitTypes.Length;
            UnitIndex++
        )
        {
            Definition_UnitType UnitType =
                UnitTypes[UnitIndex];


            if (UnitType == null)
            {
                continue;
            }


            for (
                int EligibleIndex = 0;
                EligibleIndex < _EligibleUnitTypes.Length;
                EligibleIndex++
            )
            {
                if (_EligibleUnitTypes[EligibleIndex] ==
                    UnitType)
                {
                    return true;
                }
            }
        }


        return false;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "System Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "System ID is empty.";

            return false;
        }


        if (_Manufacturer == null)
        {
            ValidationMessage =
                "System has no Manufacturer.";

            return false;
        }


        if (_Type == null)
        {
            ValidationMessage =
                "System has no System Type.";

            return false;
        }


        if (_Tier == null)
        {
            ValidationMessage =
                "System has no Tier.";

            return false;
        }


        if (_Slot ==
            SystemSlotType.None)
        {
            ValidationMessage =
                "System has no Slot Type.";

            return false;
        }


        if (_EligibleUnitTypes == null ||
            _EligibleUnitTypes.Length == 0)
        {
            ValidationMessage =
                "System has no Eligible Unit Types.";

            return false;
        }


        HashSet<Definition_UnitType> UsedUnitTypes =
            new HashSet<Definition_UnitType>();


        for (
            int i = 0;
            i < _EligibleUnitTypes.Length;
            i++
        )
        {
            if (_EligibleUnitTypes[i] == null)
            {
                ValidationMessage =
                    $"Eligible Unit Type {i} is empty.";

                return false;
            }


            if (!UsedUnitTypes.Add(
                    _EligibleUnitTypes[i]))
            {
                ValidationMessage =
                    $"Eligible Unit Type '{_EligibleUnitTypes[i].name}' is assigned more than once.";

                return false;
            }
        }


        if (_Domains ==
            Domain.None)
        {
            ValidationMessage =
                "System has no Operational Domains.";

            return false;
        }


        if (_OperationalPhases ==
            GameplayPhase.None)
        {
            ValidationMessage =
                "System has no Operational Phases.";

            return false;
        }


        if (_UsesCharges)
        {
            if ((_ReplenishRate == 0) !=
                (_ReplenishInterval == 0))
            {
                ValidationMessage =
                    "System Replenish Rate and Replenish Interval must either both be configured or both be zero.";

                return false;
            }
        }
        else
        {
            if (_ReplenishRate != 0 ||
                _ReplenishInterval != 0)
            {
                ValidationMessage =
                    "System does not use Charges but has Replenishment configured.";

                return false;
            }
        }


        if (_Passive != null &&
            !_Passive.Validate(
                out ValidationMessage))
        {
            return false;
        }


        if (_Defense != null &&
            !_Defense.Validate(
                out ValidationMessage))
        {
            return false;
        }


        if (_UnitDeployment != null &&
            !_UnitDeployment.Validate(
                out ValidationMessage))
        {
            return false;
        }


        if (_MineDeployment != null &&
            !_MineDeployment.Validate(
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid System '{name}': {ValidationMessage}",
                this
            );
        }
    }
}