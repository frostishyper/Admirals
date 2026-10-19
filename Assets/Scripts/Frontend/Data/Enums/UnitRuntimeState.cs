using System.Collections.Generic;
using UnityEngine;

// Runtime Data Class - Not Attached To A GameObject
// Represents One Live Unit Instance During A Match
public class UnitRuntimeState
{
    private readonly List<Vector2Int> _OccupiedCoordinates =
        new();


    public int RuntimeID
    {
        get;
        private set;
    }


    public UnitDefinition Definition
    {
        get;
        private set;
    }


    public MatchSide Owner
    {
        get;
        private set;
    }


    public int CurrentHP
    {
        get;
        private set;
    }


    public Vector2Int AnchorCoordinate
    {
        get;
        private set;
    }


    public UnitOrientation Orientation
    {
        get;
        private set;
    }


    public bool IsDeployed
    {
        get;
        private set;
    }


    public bool IsDestroyed
    {
        get
        {
            return CurrentHP <= 0;
        }
    }


    public IReadOnlyList<Vector2Int> OccupiedCoordinates
    {
        get
        {
            return _OccupiedCoordinates;
        }
    }


    public UnitRuntimeState(
        int RuntimeID,
        UnitDefinition Definition,
        MatchSide Owner
    )
    {
        this.RuntimeID =
            RuntimeID;

        this.Definition =
            Definition;

        this.Owner =
            Owner;


        CurrentHP =
            Definition.BaseHP;


        AnchorCoordinate =
            Vector2Int.zero;


        Orientation =
            UnitOrientation.North;


        IsDeployed =
            false;
    }


    internal void SetOccupiedCoordinates(
        Vector2Int AnchorCoordinate,
        UnitOrientation Orientation,
        IEnumerable<Vector2Int> Coordinates
    )
    {
        this.AnchorCoordinate =
            AnchorCoordinate;

        this.Orientation =
            Orientation;


        _OccupiedCoordinates.Clear();


        if (Coordinates != null)
        {
            _OccupiedCoordinates.AddRange(
                Coordinates
            );
        }
    }


    internal void ClearOccupiedCoordinates()
    {
        _OccupiedCoordinates.Clear();
    }


    internal void SetDeployed(
        bool IsDeployed
    )
    {
        this.IsDeployed =
            IsDeployed;
    }
}