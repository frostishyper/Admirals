using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


// Renders one participant-bound visibility snapshot.
// Owns no visibility or gameplay truth.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class FogOfWarPresenter : MonoBehaviour
{
    private struct TerrainCellAppearance
    {
        public Color Color;
        public TileFlags Flags;

        public TerrainCellAppearance(
            Color Color,
            TileFlags Flags)
        {
            this.Color = Color;
            this.Flags = Flags;
        }
    }

    [Header("Fog Tiles")]
    [SerializeField]
    private TileBase _FogCenter;

    [SerializeField]
    private TileBase _FogNorth;

    [SerializeField]
    private TileBase _FogSouth;

    [SerializeField]
    private TileBase _FogEast;

    [SerializeField]
    private TileBase _FogWest;

    [SerializeField]
    private TileBase _FogNortheast;

    [SerializeField]
    private TileBase _FogNorthwest;

    [SerializeField]
    private TileBase _FogSoutheast;

    [SerializeField]
    private TileBase _FogSouthwest;

    [SerializeField]
    private TileBase _FogNortheastSouthwest;

    [SerializeField]
    private TileBase _FogNorthwestSoutheast;

    [SerializeField]
    private TileBase _FogCornerNortheast;

    [SerializeField]
    private TileBase _FogCornerNorthwest;

    [SerializeField]
    private TileBase _FogCornerSoutheast;

    [SerializeField]
    private TileBase _FogCornerSouthwest;

    [Header("Obscured Terrain")]
    [SerializeField]
    private Color _ObscuredTerrainMultiplier =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    private readonly Dictionary<Vector3Int, TerrainCellAppearance>
        _OriginalTerrainAppearance =
            new Dictionary<Vector3Int, TerrainCellAppearance>();

    private HumanGameplayInput _HumanGameplayInput;
    private GameParticipantAPI.ParticipantAccess _ParticipantAccess;
    private MapAuthoring _MapAuthoring;
    private Tilemap _TerrainArt;
    private Tilemap _Fog;
    private bool _PresentationInitialized;

    private IEnumerator Start()
    {
        _HumanGameplayInput =
            GetComponent<HumanGameplayInput>();

        if (!ValidateReferences())
            yield break;

        _HumanGameplayInput.ParticipantChanged +=
            HandleParticipantChanged;

        GameMaster GameMaster =
            _HumanGameplayInput.GameMaster;

        while (!GameMaster.InitializationComplete)
        {
            if (!GameMaster.enabled)
            {
                Debug.LogError(
                    "Fog Of War Presenter could not initialize because GameMaster initialization failed.",
                    this);
                yield break;
            }

            yield return null;
        }

        if (!_HumanGameplayInput.Initialize() ||
            !InitializePresentation())
            yield break;

        BindParticipantAccess();
        RefreshVisibility();
    }

    public bool RefreshVisibility()
    {
        if (!_PresentationInitialized ||
            _ParticipantAccess == null)
            return false;

        if (!_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Fog Of War Presenter could not read its participant snapshot: {ValidationMessage}",
                this);
            return false;
        }

        ApplyTerrainVisibility(Snapshot);
        PaintFog(Snapshot);
        return true;
    }

    private bool InitializePresentation()
    {
        GameMaster GameMaster =
            _HumanGameplayInput.GameMaster;

        _MapAuthoring =
            GameMaster != null
                ? GameMaster.MapAuthoring
                : null;

        if (_MapAuthoring == null ||
            _MapAuthoring.GridReference == null ||
            _MapAuthoring.TerrainArt == null ||
            _MapAuthoring.Fog == null)
        {
            Debug.LogError(
                "Fog Of War Presenter requires the runtime MapAuthoring Grid, Terrain Art and Fog Tilemaps.",
                this);
            return false;
        }

        _TerrainArt =
            _MapAuthoring.TerrainArt;
        _Fog =
            _MapAuthoring.Fog;

        CacheTerrainAppearance();
        _PresentationInitialized = true;
        return true;
    }

    private void CacheTerrainAppearance()
    {
        _OriginalTerrainAppearance.Clear();

        for (int y = 0; y < _MapAuthoring.MapSize.y; y++)
        {
            for (int x = 0; x < _MapAuthoring.MapSize.x; x++)
            {
                Vector3Int Cell =
                    new Vector3Int(x, y, 0);

                _OriginalTerrainAppearance[Cell] =
                    new TerrainCellAppearance(
                        _TerrainArt.GetColor(Cell),
                        _TerrainArt.GetTileFlags(Cell));
            }
        }
    }

    private void ApplyTerrainVisibility(
        ParticipantGameSnapshot Snapshot)
    {
        for (int i = 0; i < Snapshot.Tiles.Length; i++)
        {
            ParticipantTileSnapshot Tile =
                Snapshot.Tiles[i];
            Vector3Int Cell =
                new Vector3Int(
                    Tile.Coordinate.x,
                    Tile.Coordinate.y,
                    0);

            if (!_OriginalTerrainAppearance.TryGetValue(
                    Cell,
                    out TerrainCellAppearance Original))
                continue;

            _TerrainArt.SetTileFlags(
                Cell,
                Original.Flags & ~TileFlags.LockColor);

            Color Color =
                Tile.Visibility == ParticipantTileVisibility.Obscured
                    ? MultiplyColor(
                        Original.Color,
                        _ObscuredTerrainMultiplier)
                    : Original.Color;

            _TerrainArt.SetColor(Cell, Color);
        }

        _TerrainArt.RefreshAllTiles();
    }

    private void PaintFog(
        ParticipantGameSnapshot Snapshot)
    {
        bool[,] Revealed =
            new bool[
                _MapAuthoring.MapSize.x,
                _MapAuthoring.MapSize.y];

        for (int i = 0; i < Snapshot.Tiles.Length; i++)
        {
            ParticipantTileSnapshot Tile =
                Snapshot.Tiles[i];

            if (_MapAuthoring.IsInsideBounds(Tile.Coordinate))
            {
                Revealed[Tile.Coordinate.x, Tile.Coordinate.y] =
                    Tile.Visibility != ParticipantTileVisibility.Fog;
            }
        }

        _Fog.ClearAllTiles();

        for (int y = 0; y < _MapAuthoring.MapSize.y; y++)
        {
            for (int x = 0; x < _MapAuthoring.MapSize.x; x++)
            {
                TileBase FogTile =
                    GetPresentationFogTile(
                        Revealed,
                        x,
                        y);

                if (FogTile != null)
                {
                    _Fog.SetTile(
                        new Vector3Int(x, y, 0),
                        FogTile);
                }
            }
        }

        _Fog.RefreshAllTiles();
    }

    private TileBase GetPresentationFogTile(
        bool[,] Revealed,
        int X,
        int Y)
    {
        if (Revealed[X, Y])
            return null;

        bool North = IsRevealed(Revealed, X, Y + 1);
        bool South = IsRevealed(Revealed, X, Y - 1);
        bool East = IsRevealed(Revealed, X + 1, Y);
        bool West = IsRevealed(Revealed, X - 1, Y);

        bool Northeast =
            IsRevealed(Revealed, X + 1, Y + 1);
        bool Northwest =
            IsRevealed(Revealed, X - 1, Y + 1);
        bool Southwest =
            IsRevealed(Revealed, X - 1, Y - 1);
        bool Southeast =
            IsRevealed(Revealed, X + 1, Y - 1);

        int CardinalCount =
            (North ? 1 : 0) +
            (South ? 1 : 0) +
            (East ? 1 : 0) +
            (West ? 1 : 0);

        if (CardinalCount == 0)
            return _FogCenter;

        if (CardinalCount == 1)
        {
            if (South)
            {
                if (Southeast && !Southwest)
                    return _FogCornerSoutheast;

                if (Southwest && !Southeast)
                    return _FogCornerSouthwest;

                return _FogNorth;
            }

            if (North)
            {
                if (Northeast && !Northwest)
                    return _FogCornerNortheast;

                if (Northwest && !Northeast)
                    return _FogCornerNorthwest;

                return _FogSouth;
            }

            if (East)
            {
                if (Southeast && !Northeast)
                    return _FogCornerSoutheast;

                if (Northeast && !Southeast)
                    return _FogCornerNortheast;

                return _FogWest;
            }

            if (Southwest && !Northwest)
                return _FogCornerSouthwest;

            if (Northwest && !Southwest)
                return _FogCornerNorthwest;

            return _FogEast;
        }

        if (CardinalCount == 2)
        {
            if (North && East)
                return _FogSouthwest;

            if (North && West)
                return _FogSoutheast;

            if (South && East)
                return _FogNorthwest;

            if (South && West)
                return _FogNortheast;

            if (North && South)
                return _FogNortheastSouthwest;

            return _FogNorthwestSoutheast;
        }

        return _FogCenter;
    }

    private static bool IsRevealed(
        bool[,] Revealed,
        int X,
        int Y)
    {
        return
            X >= 0 &&
            Y >= 0 &&
            X < Revealed.GetLength(0) &&
            Y < Revealed.GetLength(1) &&
            Revealed[X, Y];
    }

    private void BindParticipantAccess()
    {
        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }

        _ParticipantAccess =
            _HumanGameplayInput.ParticipantAccess;

        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged +=
                HandleSnapshotChanged;
        }
    }

    private void HandleParticipantChanged()
    {
        BindParticipantAccess();

        if (_PresentationInitialized)
            RefreshVisibility();
    }

    private void HandleSnapshotChanged()
    {
        RefreshVisibility();
    }

    private bool ValidateReferences()
    {
        if (_HumanGameplayInput == null ||
            _HumanGameplayInput.GameMaster == null)
        {
            Debug.LogError(
                "Fog Of War Presenter requires Human Gameplay Input with a GameMaster reference.",
                this);
            return false;
        }

        if (_FogCenter == null ||
            _FogNorth == null ||
            _FogSouth == null ||
            _FogEast == null ||
            _FogWest == null ||
            _FogNortheast == null ||
            _FogNorthwest == null ||
            _FogSoutheast == null ||
            _FogSouthwest == null ||
            _FogNortheastSouthwest == null ||
            _FogNorthwestSoutheast == null ||
            _FogCornerNortheast == null ||
            _FogCornerNorthwest == null ||
            _FogCornerSoutheast == null ||
            _FogCornerSouthwest == null)
        {
            Debug.LogError(
                "Fog Of War Presenter requires all fifteen Fog Tile references.",
                this);
            return false;
        }

        return true;
    }

    private void RestorePresentation()
    {
        if (!_PresentationInitialized)
            return;

        foreach (
            KeyValuePair<Vector3Int, TerrainCellAppearance> Pair
            in _OriginalTerrainAppearance)
        {
            _TerrainArt.SetTileFlags(
                Pair.Key,
                Pair.Value.Flags & ~TileFlags.LockColor);
            _TerrainArt.SetColor(
                Pair.Key,
                Pair.Value.Color);
            _TerrainArt.SetTileFlags(
                Pair.Key,
                Pair.Value.Flags);
        }

        _TerrainArt.RefreshAllTiles();
        _Fog.ClearAllTiles();
        _PresentationInitialized = false;
    }

    private static Color MultiplyColor(
        Color A,
        Color B)
    {
        return new Color(
            A.r * B.r,
            A.g * B.g,
            A.b * B.b,
            A.a * B.a);
    }

    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
        {
            _HumanGameplayInput.ParticipantChanged -=
                HandleParticipantChanged;
        }

        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }

        RestorePresentation();
    }
}
