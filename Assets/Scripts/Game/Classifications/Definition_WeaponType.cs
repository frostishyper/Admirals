using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "WeaponType_New",
    menuName = "Admirals/Classifications/Weapon Type"
)]
public class Definition_WeaponType : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;

    [SerializeField]
    private Sprite _Badge;


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


    public Sprite Badge
    {
        get
        {
            return _Badge;
        }
    }
}