using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Tier_New",
    menuName = "Admirals/Tier Definition"
)]
public class TierDefinition : ScriptableObject
{
    // Display Name Of The Tier
    [SerializeField] private string _TierName;

    // Numerical Value Of The Tier
    [SerializeField] private int _TierValue;

    // Badge / Sprite Used To Represent The Tier
    [SerializeField] private Sprite _TierSprite;


    // Read-Only Access For Other Systems
    public string TierName
    {
        get { return _TierName; }
    }

    public int TierValue
    {
        get { return _TierValue; }
    }

    public Sprite TierSprite
    {
        get { return _TierSprite; }
    }
}