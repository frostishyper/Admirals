using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Effect_New",
    menuName = "Admirals/Effect Definition"
)]
public class EffectDefinition : ScriptableObject
{
    // Display Name Of The Effect
    [SerializeField] private string _EffectName;

    // Unique ID Used To Identify The Effect
    [SerializeField] private string _EffectID;


    // Read-Only Access For Other Systems
    public string EffectName
    {
        get { return _EffectName; }
    }

    public string EffectID
    {
        get { return _EffectID; }
    }
}