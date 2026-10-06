using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "WeaponType_New",
    menuName = "Admirals/Weapon Type Definition"
)]
public class WeaponTypeDefinition : ScriptableObject
{
    // Display Name Of The Weapon Type
    [SerializeField] private string _WeaponTypeName;

    // Badge / Sprite Used To Represent The Weapon Type
    [SerializeField] private Sprite _WeaponTypeSprite;


    // Read-Only Access For Other Systems
    public string WeaponTypeName
    {
        get { return _WeaponTypeName; }
    }

    public Sprite WeaponTypeSprite
    {
        get { return _WeaponTypeSprite; }
    }
}