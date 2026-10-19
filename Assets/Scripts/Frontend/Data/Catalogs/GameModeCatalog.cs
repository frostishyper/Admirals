using UnityEngine;

// Holds The Game Modes Available To The Game / Lobby
[CreateAssetMenu(
    fileName = "GameModeCatalog_Default",
    menuName = "Admirals/Game Mode Catalog"
)]
public class GameModeCatalog : ScriptableObject
{
    // Available Game Mode Assets
    [SerializeField] private GameModeInfo[] _GameModes;

    // Read-Only Access For Other Systems
    public GameModeInfo[] GameModes
    {
        get { return _GameModes; }
    }
}