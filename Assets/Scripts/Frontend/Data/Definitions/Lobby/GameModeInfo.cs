using UnityEngine;

// Basic Information Used To Identify A Rule Set
[CreateAssetMenu(
    fileName = "GameMode_New",
    menuName = "Admirals/GameMode Info"
)]
public class GameModeInfo : ScriptableObject
{
    // Display Name Of The Rule Set
    [SerializeField] private string _GameModeName;

    // Unique ID For The Rule Set
    [SerializeField] private string _GameModeID;

    // Reference To The Actual Gamemode / Prefab
    [SerializeField] private GameObject _GameModeReference;


    // Read-Only Access For Other Systems
    public string GameModeName
    {
        get { return _GameModeName; }
    }

    public string GameModeID
    {
        get { return _GameModeID; }
    }

    public GameObject GameModeReference
    {
        get { return _GameModeReference; }
    }
}