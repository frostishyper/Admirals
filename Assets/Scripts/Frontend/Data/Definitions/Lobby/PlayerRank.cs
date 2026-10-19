using UnityEngine;

// Basic Information Used To Identify A Rank
[CreateAssetMenu(
    fileName = "Rank_New",
    menuName = "Admirals/Player Rank"
)]
public class PlayerRank : ScriptableObject
{
    // Display Name Of The Rank
    [SerializeField] private string _RankName;

    // Unique ID For The Rank
    [SerializeField] private string _RankID;

    // Rank Tier (Higher The Better)
    [SerializeField] private int _RankTier;

    // Sprite Used To Display The Rank
    [SerializeField] private Sprite _RankSprite;


    // Read-Only Access For Other Systems
    public string RankName
    {
        get { return _RankName; }
    }

    public string RankID
    {
        get { return _RankID; }
    }

    public int RankTier
    {
        get { return _RankTier; }
    }

    public Sprite RankSprite
    {
        get { return _RankSprite; }
    }
}