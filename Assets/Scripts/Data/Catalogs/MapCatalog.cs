using UnityEngine;

// Holds The Maps Available To The Game / Lobby
[CreateAssetMenu(
    fileName = "MapCatalog_Default",
    menuName = "Admirals/Map Catalog"
)]
public class MapCatalog : ScriptableObject
{
    // Available Map Assets
    [SerializeField] private MapInfo[] _Maps;

    // Read-Only Access For Other Systems
    public MapInfo[] Maps
    {
        get { return _Maps; }
    }
}