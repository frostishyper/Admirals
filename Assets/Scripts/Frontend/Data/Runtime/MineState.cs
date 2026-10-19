using UnityEngine;

// Runtime Data Class - Not Attached To A GameObject
// Represents One Live Mine Deployed Onto The Board
public class MineState
{
    public Vector2Int Coordinate
    {
        get;
        private set;
    }

    public UnitRuntimeState SourceUnit
    {
        get;
        private set;
    }

    public MineDeploymentSystemDefinition SystemDefinition
    {
        get;
        private set;
    }


    public MineState(
        Vector2Int Coordinate,
        UnitRuntimeState SourceUnit,
        MineDeploymentSystemDefinition SystemDefinition
    )
    {
        this.Coordinate =
            Coordinate;

        this.SourceUnit =
            SourceUnit;

        this.SystemDefinition =
            SystemDefinition;
    }
}