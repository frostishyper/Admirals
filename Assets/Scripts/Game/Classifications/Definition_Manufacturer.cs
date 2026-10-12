using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Manufacturer_New",
    menuName = "Admirals/Classifications/Manufacturer"
)]
public class Definition_Manufacturer : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;

    [SerializeField]
    private Sprite _Logo;


    [Header("Information")]
    [TextArea(3, 8)]
    [SerializeField]
    private string _Description;

    [SerializeField]
    private string _HeadquartersLocation;


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


    public Sprite Logo
    {
        get
        {
            return _Logo;
        }
    }


    public string Description
    {
        get
        {
            return _Description;
        }
    }


    public string HeadquartersLocation
    {
        get
        {
            return _HeadquartersLocation;
        }
    }
}