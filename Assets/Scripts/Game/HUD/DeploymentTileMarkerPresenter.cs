using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;


// Presentation-only renderer for initial Deployment markers.
//
// GameDeployment remains authoritative for tentative placement and selection.
// This component only visualizes participant-permitted state on Tile_Markers.
[DisallowMultipleComponent]
public sealed class DeploymentTileMarkerPresenter :
    MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;


    [Header("Animated Tiles")]
    [SerializeField]
    private TileBase _AvailableZoneTile;

    [FormerlySerializedAs("_SelectedFootprintTile")]
    [SerializeField]
    private TileBase _LegalFootprintTile;

    [SerializeField]
    private TileBase _IllegalFootprintTile;


    private readonly HashSet<Vector2Int>
        _OccupiedCoordinates =
            new HashSet<Vector2Int>();

    private readonly Dictionary<Vector3Int, TileBase>
        _DesiredTiles =
            new Dictionary<Vector3Int, TileBase>();

    private readonly Dictionary<Vector3Int, TileBase>
        _PaintedTiles =
            new Dictionary<Vector3Int, TileBase>();

    private readonly List<Vector3Int>
        _CellsToClear =
            new List<Vector3Int>();


    private RuntimeParticipant _Participant;

    private GameDeployment.ParticipantView _Plan;

    private Tilemap _TileMarkers;

    private Vector2Int[] _DeploymentZone =
        System.Array.Empty<Vector2Int>();


    private string _PreviewFleetEntryID =
        string.Empty;

    private Definition_Unit _PreviewUnit;

    private Vector2Int _PreviewMovementPivotCoordinate;

    private UnitFacing _PreviewFacing;

    private bool _PreviewLegal;

    private bool _PreviewVisible;


    private bool _Initialized;


    public bool Initialize(
        RuntimeParticipant Participant,
        GameDeployment.ParticipantView Plan
    )
    {
        if (_Initialized)
        {
            return
                ReferenceEquals(
                    _Participant,
                    Participant
                ) &&
                ReferenceEquals(
                    _Plan,
                    Plan
                );
        }


        if (!ValidateReferences())
        {
            return false;
        }


        _Participant =
            Participant;

        _Plan =
            Plan;


        if (_Participant == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter received no participant.",
                this
            );

            return false;
        }


        if (_Plan == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter received no participant-filtered Deployment view.",
                this
            );

            return false;
        }


        if (!_GameMaster.TryGetDeploymentZoneCoordinates(
                _Participant,
                out _DeploymentZone))
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter could not read Participant A's Deployment zone.",
                this
            );

            return false;
        }


        if (_GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.TileMarkers == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter could not find the runtime Map's Tile_Markers layer.",
                this
            );

            return false;
        }


        _TileMarkers =
            _GameMaster.MapAuthoring.TileMarkers;

        _Initialized =
            true;


        RefreshMarkers();

        return true;
    }


    public void ShowDragPreview(
        string FleetEntryID,
        Definition_Unit Unit,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        bool IsLegal
    )
    {
        if (!_Initialized ||
            string.IsNullOrWhiteSpace(
                FleetEntryID) ||
            Unit == null)
        {
            ClearDragPreview();

            return;
        }


        _PreviewFleetEntryID =
            FleetEntryID;

        _PreviewUnit =
            Unit;

        _PreviewMovementPivotCoordinate =
            MovementPivotCoordinate;

        _PreviewFacing =
            Facing;

        _PreviewLegal =
            IsLegal;

        _PreviewVisible =
            true;


        RefreshMarkers();
    }


    public void ClearDragPreview()
    {
        ClearDragPreviewState();


        if (_Initialized &&
            _GameMaster.CurrentPhase ==
                GameplayPhase.Deployment)
        {
            RefreshMarkers();
        }
    }


    private void LateUpdate()
    {
        if (!_Initialized)
        {
            return;
        }


        if (_GameMaster.CurrentPhase !=
            GameplayPhase.Deployment)
        {
            ClearDragPreviewState();
            ClearPaintedTiles();

            return;
        }


        RefreshMarkers();
    }


    private void RefreshMarkers()
    {
        if (_Plan == null ||
            _TileMarkers == null)
        {
            return;
        }


        _OccupiedCoordinates.Clear();
        _DesiredTiles.Clear();


        IReadOnlyList<GameDeployment.PlacementView> Placements =
            _Plan.Placements;


        for (int PlacementIndex = 0;
            PlacementIndex < Placements.Count;
            PlacementIndex++)
        {
            GameDeployment.PlacementView Placement =
                Placements[PlacementIndex];


            if (Placement == null ||
                !Placement.IsPlaced ||
                !Placement.HasFacing)
            {
                continue;
            }


            bool IsMovingPreviewedPlacement =
                _PreviewVisible &&
                Placement.ID ==
                    _PreviewFleetEntryID;


            if (IsMovingPreviewedPlacement)
            {
                continue;
            }


            if (!_GameMaster.TryGetDeploymentPlacementTiles(
                    _Participant,
                    Placement.ID,
                    out RuntimeUnitTilePlacement[] PlacementTiles,
                    out _))
            {
                continue;
            }


            for (int TileIndex = 0;
                TileIndex < PlacementTiles.Length;
                TileIndex++)
            {
                _OccupiedCoordinates.Add(
                    PlacementTiles[TileIndex].MapCoordinate
                );
            }
        }


        for (int ZoneIndex = 0;
            ZoneIndex < _DeploymentZone.Length;
            ZoneIndex++)
        {
            Vector2Int Coordinate =
                _DeploymentZone[ZoneIndex];


            if (_OccupiedCoordinates.Contains(
                    Coordinate))
            {
                continue;
            }


            SetDesiredTile(
                Coordinate,
                _AvailableZoneTile
            );
        }


        if (_PreviewVisible)
        {
            PaintDragPreview();
        }
        else
        {
            PaintSelectedFootprint();
        }


        ApplyDesiredTiles();
    }


    private void PaintSelectedFootprint()
    {
        GameDeployment.PlacementView SelectedPlacement =
            _Plan.SelectedPlacement;


        if (SelectedPlacement == null ||
            !SelectedPlacement.IsPlaced ||
            !SelectedPlacement.HasFacing)
        {
            return;
        }


        if (!_GameMaster.TryGetDeploymentPlacementTiles(
                _Participant,
                SelectedPlacement.ID,
                out RuntimeUnitTilePlacement[] SelectedTiles,
                out _))
        {
            return;
        }


        for (int TileIndex = 0;
            TileIndex < SelectedTiles.Length;
            TileIndex++)
        {
            SetDesiredTile(
                SelectedTiles[TileIndex].MapCoordinate,
                _LegalFootprintTile
            );
        }
    }


    private void PaintDragPreview()
    {
        if (!UnitFootprintGeometry.TryBuildPlacement(
                _PreviewUnit,
                _PreviewMovementPivotCoordinate,
                _PreviewFacing,
                out RuntimeUnitTilePlacement[] PreviewTiles,
                out _))
        {
            return;
        }


        TileBase PreviewTile =
            _PreviewLegal
                ? _LegalFootprintTile
                : _IllegalFootprintTile;


        for (int TileIndex = 0;
            TileIndex < PreviewTiles.Length;
            TileIndex++)
        {
            Vector2Int Coordinate =
                PreviewTiles[TileIndex].MapCoordinate;


            if (!_GameMaster.GameMap.IsInsideBounds(
                    Coordinate))
            {
                continue;
            }


            SetDesiredTile(
                Coordinate,
                PreviewTile
            );
        }
    }


    private void SetDesiredTile(
        Vector2Int Coordinate,
        TileBase Tile
    )
    {
        if (Tile == null)
        {
            return;
        }


        Vector3Int Cell =
            new Vector3Int(
                Coordinate.x,
                Coordinate.y,
                0
            );


        _DesiredTiles[Cell] =
            Tile;
    }


    private void ApplyDesiredTiles()
    {
        _CellsToClear.Clear();


        foreach (KeyValuePair<Vector3Int, TileBase> Painted
            in _PaintedTiles)
        {
            if (!_DesiredTiles.ContainsKey(
                    Painted.Key))
            {
                _CellsToClear.Add(
                    Painted.Key
                );
            }
        }


        for (int Index = 0;
            Index < _CellsToClear.Count;
            Index++)
        {
            Vector3Int Cell =
                _CellsToClear[Index];


            _TileMarkers.SetTile(
                Cell,
                null
            );


            _PaintedTiles.Remove(
                Cell
            );
        }


        foreach (KeyValuePair<Vector3Int, TileBase> Desired
            in _DesiredTiles)
        {
            if (_PaintedTiles.TryGetValue(
                    Desired.Key,
                    out TileBase CurrentTile) &&
                CurrentTile == Desired.Value)
            {
                continue;
            }


            _TileMarkers.SetTile(
                Desired.Key,
                Desired.Value
            );


            _PaintedTiles[Desired.Key] =
                Desired.Value;
        }
    }


    private void ClearPaintedTiles()
    {
        if (_TileMarkers != null)
        {
            foreach (KeyValuePair<Vector3Int, TileBase> Painted
                in _PaintedTiles)
            {
                _TileMarkers.SetTile(
                    Painted.Key,
                    null
                );
            }
        }


        _PaintedTiles.Clear();
        _DesiredTiles.Clear();
        _OccupiedCoordinates.Clear();
        _CellsToClear.Clear();
    }


    private void ClearDragPreviewState()
    {
        _PreviewFleetEntryID =
            string.Empty;

        _PreviewUnit =
            null;

        _PreviewMovementPivotCoordinate =
            default;

        _PreviewFacing =
            default;

        _PreviewLegal =
            false;

        _PreviewVisible =
            false;
    }


    private bool ValidateReferences()
    {
        if (_GameMaster == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter has no GameMaster.",
                this
            );

            return false;
        }


        if (_AvailableZoneTile == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter has no available-zone Animated Tile.",
                this
            );

            return false;
        }


        if (_LegalFootprintTile == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter has no legal-footprint Animated Tile.",
                this
            );

            return false;
        }


        if (_IllegalFootprintTile == null)
        {
            Debug.LogError(
                "Deployment Tile Marker Presenter has no illegal-footprint Animated Tile.",
                this
            );

            return false;
        }


        return true;
    }


    private void OnDisable()
    {
        ClearDragPreviewState();
        ClearPaintedTiles();
    }


    private void OnDestroy()
    {
        ClearDragPreviewState();
        ClearPaintedTiles();
    }
}
