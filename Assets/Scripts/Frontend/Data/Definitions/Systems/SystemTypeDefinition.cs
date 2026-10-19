using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "SystemType_New",
    menuName = "Admirals/System Type Definition"
)]
public class SystemTypeDefinition : ScriptableObject
{
    // Display Name Of The System Type
    [SerializeField] private string _SystemTypeName;

    // Unique ID Used To Identify The System Type
    [SerializeField] private string _SystemTypeID;

    // Badge / Sprite Used To Represent The System Type
    [SerializeField] private Sprite _SystemTypeSprite;


    // Read-Only Access For Other Systems
    public string SystemTypeName
    {
        get { return _SystemTypeName; }
    }

    public string SystemTypeID
    {
        get { return _SystemTypeID; }
    }

    public Sprite SystemTypeSprite
    {
        get { return _SystemTypeSprite; }
    }
}