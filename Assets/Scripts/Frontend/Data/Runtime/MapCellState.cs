using System.Collections.Generic;
using UnityEngine;

// Runtime Data Class - Not Attached To A GameObject
// Represents The Current Live State Of One Board Cell
public class MapCellState
{
    private readonly HashSet<TileTag> _TileTags =
        new();


    public Vector2Int Coordinate
    {
        get;
        private set;
    }


    public TerrainType TerrainType
    {
        get;
        private set;
    }


    public DeploymentZone DeploymentZone
    {
        get;
        private set;
    }


    public UnitRuntimeState OccupyingUnit
    {
        get;
        internal set;
    }


    public MineState Mine
    {
        get;
        internal set;
    }


    public IReadOnlyCollection<TileTag> TileTags
    {
        get
        {
            return _TileTags;
        }
    }


    public MapCellState(
        Vector2Int Coordinate,
        TerrainType TerrainType,
        DeploymentZone DeploymentZone
    )
    {
        this.Coordinate =
            Coordinate;

        this.TerrainType =
            TerrainType;

        this.DeploymentZone =
            DeploymentZone;


        OccupyingUnit =
            null;

        Mine =
            null;
    }


    public bool HasTag(
        TileTag Tag
    )
    {
        return
            _TileTags.Contains(
                Tag
            );
    }


    internal void AddTag(
        TileTag Tag
    )
    {
        _TileTags.Add(
            Tag
        );
    }


    internal void RemoveTag(
        TileTag Tag
    )
    {
        _TileTags.Remove(
            Tag
        );
    }
}