using UnityEngine;

// Basic Information Used To Identify A Rule Set
[CreateAssetMenu(
    fileName = "RuleSet_New",
    menuName = "Admirals/Rule Set Info"
)]
public class GameModeInfo : ScriptableObject
{
    // Display Name Of The Rule Set
    [SerializeField] private string _RuleSetName;

    // Unique ID For The Rule Set
    [SerializeField] private string _RuleSetID;


    // Read-Only Access For Other Systems
    public string RuleSetName
    {
        get { return _RuleSetName; }
    }

    public string RuleSetID
    {
        get { return _RuleSetID; }
    }
}