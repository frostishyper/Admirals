using UnityEngine;

// Attach To [GameMode Prefab Root] GameObject
// Defines The Runtime Rules And Starting Forces For A GameMode
public class GameModeRules : MonoBehaviour
{
    [Header("Victory")]
    [SerializeField]
    private GameModeVictoryCondition _VictoryCondition =
        GameModeVictoryCondition.FleetElimination;


    [Header("Side One Starting Roster")]
    [SerializeField]
    private StartingUnitEntry[] _SideOneStartingRoster;


    [Header("Side Two Starting Roster")]
    [SerializeField]
    private StartingUnitEntry[] _SideTwoStartingRoster;


    public GameModeVictoryCondition VictoryCondition
    {
        get
        {
            return _VictoryCondition;
        }
    }


    public StartingUnitEntry[] SideOneStartingRoster
    {
        get
        {
            return _SideOneStartingRoster;
        }
    }


    public StartingUnitEntry[] SideTwoStartingRoster
    {
        get
        {
            return _SideTwoStartingRoster;
        }
    }
}