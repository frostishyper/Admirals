using System;
using System.Collections.Generic;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class UnitWeaponSlot
{
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Weapon _Weapon;

    [Min(0)]
    [SerializeField]
    private int _MaxCharges;


    public Definition_Weapon Weapon
    {
        get
        {
            return _Weapon;
        }
    }


    public int MaxCharges
    {
        get
        {
            return _MaxCharges;
        }
    }


    public bool Validate(
        Definition_UnitType[] UnitTypes,
        out string ValidationMessage
    )
    {
        if (_Weapon == null)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_MaxCharges < 1)
        {
            ValidationMessage =
                $"Weapon '{_Weapon.Name}' has no Charge capacity.";

            return false;
        }


        if (!_Weapon.IsEligibleFor(
                UnitTypes))
        {
            ValidationMessage =
                $"Weapon '{_Weapon.Name}' is not eligible for this Unit's types.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class CarriedUnitLoadout
{
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Unit _Unit;

    [Min(1)]
    [SerializeField]
    private int _Count = 1;


    public Definition_Unit Unit
    {
        get
        {
            return _Unit;
        }
    }


    public int Count
    {
        get
        {
            return _Count;
        }
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class UnitSystemSlot
{
    [SerializeField]
    private SystemSlotType _SlotType;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_System _System;

    [Min(0)]
    [SerializeField]
    private int _MaxCharges;


    [Header("Carried Units")]
    [SerializeField]
    private CarriedUnitLoadout[] _CarriedUnits;


    public SystemSlotType SlotType
    {
        get
        {
            return _SlotType;
        }
    }


    public Definition_System System
    {
        get
        {
            return _System;
        }
    }


    public int MaxCharges
    {
        get
        {
            return _MaxCharges;
        }
    }


    public CarriedUnitLoadout[] CarriedUnits
    {
        get
        {
            return _CarriedUnits;
        }
    }


    public bool Validate(
        Definition_UnitType[] UnitTypes,
        out string ValidationMessage
    )
    {
        if (_SlotType == SystemSlotType.None)
        {
            ValidationMessage =
                "System Slot has no Slot Type.";

            return false;
        }


        if (_System == null)
        {
            if (_CarriedUnits != null &&
                _CarriedUnits.Length > 0)
            {
                ValidationMessage =
                    "Empty System Slot contains Carried Units.";

                return false;
            }


            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_System.Slot !=
            _SlotType)
        {
            ValidationMessage =
                $"System '{_System.Name}' belongs to {_System.Slot}, " +
                $"but the Unit Slot is {_SlotType}.";

            return false;
        }


        if (!_System.IsEligibleFor(
                UnitTypes))
        {
            ValidationMessage =
                $"System '{_System.Name}' is not eligible for this Unit's types.";

            return false;
        }


        if (_System.UsesCharges)
        {
            if (_MaxCharges < 1)
            {
                ValidationMessage =
                    $"System '{_System.Name}' uses Charges but its Slot has no Charge capacity.";

                return false;
            }
        }
        else if (_MaxCharges != 0)
        {
            ValidationMessage =
                $"System '{_System.Name}' does not use Charges but its Slot has a Charge capacity.";

            return false;
        }


        bool CarriesUnits =
            _System.UnitDeployment != null &&
            _System.UnitDeployment.Enabled;


        if (!CarriesUnits)
        {
            if (_CarriedUnits != null &&
                _CarriedUnits.Length > 0)
            {
                ValidationMessage =
                    $"System '{_System.Name}' cannot carry Units.";

                return false;
            }


            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_CarriedUnits == null)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        int AuthoredCarriedUnitCount =
            0;


        for (int i = 0;
            i < _CarriedUnits.Length;
            i++)
        {
            if (_CarriedUnits[i] != null)
            {
                AuthoredCarriedUnitCount +=
                    Mathf.Max(0, _CarriedUnits[i].Count);
            }
        }


        if (AuthoredCarriedUnitCount >
            _System.UnitDeployment.CarrySlots)
        {
            ValidationMessage =
                $"System '{_System.Name}' has {_System.UnitDeployment.CarrySlots} Carry Slots " +
                $"but {AuthoredCarriedUnitCount} carried Units were authored.";

            return false;
        }


        for (
            int i = 0;
            i < _CarriedUnits.Length;
            i++
        )
        {
            CarriedUnitLoadout Carried =
                _CarriedUnits[i];


            if (Carried == null ||
                Carried.Unit == null)
            {
                ValidationMessage =
                    $"Carried Unit Slot {i} is empty.";

                return false;
            }


            if (Carried.Count < 1)
            {
                ValidationMessage =
                    $"Carried Unit Slot {i} has an invalid Count.";

                return false;
            }


            if (!_System.UnitDeployment.IsEligibleFor(
                    Carried.Unit.UnitTypes))
            {
                ValidationMessage =
                    $"Unit '{Carried.Unit.Name}' cannot be carried by System '{_System.Name}'.";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Reusable Unit Class.
[CreateAssetMenu(
    fileName = "Unit_New",
    menuName = "Admirals/Unit"
)]
public class Definition_Unit :
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
    private Definition_UnitType[] _UnitTypes;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Tier _Tier;


    [Header("Base State")]
    [SerializeField]
    private Domain _BaseDomain;

    [Min(1)]
    [SerializeField]
    private int _Health = 1;

    [Min(0)]
    [SerializeField]
    private int _Stealth;

    [Min(0)]
    [SerializeField]
    private int _AbsoluteVision;


    [Header("Footprint")]
    [SerializeField]
    private Vector2Int _FootprintSize =
        Vector2Int.one;

    // Origin Point used by Sensors, CMS, buffs and other OriginPoint Patterns.
    [SerializeField]
    private Vector2Int _OriginPoint;

    // Point around which the Unit rotates during movement.
    // Rear placement gives an arm-like turn while a central placement pivots in place.
    [SerializeField]
    private Vector2Int _MovementPivot;


    [Header("Movement")]
    [ScriptableObjectPicker(
        nameof(Definition_Pattern.Mode),
        PatternMode.FootprintMovement
    )]
    [SerializeField]
    private Definition_Pattern _MovementPattern;


    [SerializeField]
    private UnitWeaponSlot[] _WeaponSlots;

    [SerializeField]
    private UnitSystemSlot[] _SystemSlots;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Ability[] _Abilities;


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


    public Definition_UnitType[] UnitTypes
    {
        get
        {
            return _UnitTypes;
        }
    }


    public Definition_Tier Tier
    {
        get
        {
            return _Tier;
        }
    }


    public Domain BaseDomain
    {
        get
        {
            return _BaseDomain;
        }
    }


    public int Health
    {
        get
        {
            return _Health;
        }
    }


    public int Stealth
    {
        get
        {
            return _Stealth;
        }
    }


    public int AbsoluteVision
    {
        get
        {
            return _AbsoluteVision;
        }
    }


    public Vector2Int FootprintSize
    {
        get
        {
            return _FootprintSize;
        }
    }


    public Vector2Int OriginPoint
    {
        get
        {
            return _OriginPoint;
        }
    }


    public Vector2Int MovementPivot
    {
        get
        {
            return _MovementPivot;
        }
    }


    public Definition_Pattern MovementPattern
    {
        get
        {
            return _MovementPattern;
        }
    }


    public UnitWeaponSlot[] WeaponSlots
    {
        get
        {
            return _WeaponSlots;
        }
    }


    public UnitSystemSlot[] SystemSlots
    {
        get
        {
            return _SystemSlots;
        }
    }


    public Definition_Ability[] Abilities
    {
        get
        {
            return _Abilities;
        }
    }


    // A Unit itself occupies exactly one Domain at a time.
    private bool HasSingleDomain(
        Domain DomainValue
    )
    {
        int Value =
            (int)DomainValue;


        return
            Value != 0 &&
            (Value & (Value - 1)) == 0 &&
            (DomainValue & Domain.All) == DomainValue;
    }


    // Checks whether one local footprint coordinate is actually inside the Unit.
    private bool IsInsideFootprint(
        Vector2Int Point
    )
    {
        return
            Point.x >= 0 &&
            Point.y >= 0 &&
            Point.x < _FootprintSize.x &&
            Point.y < _FootprintSize.y;
    }


    // Validates the complete authored Unit data.
    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Unit Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Unit ID is empty.";

            return false;
        }


        if (_Manufacturer == null)
        {
            ValidationMessage =
                "Unit has no Manufacturer.";

            return false;
        }


        if (_Tier == null)
        {
            ValidationMessage =
                "Unit has no Tier.";

            return false;
        }


        if (_UnitTypes == null ||
            _UnitTypes.Length == 0)
        {
            ValidationMessage =
                "Unit has no Unit Types.";

            return false;
        }


        HashSet<Definition_UnitType> UsedUnitTypes =
            new HashSet<Definition_UnitType>();


        for (
            int i = 0;
            i < _UnitTypes.Length;
            i++
        )
        {
            if (_UnitTypes[i] == null)
            {
                ValidationMessage =
                    $"Unit Type {i} is empty.";

                return false;
            }


            if (!UsedUnitTypes.Add(
                    _UnitTypes[i]))
            {
                ValidationMessage =
                    $"Unit Type '{_UnitTypes[i].Name}' is assigned more than once.";

                return false;
            }
        }


        if (!HasSingleDomain(
                _BaseDomain))
        {
            ValidationMessage =
                "Unit Base Domain must be exactly one Domain.";

            return false;
        }


        if (_Health < 1)
        {
            ValidationMessage =
                "Unit Health must be at least one.";

            return false;
        }


        if (_FootprintSize.x < 1 ||
            _FootprintSize.y < 1)
        {
            ValidationMessage =
                "Unit Footprint dimensions must both be at least one.";

            return false;
        }


        if (!IsInsideFootprint(
                _OriginPoint))
        {
            ValidationMessage =
                "Unit Origin Point must exist inside its Footprint.";

            return false;
        }


        if (!IsInsideFootprint(
                _MovementPivot))
        {
            ValidationMessage =
                "Unit Movement Pivot must exist inside its Footprint.";

            return false;
        }


        if (_MovementPattern == null)
        {
            ValidationMessage =
                "Unit has no Movement Pattern.";

            return false;
        }


        if (_MovementPattern.Mode !=
            PatternMode.FootprintMovement)
        {
            ValidationMessage =
                "Unit Movement Pattern must use FootprintMovement mode.";

            return false;
        }


        if (_WeaponSlots != null)
        {
            for (
                int i = 0;
                i < _WeaponSlots.Length;
                i++
            )
            {
                UnitWeaponSlot Slot =
                    _WeaponSlots[i];


                if (Slot == null)
                {
                    ValidationMessage =
                        $"Weapon Slot {i} is empty.";

                    return false;
                }


                if (!Slot.Validate(
                        _UnitTypes,
                        out string SlotMessage))
                {
                    ValidationMessage =
                        $"Weapon Slot {i} is invalid: {SlotMessage}";

                    return false;
                }
            }
        }


        if (_SystemSlots != null)
        {
            for (
                int i = 0;
                i < _SystemSlots.Length;
                i++
            )
            {
                UnitSystemSlot Slot =
                    _SystemSlots[i];


                if (Slot == null)
                {
                    ValidationMessage =
                        $"System Slot {i} is empty.";

                    return false;
                }


                if (!Slot.Validate(
                        _UnitTypes,
                        out string SlotMessage))
                {
                    ValidationMessage =
                        $"System Slot {i} is invalid: {SlotMessage}";

                    return false;
                }
            }
        }


        if (_Abilities != null)
        {
            for (
                int i = 0;
                i < _Abilities.Length;
                i++
            )
            {
                if (_Abilities[i] == null)
                {
                    ValidationMessage =
                        $"Ability {i} is empty.";

                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Keeps Unit-local points inside the rectangular Footprint
    // and reports invalid authored Unit data.
    private void OnValidate()
    {
        _FootprintSize.x =
            Mathf.Max(
                1,
                _FootprintSize.x
            );

        _FootprintSize.y =
            Mathf.Max(
                1,
                _FootprintSize.y
            );


        _OriginPoint.x =
            Mathf.Clamp(
                _OriginPoint.x,
                0,
                _FootprintSize.x - 1
            );

        _OriginPoint.y =
            Mathf.Clamp(
                _OriginPoint.y,
                0,
                _FootprintSize.y - 1
            );


        _MovementPivot.x =
            Mathf.Clamp(
                _MovementPivot.x,
                0,
                _FootprintSize.x - 1
            );

        _MovementPivot.y =
            Mathf.Clamp(
                _MovementPivot.y,
                0,
                _FootprintSize.y - 1
            );


        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Unit '{name}': {ValidationMessage}",
                this
            );
        }
    }
}
