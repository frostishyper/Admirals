using UnityEngine;

// Basic Information Used To Identify A Map For The Lobby
[CreateAssetMenu(
    fileName = "Map_New",
    menuName = "Admirals/Map Info"
)]
public class MapInfo : ScriptableObject
{
    // Display Name Of The Map
    [SerializeField] private string _MapName;

    // Unique ID For The Map
    [SerializeField] private string _MapID;

    // Size Of The Map Grid
    [SerializeField] private Vector2Int _MapSize;

    // Reference To The Actual Map Object / Prefab
    [SerializeField] private GameObject _MapReference;


    // Read-Only Access For Other Systems
    public string MapName
    {
        get { return _MapName; }
    }

    public string MapID
    {
        get { return _MapID; }
    }

    public Vector2Int MapSize
    {
        get { return _MapSize; }
    }

    public GameObject MapReference
    {
        get { return _MapReference; }
    }
}