using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


// Fixed authored Terrain classifications for Map Tiles.
public enum MapTerrain
{
    None = 0,

    Deep_Water = 1,
    Shallow_Water = 2,
    Land = 3,
    Barrier = 4
}


// Conditions attached to authored Map Tiles.
// More condition types can be added later without changing the Tile structure.
public enum MapTileCondition
{
    None = 0,

    Deployment_Player_A = 1,
    Deployment_Player_B = 2
}


// Embedded Serializable Data - Not A ScriptableObject
// Authored gameplay data for one Map coordinate.
[Serializable]
public class MapTileAuthoringData
{
    [SerializeField]
    private Vector2Int _Coordinate;

    [SerializeField]
    private MapTerrain _Terrain;

    [SerializeField]
    private MapTileCondition[] _Conditions;


    public Vector2Int Coordinate
    {
        get
        {
            return _Coordinate;
        }
    }


    public MapTerrain Terrain
    {
        get
        {
            return _Terrain;
        }
    }


    public MapTileCondition[] Conditions
    {
        get
        {
            return _Conditions;
        }
    }


    public MapTileAuthoringData(
        Vector2Int Coordinate
    )
    {
        _Coordinate =
            Coordinate;

        _Terrain =
            MapTerrain.None;

        _Conditions =
            Array.Empty<MapTileCondition>();
    }


    public void SetCoordinate(
        Vector2Int Coordinate
    )
    {
        _Coordinate =
            Coordinate;
    }


    public void SetTerrain(
        MapTerrain Terrain
    )
    {
        _Terrain =
            Terrain;
    }


    public bool HasCondition(
        MapTileCondition Condition
    )
    {
        if (Condition ==
                MapTileCondition.None ||
            _Conditions == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _Conditions.Length;
            i++
        )
        {
            if (_Conditions[i] ==
                Condition)
            {
                return true;
            }
        }


        return false;
    }


    public void AddCondition(
        MapTileCondition Condition
    )
    {
        if (Condition ==
                MapTileCondition.None ||
            HasCondition(
                Condition))
        {
            return;
        }


        List<MapTileCondition> NewConditions =
            new List<MapTileCondition>();


        if (_Conditions != null)
        {
            NewConditions.AddRange(
                _Conditions
            );
        }


        NewConditions.Add(
            Condition
        );


        _Conditions =
            NewConditions.ToArray();
    }


    public void RemoveCondition(
        MapTileCondition Condition
    )
    {
        if (_Conditions == null ||
            _Conditions.Length == 0)
        {
            return;
        }


        List<MapTileCondition> NewConditions =
            new List<MapTileCondition>();


        for (
            int i = 0;
            i < _Conditions.Length;
            i++
        )
        {
            if (_Conditions[i] !=
                Condition)
            {
                NewConditions.Add(
                    _Conditions[i]
                );
            }
        }


        _Conditions =
            NewConditions.ToArray();
    }


    public MapTileCondition GetDeploymentCondition()
    {
        if (HasCondition(
                MapTileCondition.Deployment_Player_A))
        {
            return
                MapTileCondition.Deployment_Player_A;
        }


        if (HasCondition(
                MapTileCondition.Deployment_Player_B))
        {
            return
                MapTileCondition.Deployment_Player_B;
        }


        return
            MapTileCondition.None;
    }


    public bool ValidateConditions(
        out string ValidationMessage
    )
    {
        if (_Conditions == null)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        HashSet<MapTileCondition> UsedConditions =
            new HashSet<MapTileCondition>();


        for (
            int i = 0;
            i < _Conditions.Length;
            i++
        )
        {
            MapTileCondition Condition =
                _Conditions[i];


            if (Condition ==
                MapTileCondition.None)
            {
                ValidationMessage =
                    $"Tile {_Coordinate} contains an empty Condition.";

                return false;
            }


            if (!UsedConditions.Add(
                    Condition))
            {
                ValidationMessage =
                    $"Tile {_Coordinate} contains duplicate Condition '{Condition}'.";

                return false;
            }
        }


        if (HasCondition(
                MapTileCondition.Deployment_Player_A) &&
            HasCondition(
                MapTileCondition.Deployment_Player_B))
        {
            ValidationMessage =
                $"Tile {_Coordinate} belongs to both Deployment Zones.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Attach To GameMap GameObject
// Owns The Authored Data And Visual Layer References For One Map Prefab.
public class MapAuthoring :
    MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private Vector2Int _MapSize =
        Vector2Int.one;


    [Header("Grid")]
    [SerializeField]
    private Grid _Grid;


    [Header("Visual Layers")]
    [SerializeField]
    private Tilemap _TerrainArt;

    [SerializeField]
    private Tilemap _BelowUnits;

    [SerializeField]
    private Transform _Units;

    [SerializeField]
    private Tilemap _TileMarkers;

    [SerializeField]
    private Tilemap _Fog;

    [SerializeField]
    private Tilemap _VFX;


    [HideInInspector]
    [SerializeField]
    private MapTileAuthoringData[] _Tiles;


    public Vector2Int MapSize
    {
        get
        {
            return _MapSize;
        }
    }


    public Grid GridReference
    {
        get
        {
            return _Grid;
        }
    }


    public Tilemap TerrainArt
    {
        get
        {
            return _TerrainArt;
        }
    }


    public Tilemap BelowUnits
    {
        get
        {
            return _BelowUnits;
        }
    }


    public Transform Units
    {
        get
        {
            return _Units;
        }
    }


    public Tilemap TileMarkers
    {
        get
        {
            return _TileMarkers;
        }
    }


    public Tilemap Fog
    {
        get
        {
            return _Fog;
        }
    }


    public Tilemap VFX
    {
        get
        {
            return _VFX;
        }
    }


    public int TileCount
    {
        get
        {
            if (_Tiles == null)
            {
                return 0;
            }


            return _Tiles.Length;
        }
    }


    public bool IsInsideBounds(
        Vector2Int Coordinate
    )
    {
        return
            Coordinate.x >= 0 &&
            Coordinate.y >= 0 &&
            Coordinate.x < _MapSize.x &&
            Coordinate.y < _MapSize.y;
    }


    private int GetTileIndex(
        Vector2Int Coordinate
    )
    {
        return
            Coordinate.y *
            _MapSize.x +
            Coordinate.x;
    }


    public bool TryGetTile(
        Vector2Int Coordinate,
        out MapTileAuthoringData Tile
    )
    {
        Tile =
            null;


        if (!IsInsideBounds(
                Coordinate) ||
            _Tiles == null)
        {
            return false;
        }


        int Index =
            GetTileIndex(
                Coordinate
            );


        if (Index < 0 ||
            Index >= _Tiles.Length)
        {
            return false;
        }


        Tile =
            _Tiles[Index];


        return
            Tile != null;
    }


    // Creates the initial Map canvas or expands an existing one.
    // Existing coordinates and their authored data are preserved exactly.
    public bool InitializeOrExpand(
        Vector2Int NewSize,
        out string ValidationMessage
    )
    {
        if (NewSize.x < 1 ||
            NewSize.y < 1)
        {
            ValidationMessage =
                "Map Width and Height must both be at least 1.";

            return false;
        }


        if (NewSize.x < _MapSize.x ||
            NewSize.y < _MapSize.y)
        {
            ValidationMessage =
                "Map resizing is expansion-only. Existing authored Map bounds cannot be reduced.";

            return false;
        }


        Vector2Int OldSize =
            _MapSize;

        MapTileAuthoringData[] OldTiles =
            _Tiles;


        MapTileAuthoringData[] NewTiles =
            new MapTileAuthoringData[
                NewSize.x *
                NewSize.y
            ];


        for (
            int Y = 0;
            Y < NewSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < NewSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                MapTileAuthoringData Tile =
                    null;


                bool ExistedPreviously =
                    X < OldSize.x &&
                    Y < OldSize.y &&
                    OldTiles != null;


                if (ExistedPreviously)
                {
                    int OldIndex =
                        Y *
                        OldSize.x +
                        X;


                    if (OldIndex >= 0 &&
                        OldIndex < OldTiles.Length)
                    {
                        Tile =
                            OldTiles[OldIndex];
                    }
                }


                if (Tile == null)
                {
                    Tile =
                        new MapTileAuthoringData(
                            Coordinate
                        );
                }
                else
                {
                    Tile.SetCoordinate(
                        Coordinate
                    );
                }


                int NewIndex =
                    Y *
                    NewSize.x +
                    X;


                NewTiles[NewIndex] =
                    Tile;
            }
        }


        _MapSize =
            NewSize;

        _Tiles =
            NewTiles;


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Initializes the Terrain layer without touching Visual Art or Conditions.
    public void PrimeMap()
    {
        EnsureTileStorage();


        for (
            int Y = 0;
            Y < _MapSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < _MapSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                TryGetTile(
                    Coordinate,
                    out MapTileAuthoringData Tile
                );


                bool IsEdge =
                    X == 0 ||
                    Y == 0 ||
                    X == _MapSize.x - 1 ||
                    Y == _MapSize.y - 1;


                Tile.SetTerrain(
                    IsEdge
                        ? MapTerrain.Barrier
                        : MapTerrain.Deep_Water
                );
            }
        }
    }


    public bool SetTerrain(
        Vector2Int Coordinate,
        MapTerrain Terrain
    )
    {
        if (!TryGetTile(
                Coordinate,
                out MapTileAuthoringData Tile))
        {
            return false;
        }


        Tile.SetTerrain(
            Terrain
        );


        return true;
    }


    // Player A and Player B deployment conditions are mutually exclusive.
    // Barrier tiles cannot receive a Deployment condition.
    public bool SetDeploymentCondition(
        Vector2Int Coordinate,
        MapTileCondition DeploymentCondition
    )
    {
        if (DeploymentCondition !=
                MapTileCondition.Deployment_Player_A &&
            DeploymentCondition !=
                MapTileCondition.Deployment_Player_B)
        {
            return false;
        }


        if (!TryGetTile(
                Coordinate,
                out MapTileAuthoringData Tile))
        {
            return false;
        }


        if (Tile.Terrain ==
            MapTerrain.Barrier)
        {
            return false;
        }


        Tile.RemoveCondition(
            MapTileCondition.Deployment_Player_A
        );

        Tile.RemoveCondition(
            MapTileCondition.Deployment_Player_B
        );


        Tile.AddCondition(
            DeploymentCondition
        );


        return true;
    }


    public bool ClearDeploymentCondition(
        Vector2Int Coordinate
    )
    {
        if (!TryGetTile(
                Coordinate,
                out MapTileAuthoringData Tile))
        {
            return false;
        }


        Tile.RemoveCondition(
            MapTileCondition.Deployment_Player_A
        );

        Tile.RemoveCondition(
            MapTileCondition.Deployment_Player_B
        );


        return true;
    }


    private void EnsureTileStorage()
    {
        int ExpectedCount =
            _MapSize.x *
            _MapSize.y;


        if (_Tiles != null &&
            _Tiles.Length ==
                ExpectedCount)
        {
            return;
        }


        Vector2Int CurrentSize =
            _MapSize;


        _MapSize =
            Vector2Int.one;


        _Tiles =
            null;


        InitializeOrExpand(
            CurrentSize,
            out _
        );
    }


    // Validates only authored Map data and visual coverage.
    // Runtime occupancy is intentionally not part of this authoring model.
    public bool ValidateMap(
        out string ValidationMessage
    )
    {
        if (_MapSize.x < 1 ||
            _MapSize.y < 1)
        {
            ValidationMessage =
                "Map has invalid Bounds.";

            return false;
        }


        if (_Grid == null)
        {
            ValidationMessage =
                "Map has no Grid reference.";

            return false;
        }


        if (_TerrainArt == null)
        {
            ValidationMessage =
                "Map has no Terrain Art Tilemap.";

            return false;
        }


        if (_BelowUnits == null)
        {
            ValidationMessage =
                "Map has no Below Units Tilemap.";

            return false;
        }


        if (_Units == null)
        {
            ValidationMessage =
                "Map has no Units visual root.";

            return false;
        }


        if (_TileMarkers == null)
        {
            ValidationMessage =
                "Map has no Tile Markers Tilemap.";

            return false;
        }


        if (_Fog == null)
        {
            ValidationMessage =
                "Map has no Fog Tilemap.";

            return false;
        }


        if (_VFX == null)
        {
            ValidationMessage =
                "Map has no VFX Tilemap.";

            return false;
        }


        int ExpectedCount =
            _MapSize.x *
            _MapSize.y;


        if (_Tiles == null ||
            _Tiles.Length !=
                ExpectedCount)
        {
            ValidationMessage =
                $"Map Tile storage is invalid. Expected {ExpectedCount} Tiles.";

            return false;
        }


        int MissingTerrainCount =
            0;

        int MissingVisualCount =
            0;

        int InvalidConditionCount =
            0;

        int BarrierDeploymentCount =
            0;


        Vector2Int FirstMissingTerrain =
            default;

        Vector2Int FirstMissingVisual =
            default;

        Vector2Int FirstBarrierDeployment =
            default;

        string FirstConditionError =
            string.Empty;


        for (
            int Y = 0;
            Y < _MapSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < _MapSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                int Index =
                    GetTileIndex(
                        Coordinate
                    );


                MapTileAuthoringData Tile =
                    _Tiles[Index];


                if (Tile == null)
                {
                    ValidationMessage =
                        $"Map Tile {Coordinate} is missing its authored data.";

                    return false;
                }


                if (Tile.Coordinate !=
                    Coordinate)
                {
                    ValidationMessage =
                        $"Map Tile storage mismatch at {Coordinate}.";

                    return false;
                }


                if (Tile.Terrain ==
                    MapTerrain.None)
                {
                    if (MissingTerrainCount ==
                        0)
                    {
                        FirstMissingTerrain =
                            Coordinate;
                    }


                    MissingTerrainCount++;
                }


                if (!_TerrainArt.HasTile(
                        new Vector3Int(
                            X,
                            Y,
                            0
                        )))
                {
                    if (MissingVisualCount ==
                        0)
                    {
                        FirstMissingVisual =
                            Coordinate;
                    }


                    MissingVisualCount++;
                }


                if (!Tile.ValidateConditions(
                        out string ConditionMessage))
                {
                    if (InvalidConditionCount ==
                        0)
                    {
                        FirstConditionError =
                            ConditionMessage;
                    }


                    InvalidConditionCount++;
                }


                bool HasDeployment =
                    Tile.HasCondition(
                        MapTileCondition.Deployment_Player_A
                    ) ||
                    Tile.HasCondition(
                        MapTileCondition.Deployment_Player_B
                    );


                if (Tile.Terrain ==
                        MapTerrain.Barrier &&
                    HasDeployment)
                {
                    if (BarrierDeploymentCount ==
                        0)
                    {
                        FirstBarrierDeployment =
                            Coordinate;
                    }


                    BarrierDeploymentCount++;
                }
            }
        }


        int OutsideVisualCount =
            0;

        Vector3Int FirstOutsideVisual =
            default;


        foreach (
            Vector3Int Cell
            in _TerrainArt.cellBounds.allPositionsWithin)
        {
            if (!_TerrainArt.HasTile(
                    Cell))
            {
                continue;
            }


            Vector2Int Coordinate =
                new Vector2Int(
                    Cell.x,
                    Cell.y
                );


            if (!IsInsideBounds(
                    Coordinate))
            {
                if (OutsideVisualCount ==
                    0)
                {
                    FirstOutsideVisual =
                        Cell;
                }


                OutsideVisualCount++;
            }
        }


        if (MissingTerrainCount > 0 ||
            MissingVisualCount > 0 ||
            InvalidConditionCount > 0 ||
            BarrierDeploymentCount > 0 ||
            OutsideVisualCount > 0)
        {
            ValidationMessage =
                "Map validation failed.";


            if (MissingTerrainCount > 0)
            {
                ValidationMessage +=
                    $"\nMissing Terrain: {MissingTerrainCount} " +
                    $"(first: {FirstMissingTerrain})";
            }


            if (MissingVisualCount > 0)
            {
                ValidationMessage +=
                    $"\nMissing Terrain Art: {MissingVisualCount} " +
                    $"(first: {FirstMissingVisual})";
            }


            if (InvalidConditionCount > 0)
            {
                ValidationMessage +=
                    $"\nInvalid Tile Conditions: {InvalidConditionCount} " +
                    $"(first: {FirstConditionError})";
            }


            if (BarrierDeploymentCount > 0)
            {
                ValidationMessage +=
                    $"\nBarrier Tiles With Deployment Conditions: {BarrierDeploymentCount} " +
                    $"(first: {FirstBarrierDeployment})";
            }


            if (OutsideVisualCount > 0)
            {
                ValidationMessage +=
                    $"\nTerrain Art Outside Map Bounds: {OutsideVisualCount} " +
                    $"(first: {FirstOutsideVisual})";
            }


            return false;
        }


        ValidationMessage =
            $"Map is valid. {_MapSize.x} × {_MapSize.y} = {ExpectedCount} authored Tiles.";

        return true;
    }
}