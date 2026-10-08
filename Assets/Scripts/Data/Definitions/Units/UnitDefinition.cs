using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines The Authored Gameplay Data And Fixed Loadout Of A Unit
[CreateAssetMenu(
    fileName = "Unit_New",
    menuName = "Admirals/Unit Definition"
)]
public class UnitDefinition : ScriptableObject
{
    // Display Name Of The Unit
    [SerializeField] private string _UnitName;

    // Unique ID Used To Identify The Unit
    [SerializeField] private string _UnitID;

    // Manufacturer Of The Unit
    [ScriptableObjectPicker]
    [SerializeField] private Manufacturers _Manufacturer;

    // Universal Tier Assigned To The Unit
    [ScriptableObjectPicker]
    [SerializeField] private TierDefinition _Tier;

    // Classification Of The Unit
    [ScriptableObjectPicker]
    [SerializeField] private UnitTypeDefinition _UnitType;

    // Natural Domain Occupied By The Unit
    [ScriptableObjectPicker]
    [SerializeField] private Domains _Domain;


    // Base Health Of The Unit
    [SerializeField]
    [Min(1)]
    private int _BaseHP = 1;

    // Base Stealth Value Used By Search Resolution
    [SerializeField]
    [Min(0)]
    private int _Stealth;

    // Defines How Far Movement Extends From The Unit's Current Footprint
    // Movement Resolution Projects This Pattern From Every Occupied Tile
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _MovementPattern;

    [SerializeField]
    [Min(1)]
    private int _AbsoluteVisionRange = 1;


    // Number Of Grid Tiles Occupied Along Each Local Axis
    // Example: (3, 1) Represents A Three-Tile-Long, One-Tile-Wide Unit
    [SerializeField] private Vector2Int _FootprintSize = Vector2Int.one;

    // Local Tile Within The Footprint Used As The Unit's Gameplay Origin
    // Coordinates Are Zero-Based
    [SerializeField] private Vector2Int _FootprintOrigin = Vector2Int.zero;


    // Fixed Weapon Installations Authored For This Unit
    [SerializeField] private WeaponSlot[] _WeaponSlots;

    // Fixed Onboard System Installations Authored For This Unit
    [SerializeField] private OnboardSystemSlot[] _OnboardSystemSlots;

    [ScriptableObjectPicker]
    [SerializeField] private AbilityDefinition[] _Abilities;


    public string UnitName
    {
        get { return _UnitName; }
    }

    public string UnitID
    {
        get { return _UnitID; }
    }

    public Manufacturers Manufacturer
    {
        get { return _Manufacturer; }
    }

    public TierDefinition Tier
    {
        get { return _Tier; }
    }

    public UnitTypeDefinition UnitType
    {
        get { return _UnitType; }
    }

    public Domains Domain
    {
        get { return _Domain; }
    }

    public int BaseHP
    {
        get { return _BaseHP; }
    }

    public int Stealth
    {
        get { return _Stealth; }
    }

    public PatternDefinition MovementPattern
    {
        get { return _MovementPattern; }
    }

    public int AbsoluteVisionRange
    {
        get { return _AbsoluteVisionRange; }
    }

    public Vector2Int FootprintSize
    {
        get { return _FootprintSize; }
    }

    public Vector2Int FootprintOrigin
    {
        get { return _FootprintOrigin; }
    }

    public WeaponSlot[] WeaponSlots
    {
        get { return _WeaponSlots; }
    }

    public OnboardSystemSlot[] OnboardSystemSlots
    {
        get { return _OnboardSystemSlots; }
    }

    public AbilityDefinition[] Abilities
    {
        get { return _Abilities; }
    }


    // Keeps Authored Unit Values And Footprint Coordinates Valid
    private void OnValidate()
    {
        _BaseHP =
            Mathf.Max(1, _BaseHP);

        _Stealth =
            Mathf.Max(0, _Stealth);

        _AbsoluteVisionRange =
            Mathf.Max(1, _AbsoluteVisionRange);

        _FootprintSize.x =
            Mathf.Max(1, _FootprintSize.x);

        _FootprintSize.y =
            Mathf.Max(1, _FootprintSize.y);


        _FootprintOrigin.x =
            Mathf.Clamp(
                _FootprintOrigin.x,
                0,
                _FootprintSize.x - 1
            );

        _FootprintOrigin.y =
            Mathf.Clamp(
                _FootprintOrigin.y,
                0,
                _FootprintSize.y - 1
            );
    }
}