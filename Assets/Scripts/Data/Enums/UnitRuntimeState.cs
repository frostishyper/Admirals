using System.Collections.Generic;
using UnityEngine;

// Runtime Data Class - Not Attached To A GameObject
// Represents One Live Unit Instance During A Match
public class UnitRuntimeState
{
    private readonly List<Vector2Int> _OccupiedCoordinates = new();


    public UnitDefinition Definition { get; private set; }

    public int CurrentHP { get; private set; }

    public Vector2Int AnchorCoordinate { get; internal set; }

    public UnitOrientation Orientation { get; internal set; }

    public IReadOnlyList<Vector2Int> OccupiedCoordinates
    {
        get { return _OccupiedCoordinates; }
    }


    public UnitRuntimeState(UnitDefinition Definition)
    {
        this.Definition = Definition;

        CurrentHP = Definition.BaseHP;

        AnchorCoordinate = Vector2Int.zero;
        Orientation = UnitOrientation.North;
    }


    internal void SetOccupiedCoordinates(
        IEnumerable<Vector2Int> Coordinates
    )
    {
        _OccupiedCoordinates.Clear();
        _OccupiedCoordinates.AddRange(Coordinates);
    }


    internal void ClearOccupiedCoordinates()
    {
        _OccupiedCoordinates.Clear();
    }
}