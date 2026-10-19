using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "SystemType_New",
    menuName = "Admirals/Classifications/System Type"
)]
public class Definition_SystemType : ScriptableObject
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