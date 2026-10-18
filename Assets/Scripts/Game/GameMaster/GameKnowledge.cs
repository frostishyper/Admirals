using System;
using System.Collections.Generic;
using UnityEngine;


public enum ParticipantTileVisibility
{
    Fog = 0,
    Obscured = 1,
    Absolute = 2
}


public sealed class ParticipantTileSnapshot
{
    public Vector2Int Coordinate { get; }
    public ParticipantTileVisibility Visibility { get; }
    public bool TerrainKnown { get; }
    public MapTerrain Terrain { get; }

    internal ParticipantTileSnapshot(
        Vector2Int Coordinate,
        ParticipantTileVisibility Visibility,
        bool TerrainKnown,
        MapTerrain Terrain)
    {
        this.Coordinate = Coordinate;
        this.Visibility = Visibility;
        this.TerrainKnown = TerrainKnown;
        this.Terrain = TerrainKnown ? Terrain : MapTerrain.None;
    }
}


public sealed class ParticipantWeaponSnapshot
{
    public int SlotIndex { get; }
    // Authored/static data is intentionally readable by participant controllers.
    // Runtime charge state remains copied into this snapshot.
    public Definition_Weapon Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }
    public int CurrentCharges { get; }
    public int MaxCharges { get; }
    public int ReplenishmentProgress { get; }

    internal ParticipantWeaponSnapshot(RuntimeWeaponSlotState Slot)
    {
        SlotIndex = Slot.SlotIndex;
        Definition = Slot.Weapon;
        DefinitionID = Slot.Weapon != null ? Slot.Weapon.ID : string.Empty;
        Name = Slot.Weapon != null ? Slot.Weapon.Name : string.Empty;
        CurrentCharges = Slot.CurrentCharges;
        MaxCharges = Slot.MaxCharges;
        ReplenishmentProgress = Slot.ReplenishmentProgress;
    }
}


public sealed class ParticipantCarriedUnitSnapshot
{
    public int CarriedUnitIndex { get; }
    public Definition_Unit Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }
    public RuntimeCarriedUnitState State { get; }

    internal ParticipantCarriedUnitSnapshot(
        RuntimeCarriedUnit Unit,
        int CarriedUnitIndex)
    {
        this.CarriedUnitIndex = CarriedUnitIndex;
        Definition = Unit?.Unit;
        DefinitionID = Unit?.Unit != null ? Unit.Unit.ID : string.Empty;
        Name = Unit?.Unit != null ? Unit.Unit.Name : string.Empty;
        State = Unit != null ? Unit.State : RuntimeCarriedUnitState.Lost;
    }
}


public sealed class ParticipantSystemSnapshot
{
    public int SlotIndex { get; }
    public SystemSlotType SlotType { get; }
    public Definition_System Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }
    public int CurrentCharges { get; }
    public int MaxCharges { get; }
    public int ReplenishmentProgress { get; }
    public ParticipantCarriedUnitSnapshot[] CarriedUnits { get; }

    internal ParticipantSystemSnapshot(RuntimeSystemSlotState Slot)
    {
        SlotIndex = Slot.SlotIndex;
        SlotType = Slot.SlotType;
        Definition = Slot.System;
        DefinitionID = Slot.System != null ? Slot.System.ID : string.Empty;
        Name = Slot.System != null ? Slot.System.Name : string.Empty;
        CurrentCharges = Slot.CurrentCharges;
        MaxCharges = Slot.MaxCharges;
        ReplenishmentProgress = Slot.ReplenishmentProgress;

        CarriedUnits = new ParticipantCarriedUnitSnapshot[Slot.CarriedUnits.Count];
        for (int i = 0; i < CarriedUnits.Length; i++)
            CarriedUnits[i] = new ParticipantCarriedUnitSnapshot(Slot.CarriedUnits[i], i);
    }
}


public sealed class ParticipantAbilitySnapshot
{
    public int SlotIndex { get; }
    public Definition_Ability Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }

    internal ParticipantAbilitySnapshot(RuntimeAbilityState Ability)
    {
        SlotIndex = Ability.SlotIndex;
        Definition = Ability.Ability;
        DefinitionID = Ability.Ability != null ? Ability.Ability.ID : string.Empty;
        Name = Ability.Ability != null ? Ability.Ability.Name : string.Empty;
    }
}


public sealed class ParticipantStatusSnapshot
{
    public Definition_Status Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }
    public int RemainingTurns { get; }
    public int IntervalProgress { get; }

    internal ParticipantStatusSnapshot(RuntimeStatusState Status)
    {
        Definition = Status.Status;
        DefinitionID = Status.Status != null ? Status.Status.ID : string.Empty;
        Name = Status.Status != null ? Status.Status.Name : string.Empty;
        RemainingTurns = Status.RemainingTurns;
        IntervalProgress = Status.IntervalProgress;
    }
}


public sealed class ParticipantModifierSnapshot
{
    public GameplayModifier Definition { get; }
    public ModifierType Type { get; }
    public int Value { get; }
    public GameplayPhase ApplicablePhases { get; }
    public DurationMode DurationMode { get; }
    public int RemainingTurns { get; }

    internal ParticipantModifierSnapshot(RuntimeModifierState State)
    {
        Definition = State.Modifier;
        Type = State.Modifier != null ? State.Modifier.Type : ModifierType.None;
        Value = State.Modifier != null ? State.Modifier.Value : 0;
        ApplicablePhases = State.Modifier != null
            ? State.Modifier.ApplicablePhases
            : GameplayPhase.None;
        DurationMode = State.DurationMode;
        RemainingTurns = State.RemainingTurns;
    }
}


public sealed class ParticipantUnitTagSnapshot
{
    public Definition_UnitTag Definition { get; }
    public string DefinitionID { get; }
    public string Name { get; }


    internal ParticipantUnitTagSnapshot(
        Definition_UnitTag Tag
    )
    {
        Definition = Tag;
        DefinitionID =
            Tag != null
                ? Tag.ID
                : string.Empty;

        Name =
            Tag != null
                ? Tag.Name
                : string.Empty;
    }
}


public sealed class ParticipantUnitSnapshot
{
    public string RuntimeUnitID { get; }
    public string FleetEntryID { get; }
    public string InstanceName { get; }
    public Definition_Unit Definition { get; }
    public string DefinitionID { get; }
    public string DefinitionName { get; }
    public RuntimeUnitLifeState LifeState { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public Domain CurrentDomain { get; }
    public UnitFacing Facing { get; }
    public int EmbarkedPresentationLevel { get; }
    public int AbsoluteVision { get; }
    public bool IsOnMap { get; }
    public Vector2Int MovementPivotCoordinate { get; }
    public Vector2Int[] OccupiedCoordinates { get; }
    public ParticipantWeaponSnapshot[] Weapons { get; }
    public ParticipantSystemSnapshot[] Systems { get; }
    public ParticipantAbilitySnapshot[] Abilities { get; }
    public ParticipantStatusSnapshot[] Statuses { get; }
    public ParticipantModifierSnapshot[] Modifiers { get; }
    public ParticipantUnitTagSnapshot[] Tags { get; }

    internal ParticipantUnitSnapshot(
        RuntimeUnit Unit,
        Vector2Int[] Coordinates,
        bool IsOnMap,
        Vector2Int MovementPivotCoordinate)
    {
        RuntimeUnitID = Unit.RuntimeID;
        FleetEntryID = Unit.SourceFleetEntryID;
        InstanceName = Unit.InstanceName;
        Definition = Unit.Definition;
        DefinitionID = Unit.Definition != null ? Unit.Definition.ID : string.Empty;
        DefinitionName = Unit.Definition != null ? Unit.Definition.Name : string.Empty;
        LifeState = Unit.LifeState;
        CurrentHealth = Unit.CurrentHealth;
        MaxHealth = Unit.MaxHealth;
        CurrentDomain = Unit.CurrentDomain;
        Facing = Unit.Facing;
        EmbarkedPresentationLevel = GetEmbarkedPresentationLevel(Unit);
        AbsoluteVision = Unit.Definition != null ? Unit.Definition.AbsoluteVision : 0;
        this.IsOnMap = IsOnMap;
        this.MovementPivotCoordinate = MovementPivotCoordinate;
        OccupiedCoordinates = Coordinates ?? Array.Empty<Vector2Int>();

        Weapons = new ParticipantWeaponSnapshot[Unit.WeaponSlots.Length];
        for (int i = 0; i < Weapons.Length; i++)
            Weapons[i] = new ParticipantWeaponSnapshot(Unit.WeaponSlots[i]);

        Systems = new ParticipantSystemSnapshot[Unit.SystemSlots.Length];
        for (int i = 0; i < Systems.Length; i++)
            Systems[i] = new ParticipantSystemSnapshot(Unit.SystemSlots[i]);

        Abilities = new ParticipantAbilitySnapshot[Unit.Abilities.Length];
        for (int i = 0; i < Abilities.Length; i++)
            Abilities[i] = new ParticipantAbilitySnapshot(Unit.Abilities[i]);

        Statuses = new ParticipantStatusSnapshot[Unit.ActiveStatuses.Count];
        for (int i = 0; i < Statuses.Length; i++)
            Statuses[i] = new ParticipantStatusSnapshot(Unit.ActiveStatuses[i]);

        Modifiers = new ParticipantModifierSnapshot[Unit.ActiveModifiers.Count];
        for (int i = 0; i < Modifiers.Length; i++)
            Modifiers[i] = new ParticipantModifierSnapshot(Unit.ActiveModifiers[i]);

        Definition_UnitTag[] RuntimeTags =
            Unit.GetTags();

        Tags =
            new ParticipantUnitTagSnapshot[
                RuntimeTags.Length
            ];

        for (int i = 0;
            i < Tags.Length;
            i++)
        {
            Tags[i] =
                new ParticipantUnitTagSnapshot(
                    RuntimeTags[i]
                );
        }
    }


    private static int GetEmbarkedPresentationLevel(RuntimeUnit Unit)
    {
        int Count = 0;
        RuntimeCarriedUnit[] CarriedUnits = Unit.GetCarriedUnits();

        for (int i = 0; i < CarriedUnits.Length; i++)
        {
            if (CarriedUnits[i]?.State == RuntimeCarriedUnitState.Carried)
                Count++;
        }

        return Mathf.Clamp(Count, 0, 3);
    }
}


// Conservative enemy projection: only currently Absolute-visible Units exist
// here. It exposes position and presentation tags, but no weapons, systems,
// statuses or hidden runtime IDs.
public sealed class VisibleEnemyUnitSnapshot
{
    public string InstanceName { get; }
    public Definition_Unit Definition { get; }
    public string DefinitionID { get; }
    public string DefinitionName { get; }
    public Domain CurrentDomain { get; }
    public UnitFacing Facing { get; }
    public int EmbarkedPresentationLevel { get; }
    public Vector2Int MovementPivotCoordinate { get; }
    public Vector2Int[] OccupiedCoordinates { get; }
    public ParticipantUnitTagSnapshot[] Tags { get; }

    internal VisibleEnemyUnitSnapshot(
        RuntimeUnit Unit,
        Vector2Int[] Coordinates,
        Vector2Int MovementPivotCoordinate)
    {
        InstanceName = Unit.InstanceName;
        Definition = Unit.Definition;
        DefinitionID = Unit.Definition != null ? Unit.Definition.ID : string.Empty;
        DefinitionName = Unit.Definition != null ? Unit.Definition.Name : string.Empty;
        CurrentDomain = Unit.CurrentDomain;
        Facing = Unit.Facing;
        EmbarkedPresentationLevel = GetEmbarkedPresentationLevel(Unit);
        this.MovementPivotCoordinate = MovementPivotCoordinate;
        OccupiedCoordinates = Coordinates ?? Array.Empty<Vector2Int>();

        Definition_UnitTag[] RuntimeTags =
            Unit.GetTags();

        Tags =
            new ParticipantUnitTagSnapshot[RuntimeTags.Length];


        for (int i = 0; i < Tags.Length; i++)
        {
            Tags[i] =
                new ParticipantUnitTagSnapshot(
                    RuntimeTags[i]
                );
        }
    }


    private static int GetEmbarkedPresentationLevel(RuntimeUnit Unit)
    {
        int Count = 0;
        RuntimeCarriedUnit[] CarriedUnits = Unit.GetCarriedUnits();

        for (int i = 0; i < CarriedUnits.Length; i++)
        {
            if (CarriedUnits[i]?.State == RuntimeCarriedUnitState.Carried)
                Count++;
        }

        return Mathf.Clamp(Count, 0, 3);
    }
}


public sealed class ParticipantMineSnapshot
{
    public string MineInstanceID { get; }
    public Vector2Int Coordinate { get; }
    public bool IsOwned { get; }


    internal ParticipantMineSnapshot(
        RuntimeMineOccupancy Mine,
        Vector2Int Coordinate,
        bool IsOwned
    )
    {
        MineInstanceID = Mine != null
            ? Mine.MineInstanceID
            : string.Empty;
        this.Coordinate = Coordinate;
        this.IsOwned = IsOwned;
    }
}


public sealed class ParticipantContactSnapshot
{
    public Vector2Int Coordinate { get; }
    public Domain Domain { get; }

    internal ParticipantContactSnapshot(Vector2Int Coordinate, Domain Domain)
    {
        this.Coordinate = Coordinate;
        this.Domain = Domain;
    }
}


public sealed class ParticipantGameSnapshot
{
    public GameplayPhase CurrentPhase { get; }
    public int TurnNumber { get; }
    public bool MatchStarted { get; }
    public bool HasFirstPlayerPriority { get; }
    public bool DeploymentLocked { get; }
    public GameMovement.ParticipantView Movement { get; }
    public GamePreBattle.ParticipantView PreBattle { get; }
    public GameSearch.ParticipantView Search { get; }
    public GameBattle.ParticipantView Battle { get; }
    public ParticipantTileSnapshot[] Tiles { get; }
    public ParticipantUnitSnapshot[] OwnUnits { get; }
    public VisibleEnemyUnitSnapshot[] VisibleEnemies { get; }
    public ParticipantMineSnapshot[] Mines { get; }
    public ParticipantContactSnapshot[] Contacts { get; }

    internal ParticipantGameSnapshot(
        GameplayPhase CurrentPhase,
        int TurnNumber,
        bool MatchStarted,
        bool HasFirstPlayerPriority,
        bool DeploymentLocked,
        GameMovement.ParticipantView Movement,
        GamePreBattle.ParticipantView PreBattle,
        GameSearch.ParticipantView Search,
        GameBattle.ParticipantView Battle,
        ParticipantTileSnapshot[] Tiles,
        ParticipantUnitSnapshot[] OwnUnits,
        VisibleEnemyUnitSnapshot[] VisibleEnemies,
        ParticipantMineSnapshot[] Mines,
        ParticipantContactSnapshot[] Contacts)
    {
        this.CurrentPhase = CurrentPhase;
        this.TurnNumber = TurnNumber;
        this.MatchStarted = MatchStarted;
        this.HasFirstPlayerPriority = HasFirstPlayerPriority;
        this.DeploymentLocked = DeploymentLocked;
        this.Movement = Movement;
        this.PreBattle = PreBattle;
        this.Search = Search;
        this.Battle = Battle;
        this.Tiles = Tiles ?? Array.Empty<ParticipantTileSnapshot>();
        this.OwnUnits = OwnUnits ?? Array.Empty<ParticipantUnitSnapshot>();
        this.VisibleEnemies = VisibleEnemies ?? Array.Empty<VisibleEnemyUnitSnapshot>();
        this.Mines = Mines ?? Array.Empty<ParticipantMineSnapshot>();
        this.Contacts = Contacts ?? Array.Empty<ParticipantContactSnapshot>();
    }
}


// Authoritative participant-specific visibility and learned-information owner.
// Physical truth remains in GameMap and mutable Unit truth remains in GameUnits.
internal sealed class GameKnowledge
{
    internal sealed class SearchContactRecord
    {
        public RuntimeUnit Unit { get; }
        public Vector2Int Coordinate { get; }
        public Domain Domain { get; }

        public SearchContactRecord(RuntimeUnit Unit, Vector2Int Coordinate, Domain Domain)
        {
            this.Unit = Unit;
            this.Coordinate = Coordinate;
            this.Domain = Domain;
        }
    }

    private readonly Dictionary<Vector2Int, ParticipantTileVisibility>
        _ParticipantAVisibility = new Dictionary<Vector2Int, ParticipantTileVisibility>();
    private readonly Dictionary<Vector2Int, ParticipantTileVisibility>
        _ParticipantBVisibility = new Dictionary<Vector2Int, ParticipantTileVisibility>();

    private readonly HashSet<Vector2Int>
        _ParticipantAKnownTerrain = new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int>
        _ParticipantBKnownTerrain = new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int>
        _ParticipantASearchedTiles = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int>
        _ParticipantBSearchedTiles = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, SearchContactRecord>
        _ParticipantAContacts = new Dictionary<Vector2Int, SearchContactRecord>();
    private readonly Dictionary<Vector2Int, SearchContactRecord>
        _ParticipantBContacts = new Dictionary<Vector2Int, SearchContactRecord>();

    private GameMap _GameMap;
    private GameUnits _GameUnits;
    private GameDeployment _GameDeployment;
    private RuntimeParticipant _ParticipantA;
    private RuntimeParticipant _ParticipantB;

    public bool IsInitialized { get; private set; }

    internal bool Initialize(
        GameMap GameMap,
        GameUnits GameUnits,
        GameDeployment GameDeployment,
        RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB,
        out string ValidationMessage)
    {
        IsInitialized = false;

        if (GameMap == null || !GameMap.Initialized ||
            GameUnits == null || !GameUnits.Initialized ||
            GameDeployment == null || !GameDeployment.IsInitialized ||
            ParticipantA == null || ParticipantB == null)
        {
            ValidationMessage =
                "Initialized map, units, deployment and both participants are required.";
            return false;
        }

        _GameMap = GameMap;
        _GameUnits = GameUnits;
        _GameDeployment = GameDeployment;
        _ParticipantA = ParticipantA;
        _ParticipantB = ParticipantB;
        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }

    internal bool Refresh(
        GameplayPhase Phase,
        out string ValidationMessage)
    {
        if (!IsInitialized)
        {
            ValidationMessage = "GameKnowledge is not initialized.";
            return false;
        }

        if (Phase == GameplayPhase.Deployment)
        {
            if (!BuildDeploymentVisibility(
                    _ParticipantA,
                    _ParticipantAVisibility) ||
                !BuildDeploymentVisibility(
                    _ParticipantB,
                    _ParticipantBVisibility))
            {
                ValidationMessage =
                    "Deployment visibility could not be calculated.";
                return false;
            }
        }
        else
        {
            BuildRuntimeVisibility(_ParticipantA, _ParticipantAVisibility);
            BuildRuntimeVisibility(_ParticipantB, _ParticipantBVisibility);
        }

        ValidationMessage = string.Empty;
        return true;
    }

    internal bool TryCreateSnapshot(
        RuntimeParticipant Participant,
        GameplayPhase Phase,
        int TurnNumber,
        bool MatchStarted,
        RuntimeParticipant FirstPlayer,
        GameMovement.ParticipantView Movement,
        GamePreBattle.ParticipantView PreBattle,
        GameSearch.ParticipantView Search,
        GameBattle.ParticipantView Battle,
        out ParticipantGameSnapshot Snapshot,
        out string ValidationMessage)
    {
        Snapshot = null;

        if (!TryGetVisibility(Participant, out Dictionary<Vector2Int, ParticipantTileVisibility> Visibility) ||
            !_GameUnits.TryGetRoster(Participant, out ParticipantUnitRoster OwnRoster))
        {
            ValidationMessage = "Participant has no knowledge state in this Match.";
            return false;
        }

        if (!Refresh(Phase, out ValidationMessage))
            return false;

        ParticipantUnitRoster EnemyRoster =
            ReferenceEquals(Participant, _ParticipantA)
                ? _GameUnits.ParticipantB
                : _GameUnits.ParticipantA;

        Snapshot = new ParticipantGameSnapshot(
            Phase,
            TurnNumber,
            MatchStarted,
            ReferenceEquals(Participant, FirstPlayer),
            Participant.DeploymentLocked,
            Movement,
            PreBattle,
            Search,
            Battle,
            BuildTileSnapshots(Visibility),
            BuildOwnUnitSnapshots(OwnRoster),
            BuildVisibleEnemySnapshots(EnemyRoster, Visibility),
            BuildMineSnapshots(Participant, Visibility),
            BuildContactSnapshots(Participant));

        ValidationMessage = string.Empty;
        return true;
    }

    private bool BuildDeploymentVisibility(
        RuntimeParticipant Participant,
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        FillVisibility(Visibility, ParticipantTileVisibility.Obscured);

        if (!_GameDeployment.TryGetDeploymentZoneCoordinates(
                Participant,
                out Vector2Int[] Zone))
            return false;

        for (int i = 0; i < Zone.Length; i++)
        {
            Visibility[Zone[i]] = ParticipantTileVisibility.Absolute;

            if (TryGetKnownTerrain(
                    Participant,
                    out HashSet<Vector2Int> KnownTerrain))
            {
                KnownTerrain.Add(
                    Zone[i]
                );
            }
        }

        return true;
    }

    private void BuildRuntimeVisibility(
        RuntimeParticipant Participant,
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        FillVisibility(Visibility, ParticipantTileVisibility.Fog);

        if (TryGetKnownTerrain(
                Participant,
                out HashSet<Vector2Int> KnownTerrain))
        {
            foreach (Vector2Int Coordinate
                in KnownTerrain)
            {
                if (_GameMap.IsInsideBounds(
                        Coordinate))
                {
                    Visibility[Coordinate] =
                        ParticipantTileVisibility.Obscured;
                }
            }
        }

        if (TryGetSearchedTiles(Participant, out HashSet<Vector2Int> SearchedTiles))
        {
            foreach (Vector2Int Coordinate in SearchedTiles)
            {
                if (_GameMap.IsInsideBounds(Coordinate))
                    Visibility[Coordinate] = ParticipantTileVisibility.Obscured;
            }
        }

        if (!_GameUnits.TryGetRoster(
                Participant,
                out ParticipantUnitRoster Roster))
            return;

        for (int i = 0; i < Roster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Unit = Roster.RuntimeUnits[i];
            if (Unit == null || !Unit.IsActive ||
                !_GameMap.TryGetUnitCoordinates(Unit, out Vector2Int[] Footprint))
                continue;

            int Range = Unit.Definition != null
                ? Mathf.Max(0, Unit.Definition.AbsoluteVision)
                : 0;

            for (int f = 0; f < Footprint.Length; f++)
            {
                for (int y = -Range; y <= Range; y++)
                {
                    for (int x = -Range; x <= Range; x++)
                    {
                        Vector2Int Coordinate =
                            Footprint[f] + new Vector2Int(x, y);

                        if (_GameMap.IsInsideBounds(Coordinate))
                        {
                            Visibility[Coordinate] =
                                ParticipantTileVisibility.Absolute;

                            KnownTerrain?.Add(
                                Coordinate
                            );
                        }
                    }
                }
            }
        }
    }

    private void FillVisibility(
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility,
        ParticipantTileVisibility State)
    {
        Visibility.Clear();

        for (int y = 0; y < _GameMap.MapSize.y; y++)
            for (int x = 0; x < _GameMap.MapSize.x; x++)
                Visibility[new Vector2Int(x, y)] = State;
    }

    private ParticipantTileSnapshot[] BuildTileSnapshots(
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        ParticipantTileSnapshot[] Result =
            new ParticipantTileSnapshot[_GameMap.TileCount];
        int Index = 0;

        for (int y = 0; y < _GameMap.MapSize.y; y++)
        {
            for (int x = 0; x < _GameMap.MapSize.x; x++)
            {
                Vector2Int Coordinate = new Vector2Int(x, y);
                ParticipantTileVisibility State = Visibility[Coordinate];
                bool Known = State != ParticipantTileVisibility.Fog;
                _GameMap.TryGetTile(Coordinate, out RuntimeMapTile Tile);
                Result[Index++] = new ParticipantTileSnapshot(
                    Coordinate,
                    State,
                    Known,
                    Known && Tile != null ? Tile.Terrain : MapTerrain.None);
            }
        }

        return Result;
    }

    private ParticipantUnitSnapshot[] BuildOwnUnitSnapshots(
        ParticipantUnitRoster Roster)
    {
        ParticipantUnitSnapshot[] Result =
            new ParticipantUnitSnapshot[Roster.RuntimeUnits.Count];

        for (int i = 0; i < Result.Length; i++)
        {
            RuntimeUnit Unit = Roster.RuntimeUnits[i];
            _GameMap.TryGetUnitCoordinates(Unit, out Vector2Int[] Coordinates);

            bool IsOnMap =
                _GameMap.TryGetUnitMovementPivotCoordinate(
                    Unit,
                    out Vector2Int MovementPivotCoordinate
                );

            Result[i] =
                new ParticipantUnitSnapshot(
                    Unit,
                    Coordinates,
                    IsOnMap,
                    MovementPivotCoordinate
                );
        }

        return Result;
    }

    private VisibleEnemyUnitSnapshot[] BuildVisibleEnemySnapshots(
        ParticipantUnitRoster EnemyRoster,
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        List<VisibleEnemyUnitSnapshot> Result =
            new List<VisibleEnemyUnitSnapshot>();

        for (int i = 0; i < EnemyRoster.RuntimeUnits.Count; i++)
        {
            RuntimeUnit Unit = EnemyRoster.RuntimeUnits[i];
            if (Unit == null || !Unit.IsActive ||
                !_GameMap.TryGetUnitCoordinates(Unit, out Vector2Int[] Coordinates))
                continue;

            bool Visible = false;
            for (int c = 0; c < Coordinates.Length; c++)
            {
                if (Visibility.TryGetValue(
                        Coordinates[c],
                        out ParticipantTileVisibility State) &&
                    State == ParticipantTileVisibility.Absolute)
                {
                    Visible = true;
                    break;
                }
            }

            if (Visible &&
                _GameMap.TryGetUnitMovementPivotCoordinate(
                    Unit,
                    out Vector2Int MovementPivotCoordinate))
            {
                Result.Add(
                    new VisibleEnemyUnitSnapshot(
                        Unit,
                        Coordinates,
                        MovementPivotCoordinate
                    )
                );
            }
        }

        return Result.ToArray();
    }


    private ParticipantMineSnapshot[] BuildMineSnapshots(
        RuntimeParticipant Participant,
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility
    )
    {
        List<ParticipantMineSnapshot> Result =
            new List<ParticipantMineSnapshot>();

        RuntimeMineMapRecord[] Mines =
            _GameMap.GetMines();


        for (int i = 0; i < Mines.Length; i++)
        {
            RuntimeMineMapRecord Record = Mines[i];
            RuntimeMineOccupancy Mine = Record?.Mine;

            if (Mine == null)
                continue;


            bool IsOwned =
                ReferenceEquals(Mine.Owner, Participant);

            bool IsVisibleEnemyMine =
                !IsOwned &&
                Visibility.TryGetValue(
                    Record.Coordinate,
                    out ParticipantTileVisibility State) &&
                State == ParticipantTileVisibility.Absolute;


            if (IsOwned || IsVisibleEnemyMine)
            {
                Result.Add(
                    new ParticipantMineSnapshot(
                        Mine,
                        Record.Coordinate,
                        IsOwned
                    )
                );
            }
        }


        return Result.ToArray();
    }


    private ParticipantContactSnapshot[] BuildContactSnapshots(
        RuntimeParticipant Participant)
    {
        if (!TryGetContacts(Participant,
                out Dictionary<Vector2Int, SearchContactRecord> Contacts))
            return Array.Empty<ParticipantContactSnapshot>();

        List<ParticipantContactSnapshot> Result =
            new List<ParticipantContactSnapshot>();
        foreach (KeyValuePair<Vector2Int, SearchContactRecord> Entry in Contacts)
        {
            SearchContactRecord Contact = Entry.Value;
            if (Contact?.Unit != null && Contact.Unit.IsActive)
                Result.Add(new ParticipantContactSnapshot(
                    Entry.Key, Contact.Domain));
        }
        return Result.ToArray();
    }


    internal void RecordSearchOutcome(RuntimeParticipant Participant,
        IReadOnlyList<Vector2Int> SearchedCoordinates,
        IReadOnlyList<SearchContactRecord> Contacts)
    {
        if (!TryGetSearchedTiles(Participant, out HashSet<Vector2Int> Tiles) ||
            !TryGetContacts(Participant,
                out Dictionary<Vector2Int, SearchContactRecord> KnownContacts))
            return;

        if (SearchedCoordinates != null)
        {
            for (int i = 0; i < SearchedCoordinates.Count; i++)
                if (_GameMap.IsInsideBounds(SearchedCoordinates[i]))
                    Tiles.Add(SearchedCoordinates[i]);
        }
        if (Contacts != null)
        {
            for (int i = 0; i < Contacts.Count; i++)
            {
                SearchContactRecord Contact = Contacts[i];
                if (Contact != null)
                    KnownContacts[Contact.Coordinate] = Contact;
            }
        }
    }


    // Called when the End phase concludes and a new turn begins.
    internal void ClearTurnSearchKnowledge()
    {
        _ParticipantASearchedTiles.Clear();
        _ParticipantBSearchedTiles.Clear();
        _ParticipantAContacts.Clear();
        _ParticipantBContacts.Clear();
    }


    // Resolves a participant-known target without exposing hidden RuntimeUnit
    // identity through the participant-facing snapshot/API.
    internal bool TryResolveKnownEnemyAtCoordinate(
        RuntimeParticipant Participant,
        Vector2Int Coordinate,
        out RuntimeUnit Unit,
        out bool IsAbsoluteVisible)
    {
        Unit = null;
        IsAbsoluteVisible = false;
        if (!IsInitialized || Participant == null ||
            !_GameMap.IsInsideBounds(Coordinate))
            return false;

        if (_GameMap.TryGetUnitOccupancy(Coordinate,
                out RuntimeUnitOccupancy Occupancy) &&
            Occupancy?.Unit != null && Occupancy.Unit.IsActive &&
            !ReferenceEquals(Occupancy.Owner, Participant) &&
            IsUnitVisibleToParticipant(Participant, Occupancy.Unit))
        {
            Unit = Occupancy.Unit;
            IsAbsoluteVisible = true;
            return true;
        }

        if (TryGetContacts(Participant,
                out Dictionary<Vector2Int, SearchContactRecord> Contacts) &&
            Contacts.TryGetValue(Coordinate, out SearchContactRecord Contact) &&
            Contact?.Unit != null && Contact.Unit.IsActive)
        {
            Unit = Contact.Unit;
            return true;
        }

        return false;
    }


    private bool TryGetSearchedTiles(RuntimeParticipant Participant,
        out HashSet<Vector2Int> Tiles)
    {
        Tiles = ReferenceEquals(Participant, _ParticipantA)
            ? _ParticipantASearchedTiles
            : ReferenceEquals(Participant, _ParticipantB)
                ? _ParticipantBSearchedTiles : null;
        return Tiles != null;
    }


    private bool TryGetContacts(RuntimeParticipant Participant,
        out Dictionary<Vector2Int, SearchContactRecord> Contacts)
    {
        Contacts = ReferenceEquals(Participant, _ParticipantA)
            ? _ParticipantAContacts
            : ReferenceEquals(Participant, _ParticipantB)
                ? _ParticipantBContacts : null;
        return Contacts != null;
    }


    internal bool IsUnitVisibleToParticipant(
        RuntimeParticipant Participant,
        RuntimeUnit Unit
    )
    {
        if (Unit == null ||
            !Unit.IsActive ||
            !TryGetVisibility(
                Participant,
                out Dictionary<Vector2Int, ParticipantTileVisibility> Visibility) ||
            !_GameMap.TryGetUnitCoordinates(
                Unit,
                out Vector2Int[] Coordinates))
        {
            return false;
        }


        for (int i = 0; i < Coordinates.Length; i++)
        {
            if (Visibility.TryGetValue(
                    Coordinates[i],
                    out ParticipantTileVisibility State) &&
                State == ParticipantTileVisibility.Absolute)
            {
                return true;
            }
        }


        return false;
    }

    private bool TryGetVisibility(
        RuntimeParticipant Participant,
        out Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        Visibility = null;

        if (!IsInitialized || Participant == null)
            return false;

        if (ReferenceEquals(Participant, _ParticipantA))
            Visibility = _ParticipantAVisibility;
        else if (ReferenceEquals(Participant, _ParticipantB))
            Visibility = _ParticipantBVisibility;

        return Visibility != null;
    }


    internal bool TryGetTileKnowledge(
        RuntimeParticipant Participant,
        Vector2Int Coordinate,
        out ParticipantTileVisibility Visibility,
        out MapTerrain KnownTerrain
    )
    {
        Visibility =
            ParticipantTileVisibility.Fog;

        KnownTerrain =
            MapTerrain.None;


        if (!TryGetVisibility(
                Participant,
                out Dictionary<Vector2Int, ParticipantTileVisibility> VisibilityMap) ||
            !VisibilityMap.TryGetValue(
                Coordinate,
                out Visibility) ||
            Visibility ==
                ParticipantTileVisibility.Fog ||
            !_GameMap.TryGetTile(
                Coordinate,
                out RuntimeMapTile Tile))
        {
            return false;
        }


        KnownTerrain =
            Tile.Terrain;

        return true;
    }


    private bool TryGetKnownTerrain(
        RuntimeParticipant Participant,
        out HashSet<Vector2Int> KnownTerrain
    )
    {
        KnownTerrain =
            null;


        if (!IsInitialized ||
            Participant == null)
        {
            return false;
        }


        if (ReferenceEquals(
                Participant,
                _ParticipantA))
        {
            KnownTerrain =
                _ParticipantAKnownTerrain;
        }
        else if (ReferenceEquals(
            Participant,
            _ParticipantB))
        {
            KnownTerrain =
                _ParticipantBKnownTerrain;
        }


        return
            KnownTerrain != null;
    }
}



