using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Tier_New",
    menuName = "Admirals/Classifications/Tier"
)]
public class Definition_Tier : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;

    [SerializeField]
    private Sprite _Badge;


    [Header("Ranking")]
    [Min(0)]
    [SerializeField]
    private int _TierValue;


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


    public int TierValue
    {
        get
        {
            return _TierValue;
        }
    }
}