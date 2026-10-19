using System;
using System.Collections.Generic;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class AttackOutcome
{
    [SerializeField]
    private string _Name;

    [SerializeField]
    private OutcomeResult _Result;

    [Min(0)]
    [SerializeField]
    private int _Damage;

    [SerializeField]
    private AttackProperty[] _AttackProperties;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public OutcomeResult Result
    {
        get
        {
            return _Result;
        }
    }


    public int Damage
    {
        get
        {
            return _Damage;
        }
    }


    public AttackProperty[] AttackProperties
    {
        get
        {
            return _AttackProperties;
        }
    }


    public bool HasAttackProperty(
        AttackProperty AttackProperty
    )
    {
        if (_AttackProperties == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _AttackProperties.Length;
            i++
        )
        {
            if (_AttackProperties[i] ==
                AttackProperty)
            {
                return true;
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
                "Attack Outcome Name is empty.";

            return false;
        }


        if (_Result ==
            OutcomeResult.None)
        {
            ValidationMessage =
                $"Attack Outcome '{_Name}' has no Result.";

            return false;
        }


        if (_Damage < 0)
        {
            ValidationMessage =
                $"Attack Outcome '{_Name}' has invalid Damage.";

            return false;
        }


        if (_AttackProperties != null)
        {
            HashSet<AttackProperty> UsedProperties =
                new HashSet<AttackProperty>();


            for (
                int i = 0;
                i < _AttackProperties.Length;
                i++
            )
            {
                AttackProperty Property =
                    _AttackProperties[i];


                if (Property ==
                    AttackProperty.None)
                {
                    ValidationMessage =
                        $"Attack Outcome '{_Name}' contains an unconfigured Attack Property.";

                    return false;
                }


                if (!UsedProperties.Add(
                        Property))
                {
                    ValidationMessage =
                        $"Attack Outcome '{_Name}' contains duplicate Attack Property '{Property}'.";

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
public class AttackD20Outcome :
    D20RollRange
{
    [SerializeField]
    private AttackOutcome _Outcome;


    public AttackOutcome Outcome
    {
        get
        {
            return _Outcome;
        }
    }


    // Validates both this D20 range and its Attack-specific Outcome.
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
                "Attack D20 Outcome has no Outcome.";

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
public class StatusAffliction
{
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Status _Status;

    [Range(0, 100)]
    [SerializeField]
    private int _ChancePercent;

    [SerializeField]
    private OutcomeTrigger _Triggers;


    public Definition_Status Status
    {
        get
        {
            return _Status;
        }
    }


    public int ChancePercent
    {
        get
        {
            return _ChancePercent;
        }
    }


    public OutcomeTrigger Triggers
    {
        get
        {
            return _Triggers;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Status == null)
        {
            ValidationMessage =
                "Status Affliction has no Status.";

            return false;
        }


        if (_ChancePercent < 0 ||
            _ChancePercent > 100)
        {
            ValidationMessage =
                $"Status Affliction '{_Status.name}' has an invalid Chance.";

            return false;
        }


        if (_Triggers ==
            OutcomeTrigger.None)
        {
            ValidationMessage =
                $"Status Affliction '{_Status.name}' has no Outcome Trigger.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Reusable Weapon.
[CreateAssetMenu(
    fileName = "Weapon_New",
    menuName = "Admirals/Weapon"
)]
public class Definition_Weapon :
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

    // A Weapon may belong to several overlapping classifications.
    // Example: Missile + Radar Guided.
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_WeaponType[] _Types;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Tier _Tier;


    [Header("Targeting")]
    [SerializeField]
    private Domain _TargetDomains;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_UnitType[] _EligibleUnitTypes;


    [Header("Replenishment")]
    [Min(0)]
    [SerializeField]
    private int _ReplenishRate;

    [Min(0)]
    [SerializeField]
    private int _ReplenishInterval;


    [Header("Attack Outcome")]
    [SerializeField]
    private bool _UsesD20Roll;

    [SerializeField]
    private AttackOutcome _AttackOutcome;

    [SerializeField]
    private AttackD20Outcome[] _D20Outcomes;


    [Header("Status Afflictions")]
    [SerializeField]
    private StatusAffliction[] _StatusAfflictions;


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


    public Definition_WeaponType[] Types
    {
        get
        {
            return _Types;
        }
    }


    public Definition_Tier Tier
    {
        get
        {
            return _Tier;
        }
    }


    public Domain TargetDomains
    {
        get
        {
            return _TargetDomains;
        }
    }


    public Definition_UnitType[] EligibleUnitTypes
    {
        get
        {
            return _EligibleUnitTypes;
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


    public bool UsesD20Roll
    {
        get
        {
            return _UsesD20Roll;
        }
    }


    public AttackOutcome AttackOutcome
    {
        get
        {
            return _AttackOutcome;
        }
    }


    public AttackD20Outcome[] D20Outcomes
    {
        get
        {
            return _D20Outcomes;
        }
    }


    public StatusAffliction[] StatusAfflictions
    {
        get
        {
            return _StatusAfflictions;
        }
    }


    // Checks whether this Weapon carries a particular Weapon Type.
    public bool HasWeaponType(
        Definition_WeaponType WeaponType
    )
    {
        if (WeaponType == null ||
            _Types == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _Types.Length;
            i++
        )
        {
            if (_Types[i] ==
                WeaponType)
            {
                return true;
            }
        }


        return false;
    }


    // Checks whether at least one of the Unit's Types
    // is allowed to equip this Weapon.
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


    // Resolves either the fixed Attack Outcome or
    // the appropriate D20 Attack Outcome.
    public bool TryGetAttackOutcome(
        int RawRoll,
        out AttackOutcome FoundOutcome,
        int RollModifier = 0
    )
    {
        FoundOutcome =
            null;


        if (!_UsesD20Roll)
        {
            if (_AttackOutcome == null)
            {
                return false;
            }


            FoundOutcome =
                _AttackOutcome;

            return true;
        }


        if (!D20Roll.TryGetOutcome(
                _D20Outcomes,
                RawRoll,
                out AttackD20Outcome D20Outcome,
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
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Weapon Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Weapon ID is empty.";

            return false;
        }


        if (_Manufacturer == null)
        {
            ValidationMessage =
                "Weapon has no Manufacturer.";

            return false;
        }


        if (_Tier == null)
        {
            ValidationMessage =
                "Weapon has no Tier.";

            return false;
        }


        if (_Types == null ||
            _Types.Length == 0)
        {
            ValidationMessage =
                "Weapon has no Weapon Types.";

            return false;
        }


        HashSet<Definition_WeaponType> UsedWeaponTypes =
            new HashSet<Definition_WeaponType>();


        for (
            int i = 0;
            i < _Types.Length;
            i++
        )
        {
            if (_Types[i] == null)
            {
                ValidationMessage =
                    $"Weapon Type {i} is empty.";

                return false;
            }


            if (!UsedWeaponTypes.Add(
                    _Types[i]))
            {
                ValidationMessage =
                    $"Weapon Type '{_Types[i].name}' is assigned more than once.";

                return false;
            }
        }


        if (_TargetDomains ==
            Domain.None)
        {
            ValidationMessage =
                "Weapon has no Target Domains.";

            return false;
        }


        if (_EligibleUnitTypes == null ||
            _EligibleUnitTypes.Length == 0)
        {
            ValidationMessage =
                "Weapon has no Eligible Unit Types.";

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


        if (_ReplenishRate < 0 ||
            _ReplenishInterval < 0)
        {
            ValidationMessage =
                "Weapon Replenishment values cannot be negative.";

            return false;
        }


        if ((_ReplenishRate == 0) !=
            (_ReplenishInterval == 0))
        {
            ValidationMessage =
                "Weapon Replenish Rate and Replenish Interval must either both be configured or both be zero.";

            return false;
        }


        if (_UsesD20Roll)
        {
            if (_D20Outcomes == null ||
                _D20Outcomes.Length == 0)
            {
                ValidationMessage =
                    "Weapon uses a D20 Roll but has no D20 Outcomes.";

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
                        $"Weapon D20 Outcome {i} is empty.";

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
            if (_AttackOutcome == null)
            {
                ValidationMessage =
                    "Weapon has no Attack Outcome.";

                return false;
            }


            if (!_AttackOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        if (_StatusAfflictions != null)
        {
            for (
                int i = 0;
                i < _StatusAfflictions.Length;
                i++
            )
            {
                if (_StatusAfflictions[i] == null)
                {
                    ValidationMessage =
                        $"Status Affliction {i} is empty.";

                    return false;
                }


                if (!_StatusAfflictions[i].Validate(
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


    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Weapon '{name}': {ValidationMessage}",
                this
            );
        }
    }
}