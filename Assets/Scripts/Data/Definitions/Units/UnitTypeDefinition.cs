using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines A Reusable Unit Classification And Its Display Badge
[CreateAssetMenu(
    fileName = "UnitType_New",
    menuName = "Admirals/Unit Type Definition"
)]
public class UnitTypeDefinition : ScriptableObject
{
    [SerializeField] private string _UnitTypeName;
    [SerializeField] private Sprite _UnitTypeSprite;


    public string UnitTypeName
    {
        get { return _UnitTypeName; }
    }

    public Sprite UnitTypeSprite
    {
        get { return _UnitTypeSprite; }
    }
}