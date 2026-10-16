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
    public string DefinitionID { get; }
    public string Name { get; }
    public int CurrentCharges { get; }
    public int MaxCharges { get; }
    public int ReplenishmentProgress { get; }

    internal ParticipantWeaponSnapshot(RuntimeWeaponSlotState Slot)
    {
        SlotIndex = Slot.SlotIndex;
        DefinitionID = Slot.Weapon != null ? Slot.Weapon.ID : string.Empty;
        Name = Slot.Weapon != null ? Slot.Weapon.Name : string.Empty;
        CurrentCharges = Slot.CurrentCharges;
        MaxCharges = Slot.MaxCharges;
        ReplenishmentProgress = Slot.ReplenishmentProgress;
    }
}


public sealed class ParticipantCarriedUnitSnapshot
{
    public string DefinitionID { get; }
    public string Name { get; }
    public RuntimeCarriedUnitState State { get; }

    internal ParticipantCarriedUnitSnapshot(RuntimeCarriedUnit Unit)
    {
        DefinitionID = Unit?.Unit != null ? Unit.Unit.ID : string.Empty;
        Name = Unit?.Unit != null ? Unit.Unit.Name : string.Empty;
        State = Unit != null ? Unit.State : RuntimeCarriedUnitState.Lost;
    }
}


public sealed class ParticipantSystemSnapshot
{
    public int SlotIndex { get; }
    public SystemSlotType SlotType { get; }
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
        DefinitionID = Slot.System != null ? Slot.System.ID : string.Empty;
        Name = Slot.System != null ? Slot.System.Name : string.Empty;
        CurrentCharges = Slot.CurrentCharges;
        MaxCharges = Slot.MaxCharges;
        ReplenishmentProgress = Slot.ReplenishmentProgress;

        CarriedUnits = new ParticipantCarriedUnitSnapshot[Slot.CarriedUnits.Count];
        for (int i = 0; i < CarriedUnits.Length; i++)
            CarriedUnits[i] = new ParticipantCarriedUnitSnapshot(Slot.CarriedUnits[i]);
    }
}


public sealed class ParticipantAbilitySnapshot
{
    public int SlotIndex { get; }
    public string DefinitionID { get; }
    public string Name { get; }

    internal ParticipantAbilitySnapshot(RuntimeAbilityState Ability)
    {
        SlotIndex = Ability.SlotIndex;
        DefinitionID = Ability.Ability != null ? Ability.Ability.ID : string.Empty;
        Name = Ability.Ability != null ? Ability.Ability.Name : string.Empty;
    }
}


public sealed class ParticipantStatusSnapshot
{
    public string DefinitionID { get; }
    public string Name { get; }
    public int RemainingTurns { get; }
    public int IntervalProgress { get; }

    internal ParticipantStatusSnapshot(RuntimeStatusState Status)
    {
        DefinitionID = Status.Status != null ? Status.Status.ID : string.Empty;
        Name = Status.Status != null ? Status.Status.Name : string.Empty;
        RemainingTurns = Status.RemainingTurns;
        IntervalProgress = Status.IntervalProgress;
    }
}


public sealed class ParticipantModifierSnapshot
{
    public ModifierType Type { get; }
    public int Value { get; }
    public GameplayPhase ApplicablePhases { get; }
    public DurationMode DurationMode { get; }
    public int RemainingTurns { get; }

    internal ParticipantModifierSnapshot(RuntimeModifierState State)
    {
        Type = State.Modifier != null ? State.Modifier.Type : ModifierType.None;
        Value = State.Modifier != null ? State.Modifier.Value : 0;
        ApplicablePhases = State.Modifier != null
            ? State.Modifier.ApplicablePhases
            : GameplayPhase.None;
        DurationMode = State.DurationMode;
        RemainingTurns = State.RemainingTurns;
    }
}


public sealed class ParticipantUnitSnapshot
{
    public string FleetEntryID { get; }
    public string InstanceName { get; }
    public string DefinitionID { get; }
    public string DefinitionName { get; }
    public RuntimeUnitLifeState LifeState { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public Domain CurrentDomain { get; }
    public UnitFacing Facing { get; }
    public int AbsoluteVision { get; }
    public Vector2Int[] OccupiedCoordinates { get; }
    public ParticipantWeaponSnapshot[] Weapons { get; }
    public ParticipantSystemSnapshot[] Systems { get; }
    public ParticipantAbilitySnapshot[] Abilities { get; }
    public ParticipantStatusSnapshot[] Statuses { get; }
    public ParticipantModifierSnapshot[] Modifiers { get; }
    public string[] Tags { get; }

    internal ParticipantUnitSnapshot(
        RuntimeUnit Unit,
        Vector2Int[] Coordinates)
    {
        FleetEntryID = Unit.SourceFleetEntryID;
        InstanceName = Unit.InstanceName;
        DefinitionID = Unit.Definition != null ? Unit.Definition.ID : string.Empty;
        DefinitionName = Unit.Definition != null ? Unit.Definition.Name : string.Empty;
        LifeState = Unit.LifeState;
        CurrentHealth = Unit.CurrentHealth;
        MaxHealth = Unit.MaxHealth;
        CurrentDomain = Unit.CurrentDomain;
        Facing = Unit.Facing;
        AbsoluteVision = Unit.Definition != null ? Unit.Definition.AbsoluteVision : 0;
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

        Tags = Unit.GetTags();
    }
}


// Conservative enemy projection: only currently Absolute-visible Units exist
// here. It intentionally exposes no weapons, systems, statuses or hidden IDs.
public sealed class VisibleEnemyUnitSnapshot
{
    public string InstanceName { get; }
    public string DefinitionID { get; }
    public string DefinitionName { get; }
    public Domain CurrentDomain { get; }
    public UnitFacing Facing { get; }
    public Vector2Int[] OccupiedCoordinates { get; }

    internal VisibleEnemyUnitSnapshot(
        RuntimeUnit Unit,
        Vector2Int[] Coordinates)
    {
        InstanceName = Unit.InstanceName;
        DefinitionID = Unit.Definition != null ? Unit.Definition.ID : string.Empty;
        DefinitionName = Unit.Definition != null ? Unit.Definition.Name : string.Empty;
        CurrentDomain = Unit.CurrentDomain;
        Facing = Unit.Facing;
        OccupiedCoordinates = Coordinates ?? Array.Empty<Vector2Int>();
    }
}


public sealed class ParticipantGameSnapshot
{
    public GameplayPhase CurrentPhase { get; }
    public int TurnNumber { get; }
    public bool MatchStarted { get; }
    public bool HasFirstPlayerPriority { get; }
    public bool DeploymentLocked { get; }
    public ParticipantTileSnapshot[] Tiles { get; }
    public ParticipantUnitSnapshot[] OwnUnits { get; }
    public VisibleEnemyUnitSnapshot[] VisibleEnemies { get; }

    internal ParticipantGameSnapshot(
        GameplayPhase CurrentPhase,
        int TurnNumber,
        bool MatchStarted,
        bool HasFirstPlayerPriority,
        bool DeploymentLocked,
        ParticipantTileSnapshot[] Tiles,
        ParticipantUnitSnapshot[] OwnUnits,
        VisibleEnemyUnitSnapshot[] VisibleEnemies)
    {
        this.CurrentPhase = CurrentPhase;
        this.TurnNumber = TurnNumber;
        this.MatchStarted = MatchStarted;
        this.HasFirstPlayerPriority = HasFirstPlayerPriority;
        this.DeploymentLocked = DeploymentLocked;
        this.Tiles = Tiles ?? Array.Empty<ParticipantTileSnapshot>();
        this.OwnUnits = OwnUnits ?? Array.Empty<ParticipantUnitSnapshot>();
        this.VisibleEnemies = VisibleEnemies ?? Array.Empty<VisibleEnemyUnitSnapshot>();
    }
}


// Authoritative participant-specific visibility and learned-information owner.
// Physical truth remains in GameMap and mutable Unit truth remains in GameUnits.
internal sealed class GameKnowledge
{
    private readonly Dictionary<Vector2Int, ParticipantTileVisibility>
        _ParticipantAVisibility = new Dictionary<Vector2Int, ParticipantTileVisibility>();
    private readonly Dictionary<Vector2Int, ParticipantTileVisibility>
        _ParticipantBVisibility = new Dictionary<Vector2Int, ParticipantTileVisibility>();

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
            BuildTileSnapshots(Visibility),
            BuildOwnUnitSnapshots(OwnRoster),
            BuildVisibleEnemySnapshots(EnemyRoster, Visibility));

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
            Visibility[Zone[i]] = ParticipantTileVisibility.Absolute;

        return true;
    }

    private void BuildRuntimeVisibility(
        RuntimeParticipant Participant,
        Dictionary<Vector2Int, ParticipantTileVisibility> Visibility)
    {
        FillVisibility(Visibility, ParticipantTileVisibility.Fog);

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

            HashSet<Vector2Int> FootprintSet =
                new HashSet<Vector2Int>(Footprint);
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

                        if (_GameMap.IsInsideBounds(Coordinate) &&
                            !FootprintSet.Contains(Coordinate))
                            Visibility[Coordinate] =
                                ParticipantTileVisibility.Absolute;
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
            Result[i] = new ParticipantUnitSnapshot(Unit, Coordinates);
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

            if (Visible)
                Result.Add(new VisibleEnemyUnitSnapshot(Unit, Coordinates));
        }

        return Result.ToArray();
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
}



