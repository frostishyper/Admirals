using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Weapon_New",
    menuName = "Admirals/Weapon Definition"
)]
public class WeaponDefinition : ScriptableObject
{
    // Display Name Of The Weapon
    [SerializeField] private string _WeaponName;

    // Manufacturer Of The Weapon
    [SerializeField] private string _Manufacturer;

    // Universal Tier Assigned To This Weapon
    [SerializeField] private TierDefinition _Tier;

    // Type / Category Of This Weapon
    [SerializeField] private WeaponTypeDefinition _WeaponType;

    // Number Of Turns Required Before Replenishment Occurs
    [SerializeField] private int _ReplenishSpeed;

    // Number Of Charges Restored Whenever Replenishment Occurs
    [SerializeField] private int _ReplenishCount;

    // Domains This Weapon Can Target
    [SerializeField] private Domain[] _TargetDomains;

    // D20 Roll Ranges Used To Determine Damage
    [SerializeField] private WeaponRollRange[] _RollRanges;

    // Possible Secondary Effects Caused By This Weapon
    [SerializeField] private WeaponEffect[] _Effects;


    // Read-Only Access For Other Systems
    public string WeaponName
    {
        get { return _WeaponName; }
    }

    public string Manufacturer
    {
        get { return _Manufacturer; }
    }

    public TierDefinition Tier
    {
        get { return _Tier; }
    }

    public WeaponTypeDefinition WeaponType
    {
        get { return _WeaponType; }
    }

    public int ReplenishSpeed
    {
        get { return _ReplenishSpeed; }
    }

    public int ReplenishCount
    {
        get { return _ReplenishCount; }
    }

    public Domain[] TargetDomains
    {
        get { return _TargetDomains; }
    }

    public WeaponRollRange[] RollRanges
    {
        get { return _RollRanges; }
    }

    public WeaponEffect[] Effects
    {
        get { return _Effects; }
    }
}