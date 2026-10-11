using UnityEngine;
using UnityEngine.Tilemaps;

// Attach To [Map Prefab Root] GameObject
// Owns The Authored Map Data And References Used By Gameplay
public class MapBoard : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private Grid _Grid;

    [SerializeField] private Tilemap _TerrainTilemap;
    [SerializeField] private Tilemap _StatesTilemap;
    [SerializeField] private Tilemap _MarkingsTilemap;
    [SerializeField] private Tilemap _FogTilemap;

    [SerializeField] private Transform _UnitRoot;
    [SerializeField] private Transform _WorldEffectsRoot;


    [Header("Board")]
    [SerializeField] private Vector2Int _TilemapOrigin;

    [SerializeField] private Vector2Int _BoardSize =
        new Vector2Int(60, 30);


    [HideInInspector]
    [SerializeField] private TerrainType[] _TerrainData;


    public Grid Grid
    {
        get { return _Grid; }
    }

    public Tilemap TerrainTilemap
    {
        get { return _TerrainTilemap; }
    }

    public Tilemap StatesTilemap
    {
        get { return _StatesTilemap; }
    }

    public Tilemap MarkingsTilemap
    {
        get { return _MarkingsTilemap; }
    }

    public Tilemap FogTilemap
    {
        get { return _FogTilemap; }
    }

    public Transform UnitRoot
    {
        get { return _UnitRoot; }
    }

    public Transform WorldEffectsRoot
    {
        get { return _WorldEffectsRoot; }
    }

    public Vector2Int TilemapOrigin
    {
        get { return _TilemapOrigin; }
    }

    public Vector2Int BoardSize
    {
        get { return _BoardSize; }
    }

    public bool HasTerrainData
    {
        get
        {
            return
                _TerrainData != null &&
                _TerrainData.Length ==
                _BoardSize.x * _BoardSize.y;
        }
    }


    public bool IsWithinBoard(
        Vector2Int BoardCoordinate
    )
    {
        return
            BoardCoordinate.x >= 0 &&
            BoardCoordinate.y >= 0 &&
            BoardCoordinate.x < _BoardSize.x &&
            BoardCoordinate.y < _BoardSize.y;
    }


    public Vector3Int BoardToTilemapCell(
        Vector2Int BoardCoordinate
    )
    {
        return new Vector3Int(
            _TilemapOrigin.x + BoardCoordinate.x,
            _TilemapOrigin.y + BoardCoordinate.y,
            0
        );
    }


    public Vector2Int TilemapCellToBoard(
        Vector3Int TilemapCell
    )
    {
        return new Vector2Int(
            TilemapCell.x - _TilemapOrigin.x,
            TilemapCell.y - _TilemapOrigin.y
        );
    }


    public bool TryGetTerrainType(
        Vector2Int BoardCoordinate,
        out TerrainType Terrain
    )
    {
        EnsureTerrainData();

        if (!IsWithinBoard(BoardCoordinate))
        {
            Terrain = default;
            return false;
        }

        Terrain =
            _TerrainData[
                GetTerrainIndex(
                    BoardCoordinate
                )
            ];

        return true;
    }


    public void SetTerrainType(
        Vector2Int BoardCoordinate,
        TerrainType Terrain
    )
    {
        EnsureTerrainData();

        if (!IsWithinBoard(BoardCoordinate))
        {
            return;
        }

        _TerrainData[
            GetTerrainIndex(
                BoardCoordinate
            )
        ] = Terrain;
    }


    public void FillTerrain(
        TerrainType Terrain
    )
    {
        EnsureTerrainData();

        for (
            int i = 0;
            i < _TerrainData.Length;
            i++
        )
        {
            _TerrainData[i] = Terrain;
        }
    }


    public void InitializeTerrainData()
    {
        _BoardSize.x =
            Mathf.Max(
                1,
                _BoardSize.x
            );

        _BoardSize.y =
            Mathf.Max(
                1,
                _BoardSize.y
            );

        _TerrainData =
            new TerrainType[
                _BoardSize.x *
                _BoardSize.y
            ];

        FillTerrain(
            TerrainType.DeepWater
        );
    }


    public Vector3 BoardToWorldCenter(
        Vector2Int BoardCoordinate
    )
    {
        return
            _TerrainTilemap.GetCellCenterWorld(
                BoardToTilemapCell(
                    BoardCoordinate
                )
            );
    }


    public Vector2Int WorldToBoard(
        Vector3 WorldPosition
    )
    {
        Vector3Int TilemapCell =
            _TerrainTilemap.WorldToCell(
                WorldPosition
            );

        return
            TilemapCellToBoard(
                TilemapCell
            );
    }


    public Bounds GetWorldBounds()
    {
        Vector3 BottomLeft =
            _TerrainTilemap.CellToWorld(
                BoardToTilemapCell(
                    Vector2Int.zero
                )
            );

        Vector3 TopRight =
            _TerrainTilemap.CellToWorld(
                BoardToTilemapCell(
                    _BoardSize
                )
            );

        Vector3 Center =
            (BottomLeft + TopRight) *
            0.5f;

        Vector3 Size =
            new Vector3(
                Mathf.Abs(
                    TopRight.x -
                    BottomLeft.x
                ),

                Mathf.Abs(
                    TopRight.y -
                    BottomLeft.y
                ),

                0f
            );

        return new Bounds(
            Center,
            Size
        );
    }


    private int GetTerrainIndex(
        Vector2Int BoardCoordinate
    )
    {
        return
            BoardCoordinate.y *
            _BoardSize.x +
            BoardCoordinate.x;
    }


    private void EnsureTerrainData()
    {
        int RequiredSize =
            Mathf.Max(
                1,
                _BoardSize.x *
                _BoardSize.y
            );

        if (_TerrainData == null ||
            _TerrainData.Length != RequiredSize)
        {
            _TerrainData =
                new TerrainType[
                    RequiredSize
                ];
        }
    }
}