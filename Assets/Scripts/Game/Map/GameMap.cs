using System;
using System.Collections.Generic;
using UnityEngine;


// Describes one requested occupied Tile for a Unit footprint.
//
// GameMaster calculates these coordinates from the Unit's position,
// facing, pivot, and footprint. GameMap only verifies whether the
// resulting physical placement is legal.
public struct RuntimeUnitTilePlacement
{
    public Vector2Int MapCoordinate
    {
        get;
        private set;
    }


    public Vector2Int FootprintCoordinate
    {
        get;
        private set;
    }


    public RuntimeUnitTilePlacement(
        Vector2Int MapCoordinate,
        Vector2Int FootprintCoordinate
    )
    {
        this.MapCoordinate =
            MapCoordinate;

        this.FootprintCoordinate =
            FootprintCoordinate;
    }
}


// Runtime spatial record stored on one occupied Tile.
//
// The actual mutable Unit state belongs to GameMaster/GameUnits.
// GameMap stores a reference to the RuntimeUnit occupying this Tile
// plus which local footprint segment occupies it.
public sealed class RuntimeUnitOccupancy
{
    public RuntimeUnit Unit
    {
        get;
        private set;
    }


    public Vector2Int FootprintCoordinate
    {
        get;
        private set;
    }


    public RuntimeParticipant Owner
    {
        get
        {
            return
                Unit != null
                    ? Unit.Owner
                    : null;
        }
    }


    public Domain Domain
    {
        get
        {
            return
                Unit != null
                    ? Unit.CurrentDomain
                    : Domain.None;
        }
    }


    internal RuntimeUnitOccupancy(
        RuntimeUnit Unit,
        Vector2Int FootprintCoordinate
    )
    {
        this.Unit =
            Unit;

        this.FootprintCoordinate =
            FootprintCoordinate;
    }
}


// Runtime spatial record for one Mine.
//
// GameMaster owns the Mine's gameplay meaning/effects.
// The Map owns where the Mine physically exists.
public sealed class RuntimeMineOccupancy
{
    public string MineInstanceID
    {
        get;
        private set;
    }


    public RuntimeParticipant Owner
    {
        get;
        private set;
    }


    internal RuntimeMineOccupancy(
        string MineInstanceID,
        RuntimeParticipant Owner
    )
    {
        this.MineInstanceID =
            MineInstanceID;

        this.Owner =
            Owner;
    }
}


internal sealed class RuntimeUnitPlacementRequest
{
    public RuntimeUnit Unit
    {
        get;
    }


    public RuntimeUnitTilePlacement[] Placements
    {
        get;
    }


    public MapTileCondition RequiredAuthoredCondition
    {
        get;
    }


    internal RuntimeUnitPlacementRequest(
        RuntimeUnit Unit,
        RuntimeUnitTilePlacement[] Placements,
        MapTileCondition RequiredAuthoredCondition
    )
    {
        this.Unit =
            Unit;

        this.Placements =
            Placements;

        this.RequiredAuthoredCondition =
            RequiredAuthoredCondition;
    }
}


internal sealed class PreparedMapUnitDeployment
{
    public RuntimeUnitPlacementRequest[] Requests
    {
        get;
    }


    public RuntimeMineOccupancy[] TriggeredMines
    {
        get;
    }


    internal PreparedMapUnitDeployment(
        RuntimeUnitPlacementRequest[] Requests,
        RuntimeMineOccupancy[] TriggeredMines
    )
    {
        this.Requests =
            Requests;

        this.TriggeredMines =
            TriggeredMines;
    }
}


// One authoritative live Tile in the current Match.
public sealed class RuntimeMapTile
{
    private readonly HashSet<MapTileCondition>
        _AuthoredConditions;

    private readonly HashSet<MapTileCondition>
        _DynamicConditions;


    public Vector2Int Coordinate
    {
        get;
        private set;
    }


    public MapTerrain Terrain
    {
        get;
        private set;
    }


    public RuntimeUnitOccupancy UnitOccupancy
    {
        get;
        private set;
    }


    public RuntimeMineOccupancy Mine
    {
        get;
        private set;
    }


    internal RuntimeMapTile(
        Vector2Int Coordinate,
        MapTerrain Terrain,
        MapTileCondition[] AuthoredConditions
    )
    {
        this.Coordinate =
            Coordinate;

        this.Terrain =
            Terrain;


        _AuthoredConditions =
            new HashSet<MapTileCondition>();

        _DynamicConditions =
            new HashSet<MapTileCondition>();


        if (AuthoredConditions == null)
        {
            return;
        }


        for (
            int i = 0;
            i < AuthoredConditions.Length;
            i++
        )
        {
            MapTileCondition Condition =
                AuthoredConditions[i];


            if (Condition ==
                MapTileCondition.None)
            {
                continue;
            }


            _AuthoredConditions.Add(
                Condition
            );
        }
    }


    public bool HasAuthoredCondition(
        MapTileCondition Condition
    )
    {
        return
            Condition !=
                MapTileCondition.None &&
            _AuthoredConditions.Contains(
                Condition
            );
    }


    public bool HasDynamicCondition(
        MapTileCondition Condition
    )
    {
        return
            Condition !=
                MapTileCondition.None &&
            _DynamicConditions.Contains(
                Condition
            );
    }


    public bool HasCondition(
        MapTileCondition Condition
    )
    {
        return
            HasAuthoredCondition(
                Condition
            ) ||
            HasDynamicCondition(
                Condition
            );
    }


    public MapTileCondition[] GetAuthoredConditions()
    {
        MapTileCondition[] Conditions =
            new MapTileCondition[
                _AuthoredConditions.Count
            ];


        _AuthoredConditions.CopyTo(
            Conditions
        );


        return
            Conditions;
    }


    public MapTileCondition[] GetDynamicConditions()
    {
        MapTileCondition[] Conditions =
            new MapTileCondition[
                _DynamicConditions.Count
            ];


        _DynamicConditions.CopyTo(
            Conditions
        );


        return
            Conditions;
    }


    internal bool AddDynamicCondition(
        MapTileCondition Condition
    )
    {
        if (Condition ==
            MapTileCondition.None)
        {
            return false;
        }


        return
            _DynamicConditions.Add(
                Condition
            );
    }


    internal bool RemoveDynamicCondition(
        MapTileCondition Condition
    )
    {
        return
            _DynamicConditions.Remove(
                Condition
            );
    }


    internal void SetUnitOccupancy(
        RuntimeUnitOccupancy Occupancy
    )
    {
        UnitOccupancy =
            Occupancy;
    }


    internal void ClearUnitOccupancy()
    {
        UnitOccupancy =
            null;
    }


    internal void SetMine(
        RuntimeMineOccupancy Mine
    )
    {
        this.Mine =
            Mine;
    }


    internal void ClearMine()
    {
        Mine =
            null;
    }
}


// Runtime authoritative Map state.
//
// The authored Map prefab supplies the immutable starting board.
// GameMap creates its own live Tile state from that authored data.
//
// GameMaster remains the authority that decides what actions may occur.
// GameMap owns the physical board and validates whether requested
// spatial changes are actually possible.
public class GameMap :
    MonoBehaviour
{
    private MapAuthoring _AuthoringMap;

    private Vector2Int _MapSize;

    private RuntimeMapTile[] _Tiles;


    // RuntimeUnit reference -> occupied Map coordinates.
    //
    // Unit identity is intentionally NOT represented by a globally
    // unique string. Participant A and Participant B may both have
    // Units named "Rover Class 1" without conflicting.
    private readonly Dictionary<RuntimeUnit, List<Vector2Int>>
        _UnitCoordinates =
            new Dictionary<RuntimeUnit, List<Vector2Int>>();


    // Mine Instance ID -> Map coordinate.
    private readonly Dictionary<string, Vector2Int>
        _MineCoordinates =
            new Dictionary<string, Vector2Int>();


    private bool _Initialized;


    public MapAuthoring AuthoringMap
    {
        get
        {
            return _AuthoringMap;
        }
    }


    public Vector2Int MapSize
    {
        get
        {
            return _MapSize;
        }
    }


    public bool Initialized
    {
        get
        {
            return _Initialized;
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


            return
                _Tiles.Length;
        }
    }


    public bool Initialize(
        MapAuthoring AuthoringMap,
        out string ValidationMessage
    )
    {
        if (AuthoringMap == null)
        {
            ValidationMessage =
                "GameMap has no authored Map source.";

            return false;
        }


        if (!AuthoringMap.ValidateMap(
                out string MapValidationMessage))
        {
            ValidationMessage =
                $"Authored Map is invalid: {MapValidationMessage}";

            return false;
        }


        _AuthoringMap =
            AuthoringMap;

        _MapSize =
            AuthoringMap.MapSize;


        _Tiles =
            new RuntimeMapTile[
                _MapSize.x *
                _MapSize.y
            ];


        _UnitCoordinates.Clear();
        _MineCoordinates.Clear();


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


                if (!AuthoringMap.TryGetTile(
                        Coordinate,
                        out MapTileAuthoringData AuthoredTile))
                {
                    ValidationMessage =
                        $"Authored Tile {Coordinate} could not be read.";

                    ClearRuntimeState();

                    return false;
                }


                int Index =
                    GetTileIndex(
                        Coordinate
                    );


                _Tiles[Index] =
                    new RuntimeMapTile(
                        Coordinate,
                        AuthoredTile.Terrain,
                        AuthoredTile.Conditions
                    );
            }
        }


        _Initialized =
            true;


        ValidationMessage =
            string.Empty;

        return true;
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
        out RuntimeMapTile Tile
    )
    {
        Tile =
            null;


        if (!_Initialized ||
            !IsInsideBounds(
                Coordinate))
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


    public bool TerrainAllowsDomain(
        MapTerrain Terrain,
        Domain UnitDomain
    )
    {
        // A Unit physically occupies exactly one Domain at a time.
        if (UnitDomain != Domain.Air &&
            UnitDomain != Domain.Surface &&
            UnitDomain != Domain.Subsurface)
        {
            return false;
        }


        switch (Terrain)
        {
            case MapTerrain.Deep_Water:

                return
                    UnitDomain == Domain.Air ||
                    UnitDomain == Domain.Surface ||
                    UnitDomain == Domain.Subsurface;


            case MapTerrain.Shallow_Water:

                return
                    UnitDomain == Domain.Air ||
                    UnitDomain == Domain.Surface;


            case MapTerrain.Land:

                return
                    UnitDomain == Domain.Air;


            case MapTerrain.Barrier:

                return false;


            default:

                return false;
        }
    }


    public Vector2Int WorldToCoordinate(
        Vector3 WorldPosition
    )
    {
        if (_AuthoringMap == null ||
            _AuthoringMap.GridReference == null)
        {
            return
                new Vector2Int(
                    int.MinValue,
                    int.MinValue
                );
        }


        Vector3Int Cell =
            _AuthoringMap.GridReference.WorldToCell(
                WorldPosition
            );


        return
            new Vector2Int(
                Cell.x,
                Cell.y
            );
    }


    public Vector3 CoordinateToWorldCenter(
        Vector2Int Coordinate
    )
    {
        if (_AuthoringMap == null ||
            _AuthoringMap.GridReference == null)
        {
            return
                Vector3.zero;
        }


        return
            _AuthoringMap.GridReference.GetCellCenterWorld(
                new Vector3Int(
                    Coordinate.x,
                    Coordinate.y,
                    0
                )
            );
    }


    // Checks the complete requested Unit footprint without modifying the board.
    //
    // RequiredAuthoredCondition is used for board rules such as Deployment.
    // Pass None when no authored Tile Condition is required.
    public bool ValidateUnitPlacement(
        RuntimeUnit Unit,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        MapTileCondition RequiredAuthoredCondition,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        return
            ValidateUnitPlacementInternal(
                Unit,
                Placements,
                RequiredAuthoredCondition,
                true,
                out TriggeredMines,
                out ValidationMessage
            );
    }


    // Checks a tentative Unit Definition footprint without creating a
    // RuntimeUnit or modifying committed board occupancy.
    //
    // Initial Deployment uses this path because RuntimeUnits must not exist
    // before the complete participant plan is confirmed.
    public bool ValidateUnitPlacement(
        Definition_Unit UnitDefinition,
        RuntimeParticipant Owner,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        MapTileCondition RequiredAuthoredCondition,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        TriggeredMines =
            Array.Empty<RuntimeMineOccupancy>();


        if (UnitDefinition == null)
        {
            ValidationMessage =
                "Unit Definition is missing.";

            return false;
        }


        if (Owner == null)
        {
            ValidationMessage =
                $"Unit '{UnitDefinition.Name}' has no owning Participant.";

            return false;
        }


        return
            ValidateUnitPlacementInternal(
                null,
                UnitDefinition.Name,
                Owner,
                UnitDefinition.BaseDomain,
                Placements,
                RequiredAuthoredCondition,
                false,
                out TriggeredMines,
                out ValidationMessage
            );
    }


    private bool ValidateUnitPlacementInternal(
        RuntimeUnit Unit,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        MapTileCondition RequiredAuthoredCondition,
        bool AllowExistingOccupancyFromSameUnit,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        TriggeredMines =
            Array.Empty<RuntimeMineOccupancy>();


        if (Unit == null)
        {
            ValidationMessage =
                "Runtime Unit is missing.";

            return false;
        }


        return
            ValidateUnitPlacementInternal(
                Unit,
                Unit.InstanceName,
                Unit.Owner,
                Unit.CurrentDomain,
                Placements,
                RequiredAuthoredCondition,
                AllowExistingOccupancyFromSameUnit,
                out TriggeredMines,
                out ValidationMessage
            );
    }


    private bool ValidateUnitPlacementInternal(
        RuntimeUnit ExistingUnit,
        string UnitName,
        RuntimeParticipant Owner,
        Domain UnitDomain,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        MapTileCondition RequiredAuthoredCondition,
        bool AllowExistingOccupancyFromSameUnit,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        TriggeredMines =
            Array.Empty<RuntimeMineOccupancy>();


        if (!_Initialized)
        {
            ValidationMessage =
                "GameMap has not been initialized.";

            return false;
        }


        if (Owner == null)
        {
            ValidationMessage =
                $"Unit '{UnitName}' has no owning Participant.";

            return false;
        }


        if (UnitDomain != Domain.Air &&
            UnitDomain != Domain.Surface &&
            UnitDomain != Domain.Subsurface)
        {
            ValidationMessage =
                $"Unit '{UnitName}' has invalid Domain '{UnitDomain}'.";

            return false;
        }


        if (Placements == null ||
            Placements.Count == 0)
        {
            ValidationMessage =
                $"Unit '{UnitName}' has no requested occupied Tiles.";

            return false;
        }


        HashSet<Vector2Int> UsedMapCoordinates =
            new HashSet<Vector2Int>();

        HashSet<Vector2Int> UsedFootprintCoordinates =
            new HashSet<Vector2Int>();

        List<RuntimeMineOccupancy> Mines =
            new List<RuntimeMineOccupancy>();


        for (
            int i = 0;
            i < Placements.Count;
            i++
        )
        {
            RuntimeUnitTilePlacement Placement =
                Placements[i];


            if (!UsedMapCoordinates.Add(
                    Placement.MapCoordinate))
            {
                ValidationMessage =
                    $"Unit '{UnitName}' requests Map Tile " +
                    $"{Placement.MapCoordinate} more than once.";

                return false;
            }


            if (!UsedFootprintCoordinates.Add(
                    Placement.FootprintCoordinate))
            {
                ValidationMessage =
                    $"Unit '{UnitName}' uses footprint segment " +
                    $"{Placement.FootprintCoordinate} more than once.";

                return false;
            }


            if (!TryGetTile(
                    Placement.MapCoordinate,
                    out RuntimeMapTile Tile))
            {
                ValidationMessage =
                    $"Unit '{UnitName}' would extend outside the Map at " +
                    $"{Placement.MapCoordinate}.";

                return false;
            }


            if (!TerrainAllowsDomain(
                    Tile.Terrain,
                    UnitDomain))
            {
                ValidationMessage =
                    $"Unit '{UnitName}' Domain '{UnitDomain}' cannot occupy " +
                    $"{Tile.Terrain} at {Placement.MapCoordinate}.";

                return false;
            }


            if (RequiredAuthoredCondition !=
                    MapTileCondition.None &&
                !Tile.HasAuthoredCondition(
                    RequiredAuthoredCondition))
            {
                ValidationMessage =
                    $"Tile {Placement.MapCoordinate} does not have required authored " +
                    $"Condition '{RequiredAuthoredCondition}'.";

                return false;
            }


            if (Tile.UnitOccupancy !=
                null)
            {
                bool SameUnit =
                    ExistingUnit != null &&
                    ReferenceEquals(
                        Tile.UnitOccupancy.Unit,
                        ExistingUnit
                    );


                if (!SameUnit ||
                    !AllowExistingOccupancyFromSameUnit)
                {
                    string OccupyingUnitName =
                        Tile.UnitOccupancy.Unit != null
                            ? Tile.UnitOccupancy.Unit.InstanceName
                            : "Unknown Unit";


                    ValidationMessage =
                        $"Tile {Placement.MapCoordinate} is already occupied by " +
                        $"'{OccupyingUnitName}'.";

                    return false;
                }
            }


            if (Tile.Mine ==
                null)
            {
                continue;
            }


            bool MineRelevantDomain =
                UnitDomain == Domain.Surface ||
                UnitDomain == Domain.Subsurface;


            if (!MineRelevantDomain)
            {
                // Air passes over Mines without interacting with them.
                continue;
            }


            bool FriendlyMine =
                ReferenceEquals(
                    Tile.Mine.Owner,
                    Owner
                );


            if (FriendlyMine)
            {
                ValidationMessage =
                    $"Unit '{UnitName}' cannot occupy {Placement.MapCoordinate} " +
                    "because the Tile contains a friendly Mine.";

                return false;
            }


            Mines.Add(
                Tile.Mine
            );
        }


        TriggeredMines =
            Mines.ToArray();


        ValidationMessage =
            string.Empty;

        return true;
    }


    internal bool TryPrepareUnitPlacementBatch(
        IReadOnlyList<RuntimeUnitPlacementRequest> Requests,
        out PreparedMapUnitDeployment PreparedDeployment,
        out string ValidationMessage
    )
    {
        PreparedDeployment =
            null;


        if (Requests == null ||
            Requests.Count == 0)
        {
            ValidationMessage =
                "Unit placement batch is empty.";

            return false;
        }


        HashSet<RuntimeUnit> SeenUnits =
            new HashSet<RuntimeUnit>();

        HashSet<Vector2Int> SeenCoordinates =
            new HashSet<Vector2Int>();

        HashSet<RuntimeMineOccupancy> TriggeredMines =
            new HashSet<RuntimeMineOccupancy>();

        RuntimeUnitPlacementRequest[] PreparedRequests =
            new RuntimeUnitPlacementRequest[Requests.Count];


        for (int RequestIndex = 0;
            RequestIndex < Requests.Count;
            RequestIndex++)
        {
            RuntimeUnitPlacementRequest Request =
                Requests[RequestIndex];


            if (Request == null ||
                Request.Unit == null ||
                Request.Placements == null ||
                Request.Placements.Length == 0 ||
                !SeenUnits.Add(
                    Request.Unit) ||
                _UnitCoordinates.ContainsKey(
                    Request.Unit))
            {
                ValidationMessage =
                    "Unit placement batch contains an invalid or duplicate Unit.";

                return false;
            }


            if (!ValidateUnitPlacementInternal(
                    Request.Unit,
                    Request.Placements,
                    Request.RequiredAuthoredCondition,
                    false,
                    out RuntimeMineOccupancy[] RequestMines,
                    out ValidationMessage))
            {
                return false;
            }


            for (int TileIndex = 0;
                TileIndex < Request.Placements.Length;
                TileIndex++)
            {
                if (!SeenCoordinates.Add(
                        Request.Placements[TileIndex].MapCoordinate))
                {
                    ValidationMessage =
                        "Unit placement batch contains overlapping footprints.";

                    return false;
                }
            }


            for (int MineIndex = 0;
                MineIndex < RequestMines.Length;
                MineIndex++)
            {
                TriggeredMines.Add(
                    RequestMines[MineIndex]
                );
            }


            PreparedRequests[RequestIndex] =
                Request;
        }


        RuntimeMineOccupancy[] PreparedMines =
            new RuntimeMineOccupancy[TriggeredMines.Count];

        TriggeredMines.CopyTo(
            PreparedMines
        );


        PreparedDeployment =
            new PreparedMapUnitDeployment(
                PreparedRequests,
                PreparedMines
            );

        ValidationMessage =
            string.Empty;

        return true;
    }


    internal void CommitPreparedUnitPlacementBatch(
        PreparedMapUnitDeployment PreparedDeployment
    )
    {
        if (PreparedDeployment == null)
        {
            throw new InvalidOperationException(
                "Prepared Map Unit Deployment is missing."
            );
        }


        for (int RequestIndex = 0;
            RequestIndex < PreparedDeployment.Requests.Length;
            RequestIndex++)
        {
            RuntimeUnitPlacementRequest Request =
                PreparedDeployment.Requests[RequestIndex];

            List<Vector2Int> OccupiedCoordinates =
                new List<Vector2Int>(
                    Request.Placements.Length
                );


            for (int TileIndex = 0;
                TileIndex < Request.Placements.Length;
                TileIndex++)
            {
                RuntimeUnitTilePlacement Placement =
                    Request.Placements[TileIndex];


                TryGetTile(
                    Placement.MapCoordinate,
                    out RuntimeMapTile Tile
                );

                Tile.SetUnitOccupancy(
                    new RuntimeUnitOccupancy(
                        Request.Unit,
                        Placement.FootprintCoordinate
                    )
                );

                OccupiedCoordinates.Add(
                    Placement.MapCoordinate
                );
            }


            _UnitCoordinates.Add(
                Request.Unit,
                OccupiedCoordinates
            );
        }


        RemoveTriggeredMines(
            PreparedDeployment.TriggeredMines
        );
    }


    // Places a RuntimeUnit that is not currently on the board.
    //
    // Enemy Mines entered by Surface/Subsurface Units are removed from
    // the board and returned to GameMaster for effect resolution.
    public bool TryPlaceUnit(
        RuntimeUnit Unit,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        MapTileCondition RequiredAuthoredCondition,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        TriggeredMines =
            Array.Empty<RuntimeMineOccupancy>();


        if (Unit == null)
        {
            ValidationMessage =
                "Runtime Unit is missing.";

            return false;
        }


        if (_UnitCoordinates.ContainsKey(
                Unit))
        {
            ValidationMessage =
                $"Unit '{Unit.InstanceName}' is already on the Map.";

            return false;
        }


        if (!ValidateUnitPlacementInternal(
                Unit,
                Placements,
                RequiredAuthoredCondition,
                false,
                out TriggeredMines,
                out ValidationMessage))
        {
            return false;
        }


        List<Vector2Int> OccupiedCoordinates =
            new List<Vector2Int>(
                Placements.Count
            );


        for (
            int i = 0;
            i < Placements.Count;
            i++
        )
        {
            RuntimeUnitTilePlacement Placement =
                Placements[i];


            TryGetTile(
                Placement.MapCoordinate,
                out RuntimeMapTile Tile
            );


            Tile.SetUnitOccupancy(
                new RuntimeUnitOccupancy(
                    Unit,
                    Placement.FootprintCoordinate
                )
            );


            OccupiedCoordinates.Add(
                Placement.MapCoordinate
            );
        }


        _UnitCoordinates.Add(
            Unit,
            OccupiedCoordinates
        );


        RemoveTriggeredMines(
            TriggeredMines
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Atomically moves an already-placed RuntimeUnit to a new footprint.
    //
    // GameMaster calculates the requested footprint first.
    // GameMap verifies it against the actual board and only mutates
    // occupancy after the entire placement passes validation.
    public bool TryMoveUnit(
        RuntimeUnit Unit,
        IReadOnlyList<RuntimeUnitTilePlacement> Placements,
        out RuntimeMineOccupancy[] TriggeredMines,
        out string ValidationMessage
    )
    {
        TriggeredMines =
            Array.Empty<RuntimeMineOccupancy>();


        if (Unit == null)
        {
            ValidationMessage =
                "Runtime Unit is missing.";

            return false;
        }


        if (!_UnitCoordinates.TryGetValue(
                Unit,
                out List<Vector2Int> OldCoordinates))
        {
            ValidationMessage =
                $"Unit '{Unit.InstanceName}' is not currently on the Map.";

            return false;
        }


        if (!ValidateUnitPlacementInternal(
                Unit,
                Placements,
                MapTileCondition.None,
                true,
                out TriggeredMines,
                out ValidationMessage))
        {
            return false;
        }


        for (
            int i = 0;
            i < OldCoordinates.Count;
            i++
        )
        {
            if (!TryGetTile(
                    OldCoordinates[i],
                    out RuntimeMapTile Tile))
            {
                Debug.LogError(
                    $"GameMap occupancy corruption: Unit '{Unit.InstanceName}' " +
                    $"references missing Tile {OldCoordinates[i]}.",
                    this
                );

                continue;
            }


            if (Tile.UnitOccupancy ==
                    null ||
                !ReferenceEquals(
                    Tile.UnitOccupancy.Unit,
                    Unit))
            {
                Debug.LogError(
                    $"GameMap occupancy corruption: Tile {OldCoordinates[i]} " +
                    $"does not contain expected Unit '{Unit.InstanceName}'.",
                    this
                );

                continue;
            }


            Tile.ClearUnitOccupancy();
        }


        List<Vector2Int> NewCoordinates =
            new List<Vector2Int>(
                Placements.Count
            );


        for (
            int i = 0;
            i < Placements.Count;
            i++
        )
        {
            RuntimeUnitTilePlacement Placement =
                Placements[i];


            TryGetTile(
                Placement.MapCoordinate,
                out RuntimeMapTile Tile
            );


            Tile.SetUnitOccupancy(
                new RuntimeUnitOccupancy(
                    Unit,
                    Placement.FootprintCoordinate
                )
            );


            NewCoordinates.Add(
                Placement.MapCoordinate
            );
        }


        _UnitCoordinates[Unit] =
            NewCoordinates;


        RemoveTriggeredMines(
            TriggeredMines
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryRemoveUnit(
        RuntimeUnit Unit,
        out string ValidationMessage
    )
    {
        if (Unit == null)
        {
            ValidationMessage =
                "Runtime Unit is missing.";

            return false;
        }


        if (!_UnitCoordinates.TryGetValue(
                Unit,
                out List<Vector2Int> Coordinates))
        {
            ValidationMessage =
                $"Unit '{Unit.InstanceName}' is not currently on the Map.";

            return false;
        }


        for (
            int i = 0;
            i < Coordinates.Count;
            i++
        )
        {
            if (!TryGetTile(
                    Coordinates[i],
                    out RuntimeMapTile Tile))
            {
                Debug.LogError(
                    $"GameMap occupancy corruption while removing Unit " +
                    $"'{Unit.InstanceName}': Tile {Coordinates[i]} does not exist.",
                    this
                );

                continue;
            }


            if (Tile.UnitOccupancy ==
                    null ||
                !ReferenceEquals(
                    Tile.UnitOccupancy.Unit,
                    Unit))
            {
                Debug.LogError(
                    $"GameMap occupancy corruption while removing Unit " +
                    $"'{Unit.InstanceName}': Tile {Coordinates[i]} is owned by something else.",
                    this
                );

                continue;
            }


            Tile.ClearUnitOccupancy();
        }


        _UnitCoordinates.Remove(
            Unit
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryGetUnitCoordinates(
        RuntimeUnit Unit,
        out Vector2Int[] Coordinates
    )
    {
        Coordinates =
            Array.Empty<Vector2Int>();


        if (Unit == null)
        {
            return false;
        }


        if (!_UnitCoordinates.TryGetValue(
                Unit,
                out List<Vector2Int> StoredCoordinates))
        {
            return false;
        }


        Coordinates =
            StoredCoordinates.ToArray();


        return true;
    }


    public bool IsUnitOnMap(
        RuntimeUnit Unit
    )
    {
        return
            Unit != null &&
            _UnitCoordinates.ContainsKey(
                Unit
            );
    }


    public bool TryGetUnitOccupancy(
        Vector2Int Coordinate,
        out RuntimeUnitOccupancy Occupancy
    )
    {
        Occupancy =
            null;


        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            return false;
        }


        Occupancy =
            Tile.UnitOccupancy;


        return
            Occupancy != null;
    }


    // Adds one Mine to the board.
    //
    // Detailed Mine deployment rules belong to GameMaster and the
    // deploying System. GameMap enforces the physical board facts:
    // the Tile must exist, cannot be a Barrier, and cannot already
    // contain another Mine.
    public bool TryPlaceMine(
        string MineInstanceID,
        RuntimeParticipant Owner,
        Vector2Int Coordinate,
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                MineInstanceID))
        {
            ValidationMessage =
                "Mine Instance ID is empty.";

            return false;
        }


        if (Owner == null)
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' has no owning Participant.";

            return false;
        }


        if (_MineCoordinates.ContainsKey(
                MineInstanceID))
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' is already on the Map.";

            return false;
        }


        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' cannot be placed outside the Map.";

            return false;
        }


        if (Tile.Terrain ==
            MapTerrain.Barrier)
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' cannot exist on Barrier Tile {Coordinate}.";

            return false;
        }


        if (Tile.Mine !=
            null)
        {
            ValidationMessage =
                $"Tile {Coordinate} already contains Mine '{Tile.Mine.MineInstanceID}'.";

            return false;
        }


        Tile.SetMine(
            new RuntimeMineOccupancy(
                MineInstanceID,
                Owner
            )
        );


        _MineCoordinates.Add(
            MineInstanceID,
            Coordinate
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryRemoveMine(
        string MineInstanceID,
        out string ValidationMessage
    )
    {
        if (!_MineCoordinates.TryGetValue(
                MineInstanceID,
                out Vector2Int Coordinate))
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' is not currently on the Map.";

            return false;
        }


        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            ValidationMessage =
                $"Mine '{MineInstanceID}' references missing Tile {Coordinate}.";

            Debug.LogError(
                $"GameMap Mine state corruption: {ValidationMessage}",
                this
            );


            _MineCoordinates.Remove(
                MineInstanceID
            );


            return false;
        }


        if (Tile.Mine ==
                null ||
            Tile.Mine.MineInstanceID !=
                MineInstanceID)
        {
            ValidationMessage =
                $"Tile {Coordinate} does not contain expected Mine '{MineInstanceID}'.";

            Debug.LogError(
                $"GameMap Mine state corruption: {ValidationMessage}",
                this
            );


            _MineCoordinates.Remove(
                MineInstanceID
            );


            return false;
        }


        Tile.ClearMine();


        _MineCoordinates.Remove(
            MineInstanceID
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool TryGetMine(
        Vector2Int Coordinate,
        out RuntimeMineOccupancy Mine
    )
    {
        Mine =
            null;


        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            return false;
        }


        Mine =
            Tile.Mine;


        return
            Mine != null;
    }


    public bool AddDynamicCondition(
        Vector2Int Coordinate,
        MapTileCondition Condition,
        out string ValidationMessage
    )
    {
        if (Condition ==
            MapTileCondition.None)
        {
            ValidationMessage =
                "Cannot add an empty Dynamic Tile Condition.";

            return false;
        }


        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            ValidationMessage =
                $"Tile {Coordinate} does not exist.";

            return false;
        }


        Tile.AddDynamicCondition(
            Condition
        );


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool RemoveDynamicCondition(
        Vector2Int Coordinate,
        MapTileCondition Condition,
        out string ValidationMessage
    )
    {
        if (!TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            ValidationMessage =
                $"Tile {Coordinate} does not exist.";

            return false;
        }


        if (!Tile.RemoveDynamicCondition(
                Condition))
        {
            ValidationMessage =
                $"Tile {Coordinate} does not contain Dynamic Condition '{Condition}'.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private void RemoveTriggeredMines(
        RuntimeMineOccupancy[] TriggeredMines
    )
    {
        if (TriggeredMines == null)
        {
            return;
        }


        for (
            int i = 0;
            i < TriggeredMines.Length;
            i++
        )
        {
            RuntimeMineOccupancy Mine =
                TriggeredMines[i];


            if (Mine == null)
            {
                continue;
            }


            if (!_MineCoordinates.TryGetValue(
                    Mine.MineInstanceID,
                    out Vector2Int Coordinate))
            {
                Debug.LogError(
                    $"GameMap Mine state corruption: triggered Mine " +
                    $"'{Mine.MineInstanceID}' has no stored coordinate.",
                    this
                );

                continue;
            }


            if (TryGetTile(
                    Coordinate,
                    out RuntimeMapTile Tile) &&
                Tile.Mine !=
                    null &&
                Tile.Mine.MineInstanceID ==
                    Mine.MineInstanceID)
            {
                Tile.ClearMine();
            }


            _MineCoordinates.Remove(
                Mine.MineInstanceID
            );
        }
    }


    private void ClearRuntimeState()
    {
        _AuthoringMap =
            null;

        _MapSize =
            Vector2Int.zero;

        _Tiles =
            null;


        _UnitCoordinates.Clear();
        _MineCoordinates.Clear();


        _Initialized =
            false;
    }
}

