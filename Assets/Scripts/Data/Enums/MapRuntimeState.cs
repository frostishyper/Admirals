using System.Collections.Generic;
using UnityEngine;

// Runtime Data Class - Not Attached To A GameObject
// Holds The Live Gameplay State Of The Loaded Map
public class MapRuntimeState
{
    private readonly Dictionary<Vector2Int, MapCellState> _Cells =
        new();


    public IReadOnlyDictionary<Vector2Int, MapCellState> Cells
    {
        get { return _Cells; }
    }

    public int CellCount
    {
        get { return _Cells.Count; }
    }


    internal void AddCell(
        Vector2Int Coordinate,
        TerrainType TerrainType
    )
    {
        _Cells[Coordinate] =
            new MapCellState(
                Coordinate,
                TerrainType
            );
    }


    public bool TryGetCell(
        Vector2Int Coordinate,
        out MapCellState Cell
    )
    {
        return
            _Cells.TryGetValue(
                Coordinate,
                out Cell
            );
    }


    public bool IsOccupied(
        Vector2Int Coordinate
    )
    {
        if (!_Cells.TryGetValue(
                Coordinate,
                out MapCellState Cell))
        {
            return false;
        }

        return
            Cell.OccupyingUnit != null;
    }


    public bool HasTileTag(
        Vector2Int Coordinate,
        TileTag Tag
    )
    {
        if (!_Cells.TryGetValue(
                Coordinate,
                out MapCellState Cell))
        {
            return false;
        }

        return
            Cell.HasTag(
                Tag
            );
    }


    public bool TryGetMine(
        Vector2Int Coordinate,
        out MineState Mine
    )
    {
        Mine = null;

        if (!_Cells.TryGetValue(
                Coordinate,
                out MapCellState Cell))
        {
            return false;
        }

        if (Cell.Mine == null)
        {
            return false;
        }

        Mine =
            Cell.Mine;

        return true;
    }


    public bool CanPlaceMine(
        Vector2Int Coordinate,
        MineDeploymentSystemDefinition SystemDefinition
    )
    {
        if (SystemDefinition == null)
        {
            return false;
        }


        if (!_Cells.TryGetValue(
                Coordinate,
                out MapCellState Cell))
        {
            return false;
        }


        if (Cell.Mine != null)
        {
            return false;
        }


        if (Cell.OccupyingUnit != null)
        {
            return false;
        }


        if (!SystemDefinition.CanDeployOnTerrain(
                Cell.TerrainType))
        {
            return false;
        }


        return true;
    }


    public bool TryPlaceMine(
        Vector2Int Coordinate,
        UnitRuntimeState SourceUnit,
        MineDeploymentSystemDefinition SystemDefinition,
        out MineState Mine
    )
    {
        Mine = null;


        if (SourceUnit == null ||
            SystemDefinition == null)
        {
            return false;
        }


        if (!CanPlaceMine(
                Coordinate,
                SystemDefinition))
        {
            return false;
        }


        MapCellState Cell =
            _Cells[Coordinate];


        Mine =
            new MineState(
                Coordinate,
                SourceUnit,
                SystemDefinition
            );


        Cell.Mine =
            Mine;


        Cell.AddTag(
            TileTag.Mined
        );


        return true;
    }


    public bool RemoveMine(
        Vector2Int Coordinate
    )
    {
        if (!_Cells.TryGetValue(
                Coordinate,
                out MapCellState Cell))
        {
            return false;
        }


        if (Cell.Mine == null)
        {
            return false;
        }


        Cell.Mine =
            null;


        Cell.RemoveTag(
            TileTag.Mined
        );


        return true;
    }
}