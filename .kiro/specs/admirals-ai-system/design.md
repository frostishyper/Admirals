# Design Document: Admirals AI System

## Overview

The Admirals AI system provides intelligent computer opponents for the turn-based naval warfare game. The system operates within strict constraints: only a subset of units can move per turn (MovementLimit = (ActiveUnits + 1) / 2), all decisions must respect fog-of-war rules, and all operations must go through GameMaster's participant-filtered APIs.

The AI architecture is stratified into three layers:

1. **Strategic Layer (FleetCommander)**: Evaluates overall fleet state and selects high-level stances (Hunt, Advance, Hold, DefensiveFallback, Retreat) that guide tactical behavior
2. **Tactical Layer (MovementPrioritizer, MovementPlanner)**: Ranks units by movement priority, scores potential destinations, and selects movements that execute the strategic stance
3. **Orchestration Layer (AIController)**: Coordinates phase transitions, builds turn context, and manages the commit workflow with GameMaster APIs

The critical design challenge is the movement limitation system: with only half the fleet able to move per turn, the AI must make intelligent decisions about which units to move first. This is addressed through a robust priority scoring system that considers unit role, tactical opportunity, fleet stance, and threat proximity.

### Key Design Principles

- **Fog-of-war compliance**: All decisions reason only over visible enemy units and maintain predictions for hidden enemies
- **GameMaster API integration**: All operations validated and committed through authoritative participant-filtered APIs
- **Self-contained implementation**: Entire AI system lives in Assets/Scripts/Game/AI/ with no modifications to existing game code
- **No static state leakage**: All state is instance-based or participant-keyed to support clean initialization/cleanup between matches
- **Graceful degradation**: System handles validation failures, pathfinding blockages, and low-visibility scenarios without halting

## Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                       GameMaster                             │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │ GameMovement │  │GameDeployment│  │ GameKnowledge   │   │
│  └──────┬───────┘  └──────┬───────┘  └────────┬────────┘   │
│         │                  │                    │            │
└─────────┼──────────────────┼────────────────────┼────────────┘
          │                  │                    │
          │ PhaseChanged event                    │
          │                  │                    │
┌─────────▼──────────────────▼────────────────────▼────────────┐
│                      AIController                             │
│                  (MonoBehaviour Orchestrator)                 │
│                                                               │
│  Responsibilities:                                            │
│  - Subscribe to GameMaster.PhaseChanged events               │
│  - Validate participant identity                             │
│  - Build AITurnContext from GameMaster state                 │
│  - Coordinate subsystem calls                                │
│  - Enforce MovementLimit                                     │
│  - Track claimed targets                                     │
│  - Handle validation/commit workflow                         │
└───────┬──────────────┬─────────────────┬─────────────────────┘
        │              │                 │
   ┌────▼─────┐  ┌────▼──────┐    ┌─────▼──────┐
   │  Fleet   │  │ Movement  │    │ Deployment │
   │Commander │  │Prioritizer│    │  Planner   │
   └────┬─────┘  └────┬──────┘    └─────┬──────┘
        │             │                  │
        │             │                  │
   ┌────▼─────────────▼──────────────────▼──────┐
   │          Supporting Data Structures         │
   │                                             │
   │  - AITurnContext: Per-turn snapshot builder │
   │  - FleetSnapshot: Immutable fleet state     │
   │  - EnemyMemory: Position tracking/prediction│
   └─────────────────────────────────────────────┘
```

### Data Flow: Movement Phase Processing

```
1. GameMaster fires PhaseChanged(Phase.Move, ActiveParticipant)
                │
                ▼
2. AIController.OnPhaseChanged()
   - Validate ActiveParticipant == AI's RuntimeParticipant
   - Build AITurnContext from GameMaster.GameUnits, GameKnowledge
                │
                ▼
3. FleetCommander.Evaluate(context)
   - Calculate MaterialScore and FleetHpFraction
   - Select FleetStance based on thresholds and hysteresis
   - Generate FleetSnapshot with positions, roles, contacts
   - Update EnemyMemory with visible contacts
                │
                ▼
4. MovementPrioritizer.PrioritizeUnits(context, snapshot)
   - Score each alive unit for movement priority
   - Consider: UnitRole, threat proximity, attack opportunity, stance
   - Return sorted list of RuntimeUnit IDs
                │
                ▼
5. For each unit in priority order (until MovementLimit reached):
   │
   ├─▶ 5a. MovementPlanner.ChooseDestination(unit, context, snapshot)
   │   - Generate candidate tiles within movement range
   │   - Calculate danger at each tile from enemy weapon ranges
   │   - Score tiles: AttackOpportunity - Danger + Cohesion + RoleBonus
   │   - Return best tile and likely target
   │
   ├─▶ 5b. GameMovement.ValidateMovement(unit, destination)
   │   - Get MovementPlan with pathfinding route
   │   - Check if route is valid and within budget
   │
   ├─▶ 5c. GameMovement.TryCommitMovement(unit, destination)
   │   - Commit movement atomically
   │   - Return CommitResult with final position
   │
   └─▶ 5d. AIController updates state
       - Increment MovesCommitted counter
       - Add likely target to claimed targets set
       - Handle validation/commit failures gracefully
                │
                ▼
6. When MovesCommitted == MovementLimit OR all units processed:
   - Clear claimed targets set
   - End turn processing
```

### State Machine: FleetStance Transitions

```
                    ┌─────────────────────┐
                    │       HUNT          │
         ┌──────────│  (No contacts,      │◀─────────────┐
         │          │   explore map)      │              │
         │          └──────────┬──────────┘              │
         │                     │ Contacts detected       │
         │                     ▼                         │
         │          ┌─────────────────────┐              │
         │          │      ADVANCE        │              │
         │  ┌───────│  (Material ≥ 0.90, │              │
         │  │       │   strong position)  │              │
         │  │       └──────────┬──────────┘              │
         │  │                  │                         │
         │  │ Score drops      │ Score stabilizes        │ Score recovers
         │  │                  ▼                         │
         │  │       ┌─────────────────────┐              │
         │  └──────▶│       HOLD          │──────────────┘
         │          │  (0.60 ≤ Material   │
         │          │   < 0.90, stable)   │
         │          └──────────┬──────────┘
         │                     │
         │ Score drops         │ Score drops further
         │                     ▼
         │          ┌─────────────────────┐
         └─────────▶│  DEFENSIVE FALLBACK │
                    │  (Material < 0.60,  │
                    │   retreat+regroup)  │
                    └──────────┬──────────┘
                               │ Critical damage
                               ▼
                    ┌─────────────────────┐
                    │      RETREAT        │
                    │  (Material < 0.35   │
                    │   OR HP < 0.30)     │
                    └─────────────────────┘

Hysteresis: Current stance thresholds widened by ±0.08
Stance Dwell: Minimum 2 turns (except Retreat and Hunt exit)
```

## Components and Interfaces

### AIController (MonoBehaviour Orchestrator)

**Responsibility**: Main orchestrator that subscribes to GameMaster events and coordinates AI subsystems.

**Key Members**:
```csharp
public class AIController : MonoBehaviour
{
    // Configuration
    [SerializeField] private RuntimeParticipant assignedParticipant;
    
    // Subsystem references
    private GameMaster gameMaster;
    private FleetCommander fleetCommander;
    
    // Turn state (cleared each phase)
    private HashSet<ShipInstance> claimedTargets;
    private HashSet<string> movedUnitIDs;
    private int movesCommitted;
    
    // Lifecycle
    void Awake() { /* Find GameMaster */ }
    void OnEnable() { /* Subscribe to PhaseChanged */ }
    void OnDisable() { /* Unsubscribe */ }
    
    // Phase handlers
    void OnPhaseChanged(GameplayPhase phase, RuntimeParticipant activeParticipant);
    void HandleDeploymentPhase();
    void HandleMovementPhase();
    void HandleSearchPhase();
    void HandleBattlePhase();
    
    // Movement processing
    void ProcessMovementPhase();
    bool TryMoveUnit(RuntimeUnit unit, AITurnContext context, FleetSnapshot snapshot);
    
    // Utilities
    AITurnContext BuildTurnContext();
}
```

**State Management**:
- `claimedTargets`: Tracks which enemies are being approached by moving units (prevents convergence)
- `movedUnitIDs`: Tracks successfully moved units to prevent duplicate attempts
- `movesCommitted`: Counter checked against MovementLimit
- All turn state cleared at phase start

**Error Handling**:
- Validation failures: Log message, mark destination invalid, try alternative
- Commit failures: Log message, continue to next unit
- No valid destinations for unit: Skip unit, continue processing
- GameMaster API errors: Log diagnostics, gracefully degrade

### FleetCommander (Strategic Layer)

**Responsibility**: Evaluates fleet state, selects strategic stance, generates immutable fleet snapshots, and manages enemy alive estimates.

**Key Members**:
```csharp
public class FleetCommander
{
    // Configuration constants
    private const float RetreatScoreBelow = 0.35f;
    private const float RetreatHpBelow = 0.30f;
    private const float FallbackScoreBelow = 0.60f;
    private const float AdvanceScoreAtLeast = 0.90f;
    private const float HysteresisMargin = 0.08f;
    private const int MinDwellTurns = 2;
    
    // State
    private readonly GridManager gridManager;
    private readonly PlayerId owner;
    private readonly int startingFleetSize;
    private int enemyAliveEstimate;
    private Vector2Int homeCentroid;
    private bool homeSet;
    private int turnsInStance;
    
    public FleetStance CurrentStance { get; private set; }
    public FleetSnapshot LastSnapshot { get; private set; }
    
    // Constructor
    public FleetCommander(GridManager gridManager, PlayerId owner);
    
    // Main evaluation (called at Move phase start)
    public FleetSnapshot Evaluate(AITurnContext context, FleetStance? debugForceStance = null);
    
    // Battle phase refresh (updates contacts without stance change)
    public FleetSnapshot RefreshContacts(AITurnContext context);
    
    // Kill tracking
    public void NoteKillConfirmed();
    
    // Target prioritization for Battle phase
    public List<ShipInstance> PrioritizeTargets(List<ShipInstance> knownEnemies, GridManager gridManager);
    
    // Internal methods
    private FleetSnapshot BuildSnapshot(AITurnContext context);
    private FleetStance ChooseStance(FleetSnapshot snapshot);
    private bool IsLockedByDwell(FleetStance desired);
    private float CalculateThreat(ShipInstance enemy, GridManager gridManager);
}
```

**Stance Selection Algorithm**:
```
MaterialScore = OwnAlive / (OwnStartCount + EnemyEstimate)
FleetHpFraction = Sum(currentHealth) / Sum(maxHealth)

// Apply hysteresis: current stance gets +/- HysteresisMargin
hysteresis = (currentStance == candidateStance) ? HysteresisMargin : 0

if MaterialScore < RetreatScoreBelow + hysteresis OR FleetHpFraction < RetreatHpBelow + hysteresis:
    return Retreat
    
if MaterialScore < FallbackScoreBelow + hysteresis:
    return DefensiveFallback
    
if contacts.Count == 0:
    return Hunt
    
if MaterialScore >= AdvanceScoreAtLeast - hysteresis:
    return Advance
    
return Hold
```

**Stance Dwell Enforcement**:
- Minimum 2 turns in any stance except Hunt and Retreat
- Retreat transitions happen immediately (safety override)
- Hunt exits happen immediately (information arrived)
- Other transitions blocked until dwell counter reaches MinDwellTurns

### MovementPrioritizer (Tactical Priority System)

**Responsibility**: Ranks all alive units by movement priority to determine which units move when only N of M can move.

**Key Members**:
```csharp
public static class MovementPrioritizer
{
    // Weight constants
    private const float RoleWeightScout = 50f;
    private const float RoleWeightStriker = 30f;
    private const float RoleWeightAnchor = 20f;
    private const float ThreatProximityWeight = 40f;
    private const float AttackOpportunityWeight = 35f;
    private const float DamagedPenalty = -20f;
    private const float BlockedPenalty = -100f;
    
    // Main API
    public static List<RuntimeUnit> PrioritizeUnits(
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager);
    
    // Priority calculation
    private static float CalculatePriority(
        RuntimeUnit unit,
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager);
    
    // Component calculations
    private static float GetRoleWeight(UnitRole role);
    private static float CalculateThreatProximity(RuntimeUnit unit, FleetSnapshot snapshot);
    private static float CalculateAttackOpportunity(RuntimeUnit unit, FleetSnapshot snapshot, GridManager gridManager);
    private static float CalculateHealthFactor(RuntimeUnit unit);
    private static float GetStanceModifier(RuntimeUnit unit, FleetSnapshot snapshot);
}
```

**Priority Scoring Formula**:
```
Priority = RoleWeight + ThreatProximity + AttackOpportunity + HealthFactor + StanceModifier

RoleWeight:
  Scout:   50 (highest - scouts move first to reveal fog)
  Striker: 30 (medium - flexible tactical units)
  Anchor:  20 (lowest - slow, defensive units move last)

ThreatProximity (max 40 points):
  nearestEnemyDistance = min(distance to each contact)
  proximity = 1.0 - (nearestEnemyDistance / mapDiagonal)
  score = proximity * ThreatProximityWeight

AttackOpportunity (max 35 points):
  For each weapon slot:
    If any enemy within weapon range:
      score += (expectedDamage / maxHealth) * AttackOpportunityWeight

HealthFactor:
  If currentHealth < maxHealth * 0.4:
    factor = -20 (damaged units deprioritized unless Retreat)
  Else:
    factor = 0

StanceModifier:
  Retreat:
    If unit.currentHealth < maxHealth * 0.4:
      modifier = +60 (damaged units move first in Retreat)
    Else:
      modifier = 0
      
  Advance/Hunt:
    If unit.role == Scout:
      modifier = +30 (scouts lead the advance)
    Else if nearestEnemy < 8 tiles:
      modifier = +20 (close units advance)
      
  DefensiveFallback:
    If unit at edge of formation:
      modifier = +25 (edge units reposition first)
      
  Hold:
    modifier = 0 (default priority only)
```

**Blockage Detection**:
- Before returning sorted list, validate each high-priority unit has at least one reachable tile
- Units with no valid destinations marked as blocked
- Blocked units moved to end of priority list
- Allows AI to skip blocked units and use movement slots for unblocked units

### MovementPlanner (Tactical Destination Selection)

**Responsibility**: Scores potential destination tiles for a single unit and selects the best destination that balances offense, safety, and formation.

**Key Members**:
```csharp
public static class MovementPlanner
{
    // Tile scoring weights (scaled as fractions of unit maxHealth)
    private const float KillBonus = 50f;
    private const float MaxDamageScore = 40f;
    private const float TargetWeaknessWeight = 10f;
    private const float DangerWeight = 35f;
    private const float SelfRiskWeight = 15f;
    private const float AlreadyClaimedPenalty = 20f;
    private const float CohesionBaseWeight = 8f;
    private const float FragilityPushbackWeight = 12f;
    private const float ScoutForwardWeight = 6f;
    private const float FragilityThreshold = 0.6f;
    private const float RetreatDirectionalWeight = 18f;
    private const float FallbackDirectionalWeight = 10f;
    private const float AdvanceDirectionalWeight = 10f;
    
    // Main API
    public struct Decision
    {
        public Vector2Int destination;
        public ShipInstance likelyTarget;  // null if repositioning
        public bool isFleeing;
    }
    
    public static Decision ChooseDestination(
        RuntimeUnit unit,
        AITurnContext context,
        FleetSnapshot snapshot,
        GameMovement gameMovement,
        GridManager gridManager,
        HashSet<ShipInstance> claimedTargets);
    
    // Tile scoring
    private static float ScoreTile(
        Vector2Int tile,
        RuntimeUnit unit,
        ShipInstance targetEnemy,
        AITurnContext context,
        FleetSnapshot snapshot,
        GridManager gridManager,
        HashSet<ShipInstance> claimedTargets);
    
    // Component scoring
    private static float ScoreAttackFromTile(
        Vector2Int tile,
        RuntimeUnit unit,
        ShipInstance enemy,
        GridManager gridManager,
        HashSet<ShipInstance> claimedTargets);
    
    private static float DangerAtTile(
        Vector2Int tile,
        RuntimeUnit unit,
        AITurnContext context,
        GridManager gridManager);
    
    private static float ScoreFormationTerms(
        Vector2Int tile,
        RuntimeUnit unit,
        FleetSnapshot snapshot,
        GridManager gridManager);
    
    private static float ScoreDirectionalBias(
        Vector2Int tile,
        RuntimeUnit unit,
        FleetSnapshot snapshot,
        GridManager gridManager);
    
    // Utilities
    private static List<Vector2Int> GetPlaceableReachableTiles(
        RuntimeUnit unit,
        GameMovement gameMovement,
        GridManager gridManager);
    
    private static Vector2Int FindSafestTile(
        List<Vector2Int> candidates,
        RuntimeUnit unit,
        AITurnContext context,
        GridManager gridManager);
}
```

**Tile Scoring Algorithm**:
```
TileScore = AttackOpportunity - Danger + Cohesion + RoleBias + DirectionalBias

AttackOpportunity (0 to ~50 points):
  For each enemy in weapon range from tile:
    weapon = SelectBestWeapon(unit, enemy)
    expectedDamage = weapon.ExpectedValue()
    
    if expectedDamage >= enemy.currentHealth:
      score += KillBonus (50)
    else:
      score += (expectedDamage / unit.maxHealth) * MaxDamageScore (max 40)
      
    if claimedTargets.Contains(enemy):
      score -= AlreadyClaimedPenalty (20)

Danger (-50 to 0 points):
  For each enemy unit:
    For each enemy weapon:
      if tile within enemy weapon range:
        expectedIncoming = weapon.ExpectedValue()
        danger += (expectedIncoming / unit.maxHealth) * DangerWeight (35)
        
  score -= danger
  
  if stance == Retreat or DefensiveFallback:
    score -= danger * 0.5  // Extra danger penalty

Cohesion (-10 to +10 points):
  distToFleet = Distance(tile, snapshot.FleetCentroid)
  cohesion = -distToFleet * (CohesionBaseWeight / mapDiagonal)
  
  if stance == Retreat or DefensiveFallback:
    cohesion *= 2.0  // Pull damaged fleet together
  else if stance == Advance or Hunt:
    cohesion *= 0.5  // Allow spread
    
  if unit.role == Scout and stance == Hunt:
    cohesion *= -1.0  // Push scouts forward
    
  if unit.currentHealth < unit.maxHealth * 0.4:
    cohesion *= 1.5  // Pull damaged units to safety
    
  score += cohesion

RoleBias (0 to +6 points):
  if unit.role == Scout:
    distToNearestEnemy = Distance(tile, nearestContact)
    if stance == Hunt or Advance:
      score += (1.0 - distToNearestEnemy / mapDiagonal) * ScoutForwardWeight
      
  if unit.EffectiveFragility > FragilityThreshold:
    distToNearestEnemy = Distance(tile, nearestContact)
    score -= (1.0 - distToNearestEnemy / mapDiagonal) * FragilityPushbackWeight

DirectionalBias (stance-driven fleet movement):
  if stance == Retreat:
    directionToHome = Vector(tile, snapshot.HomeCentroid)
    score += dot(directionToHome, tileVector) * RetreatDirectionalWeight
    
  if stance == DefensiveFallback:
    directionToHome = Vector(tile, snapshot.HomeCentroid)
    score += dot(directionToHome, tileVector) * FallbackDirectionalWeight
    
  if stance == Advance:
    directionToEnemy = Vector(tile, nearestContact)
    score += dot(directionToEnemy, tileVector) * AdvanceDirectionalWeight
```

**Flee Logic**:
```
isCriticallyLow = (unit.currentHealth / unit.maxHealth) <= 0.3
bestIsLethal = (bestTileTarget != null AND expectedDamage >= target.currentHealth)

if isCriticallyLow AND NOT bestIsLethal:
    // Override: find safest tile regardless of attack opportunity
    safestTile = FindSafestTile(candidates, unit, context, gridManager)
    return Decision { destination = safestTile, likelyTarget = null, isFleeing = true }
else:
    return Decision { destination = bestTile, likelyTarget = bestTarget, isFleeing = false }
```

### DeploymentPlanner (Initial Placement Strategy)

**Responsibility**: Analyzes deployment zone geometry and distributes units across sectors with appropriate spacing.

**Key Members**:
```csharp
public static class DeploymentPlanner
{
    // Configuration
    private const int MinimumSpacing = 2;
    private const int SpacingRelaxationStep = 1;
    
    // Sector definitions
    private enum Sector { Left, Center, Right }
    
    // Main API
    public static bool ExecuteDeployment(
        AITurnContext context,
        FleetSnapshot snapshot,
        GameDeployment gameDeployment,
        GridManager gridManager);
    
    // Zone analysis
    private static Vector2Int CalculateDeploymentCentroid(
        List<Vector2Int> deploymentZone);
    
    private static Dictionary<Sector, List<Vector2Int>> DivideIntoSectors(
        List<Vector2Int> deploymentZone,
        Vector2Int centroid,
        Vector2Int enemyDirection);
    
    private static Vector2Int EstimateEnemyDirection(
        Vector2Int deploymentCentroid,
        GridManager gridManager);
    
    // Placement
    private static Dictionary<RuntimeUnit, Sector> AssignUnitsToSectors(
        List<RuntimeUnit> units,
        FleetSnapshot snapshot);
    
    private static bool TryPlaceUnit(
        RuntimeUnit unit,
        Sector sector,
        Dictionary<Sector, List<Vector2Int>> sectorTiles,
        List<Vector2Int> occupiedPositions,
        GameDeployment gameDeployment,
        GridManager gridManager,
        UnitFacing facing,
        out Vector2Int placedPosition);
    
    private static UnitFacing CalculateFacingToward(
        Vector2Int enemyDirection);
}
```

**Deployment Algorithm**:
```
1. Query GameDeployment for deployment zone tile list

2. Calculate deployment zone centroid:
   centroid = Mean(all deployment zone tiles)

3. Estimate enemy direction:
   Find opposite edge of map from centroid
   enemyDirection = Vector(centroid, oppositeEdge)

4. Divide deployment zone into 3 sectors:
   For each tile in zone:
     perpendicularAxis = Perpendicular(enemyDirection)
     offset = dot(tile - centroid, perpendicularAxis)
     
     if offset < -threshold:
       sector = Left
     else if offset > threshold:
       sector = Right
     else:
       sector = Center

5. Assign units to sectors based on role:
   For each unit:
     if unit.role == Anchor:
       assignedSector[unit] = Center
     else if unit.role == Scout:
       assignedSector[unit] = (unitIndex % 2 == 0) ? Left : Right
     else:  // Striker
       assignedSector[unit] = LeastPopulatedSector()

6. Place units with spacing:
   currentSpacing = MinimumSpacing
   
   For each unit in assignedSector order:
     placed = false
     
     while NOT placed AND currentSpacing >= 0:
       For each tile in unit's assigned sector:
         if Distance(tile, occupiedPositions) >= currentSpacing:
           if GameDeployment.ValidatePlacement(unit, tile, facing):
             if GameDeployment.TrySubmitPlacement(unit, tile, facing):
               occupiedPositions.Add(tile)
               placed = true
               break
       
       if NOT placed:
         currentSpacing -= SpacingRelaxationStep
     
     if NOT placed:
       Log error for diagnostic purposes

7. Return success if all units placed
```

### AITurnContext (Per-Turn Snapshot Builder)

**Responsibility**: Builds a consistent view of game state at the start of each phase by querying GameMaster APIs.

**Structure**:
```csharp
public class AITurnContext
{
    // Participant identity
    public RuntimeParticipant OwnParticipant { get; set; }
    public RuntimeParticipant EnemyParticipant { get; set; }
    
    // Unit collections
    public List<RuntimeUnit> OwnUnits { get; set; }
    public List<RuntimeUnit> OwnAliveUnits { get; set; }
    public List<RuntimeUnit> EnemyVisibleUnits { get; set; }
    
    // Map and grid
    public GameMap GameMap { get; set; }
    public GridManager GridManager { get; set; }
    
    // Phase context
    public GameplayPhase CurrentPhase { get; set; }
    public int TurnNumber { get; set; }
    
    // Movement phase specific
    public int MovementLimit { get; set; }
    public int MovesCommitted { get; set; }
    public bool CanAct { get; set; }
    
    // Factory method
    public static AITurnContext Build(
        GameMaster gameMaster,
        RuntimeParticipant ownParticipant);
}
```

**Construction Algorithm**:
```csharp
public static AITurnContext Build(GameMaster gameMaster, RuntimeParticipant ownParticipant)
{
    var context = new AITurnContext();
    context.OwnParticipant = ownParticipant;
    context.CurrentPhase = gameMaster.CurrentPhase;
    context.TurnNumber = gameMaster.TurnNumber;
    
    // Query units
    context.OwnUnits = new List<RuntimeUnit>();
    context.OwnAliveUnits = new List<RuntimeUnit>();
    
    foreach (var unit in gameMaster.GameUnits.GetAllUnits())
    {
        if (unit.Participant == ownParticipant)
        {
            context.OwnUnits.Add(unit);
            if (unit.LifeState == RuntimeUnitLifeState.Active)
            {
                context.OwnAliveUnits.Add(unit);
            }
        }
    }
    
    // Query visible enemies
    context.EnemyVisibleUnits = new List<RuntimeUnit>();
    foreach (var unit in gameMaster.GameUnits.GetAllUnits())
    {
        if (unit.Participant != ownParticipant)
        {
            if (gameMaster.GameKnowledge.IsUnitVisibleToParticipant(unit, ownParticipant))
            {
                context.EnemyVisibleUnits.Add(unit);
            }
        }
    }
    
    // Query movement phase state
    if (context.CurrentPhase == GameplayPhase.Move)
    {
        var movementView = gameMaster.GameMovement.GetParticipantView(ownParticipant);
        context.MovementLimit = movementView.MovementLimit;
        context.MovesCommitted = movementView.MovesCommitted;
        context.CanAct = movementView.CanAct;
    }
    
    context.GameMap = gameMaster.GameMap;
    context.GridManager = FindGridManager();  // Scene lookup
    
    return context;
}
```

### FleetSnapshot (Immutable Fleet State)

**Responsibility**: Immutable snapshot of fleet strategic state generated once per turn and shared across all subsystems.

**Structure**:
```csharp
public class FleetSnapshot
{
    // Ownership
    public PlayerId Owner { get; set; }
    
    // Strategic state
    public FleetStance CurrentStance { get; set; }
    public int TurnsInStance { get; set; }
    
    // Force composition
    public int OwnStartCount { get; set; }
    public int OwnAlive { get; set; }
    public int EnemyAliveEstimate { get; set; }
    public float MaterialScore { get; set; }      // OwnAlive / (OwnStartCount + EnemyEstimate)
    public float FleetHpFraction { get; set; }    // Sum(currentHP) / Sum(maxHP)
    
    // Positions
    public Vector2Int FleetCentroid { get; set; }
    public Vector2Int HomeCentroid { get; set; }
    public float MapProgress { get; set; }        // Distance from home / map diagonal
    
    // Contacts
    public List<RuntimeUnit> Contacts { get; set; }
    public float NearestContactDistance { get; set; }
    
    // Unit details
    public List<FleetMember> Members { get; set; }
}

public class FleetMember
{
    public RuntimeUnit Unit { get; set; }
    public UnitRole Role { get; set; }
    public float HpFraction { get; set; }
    public float EffectiveFragility { get; set; }  // Fragility considering role
    public Vector2Int Position { get; set; }
    public UnitFacing Facing { get; set; }
}

public enum UnitRole
{
    Scout,    // Fast, long-range reconnaissance
    Anchor,   // Durable, defensive line holders
    Striker   // Balanced tactical units
}

public enum FleetStance
{
    Hunt,              // Explore, no contacts visible
    Advance,           // Strong position, press advantage
    Hold,              // Balanced, maintain position
    DefensiveFallback, // Weakened, regroup defensively
    Retreat            // Critical damage, disengage
}
```

**Role Classification Logic**:
```csharp
public static UnitRole ClassifyRole(RuntimeUnit unit)
{
    var definition = unit.Definition;
    
    int maxSpeed = definition.MovementBudget;
    int maxRange = GetMaxWeaponRange(unit);
    int totalHP = definition.MaxHealth;
    int weaponCount = CountEquippedWeapons(unit);
    
    // Scout: Fast + Long Range
    if (maxSpeed >= 6 && maxRange >= 4)
    {
        return UnitRole.Scout;
    }
    
    // Anchor: Durable + Multi-weapon
    if (totalHP >= 80 && weaponCount >= 2)
    {
        return UnitRole.Anchor;
    }
    
    // Default: Striker
    return UnitRole.Striker;
}
```

**EffectiveFragility Calculation**:
```csharp
// Fragility = 1.0 - (currentHP / maxHP), adjusted by role
// Scouts are inherently more fragile (squishy), Anchors less so
public static float CalculateEffectiveFragility(RuntimeUnit unit, UnitRole role)
{
    float rawFragility = 1.0f - ((float)unit.CurrentHealth / unit.MaxHealth);
    
    float roleMultiplier = role switch
    {
        UnitRole.Scout => 1.3f,   // Scouts feel fragility more
        UnitRole.Anchor => 0.7f,  // Anchors tolerate more damage
        UnitRole.Striker => 1.0f  // No adjustment
    };
    
    return Mathf.Clamp01(rawFragility * roleMultiplier);
}
```

### EnemyMemory (Position Tracking and Prediction)

**Responsibility**: Tracks last known positions of enemy units and predicts likely current positions when units are hidden by fog.

**Structure**:
```csharp
public static class EnemyMemory
{
    private const int PredictionExpirationTurns = 5;
    
    private class MemoryEntry
    {
        public RuntimeUnit Unit { get; set; }
        public Vector2Int LastKnownPosition { get; set; }
        public UnitFacing LastKnownFacing { get; set; }
        public int LastSeenTurn { get; set; }
        public Vector2Int PredictedPosition { get; set; }
        public bool IsValid { get; set; }
    }
    
    // Storage: keyed by (ParticipantID, UnitID)
    private static Dictionary<(string, string), MemoryEntry> memory = 
        new Dictionary<(string, string), MemoryEntry>();
    
    // Public API
    public static void UpdateContacts(
        RuntimeParticipant observer,
        List<RuntimeUnit> visibleEnemies,
        int currentTurn);
    
    public static List<Vector2Int> GetPredictedPositions(
        RuntimeParticipant observer,
        int currentTurn);
    
    public static void ClearForParticipant(RuntimeParticipant observer);
    
    public static void ClearAll();
    
    // Internal
    private static Vector2Int PredictPosition(
        MemoryEntry entry,
        int currentTurn);
}
```

**Update Algorithm**:
```csharp
public static void UpdateContacts(RuntimeParticipant observer, List<RuntimeUnit> visibleEnemies, int currentTurn)
{
    // Update entries for visible enemies
    foreach (var enemy in visibleEnemies)
    {
        var key = (observer.ID, enemy.RuntimeID);
        
        if (memory.ContainsKey(key))
        {
            var entry = memory[key];
            entry.LastKnownPosition = enemy.CurrentTileCoordinate;
            entry.LastKnownFacing = enemy.CurrentFacing;
            entry.LastSeenTurn = currentTurn;
            entry.PredictedPosition = enemy.CurrentTileCoordinate;
            entry.IsValid = true;
        }
        else
        {
            memory[key] = new MemoryEntry
            {
                Unit = enemy,
                LastKnownPosition = enemy.CurrentTileCoordinate,
                LastKnownFacing = enemy.CurrentFacing,
                LastSeenTurn = currentTurn,
                PredictedPosition = enemy.CurrentTileCoordinate,
                IsValid = true
            };
        }
    }
    
    // Expire old predictions
    var keysToRemove = new List<(string, string)>();
    foreach (var kvp in memory)
    {
        if (kvp.Value.IsValid && (currentTurn - kvp.Value.LastSeenTurn) > PredictionExpirationTurns)
        {
            kvp.Value.IsValid = false;
            keysToRemove.Add(kvp.Key);
        }
    }
    
    foreach (var key in keysToRemove)
    {
        memory.Remove(key);
    }
}
```

**Prediction Algorithm**:
```csharp
private static Vector2Int PredictPosition(MemoryEntry entry, int currentTurn)
{
    int turnsElapsed = currentTurn - entry.LastSeenTurn;
    
    if (turnsElapsed == 0)
    {
        return entry.LastKnownPosition;
    }
    
    // Simple linear prediction based on facing direction
    // Assumes enemy moved at half speed in facing direction
    int assumedSpeed = 3;  // Conservative estimate
    int predictedDistance = Mathf.Min(turnsElapsed * assumedSpeed, 10);  // Cap at 10 tiles
    
    Vector2Int direction = entry.LastKnownFacing switch
    {
        UnitFacing.North => Vector2Int.up,
        UnitFacing.East => Vector2Int.right,
        UnitFacing.South => Vector2Int.down,
        UnitFacing.West => Vector2Int.left,
        _ => Vector2Int.zero
    };
    
    return entry.LastKnownPosition + (direction * predictedDistance);
}
```

## Data Models

### Key Enumerations

```csharp
public enum GameplayPhase
{
    None,
    Deployment,
    Move,
    Search,
    Battle
}

public enum UnitRole
{
    Scout,    // maxSpeed >= 6 AND maxRange >= 4
    Anchor,   // maxHealth >= 80 AND weaponCount >= 2
    Striker   // Default
}

public enum FleetStance
{
    Hunt,              // No contacts, explore
    Advance,           // Material >= 0.90, press attack
    Hold,              // 0.60 <= Material < 0.90, maintain
    DefensiveFallback, // Material < 0.60, regroup
    Retreat            // Material < 0.35 OR HP < 0.30, flee
}

public enum UnitFacing
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public enum RuntimeUnitLifeState
{
    Active,
    Destroyed
}
```

### GameMaster API Data Structures

The AI system interacts with these GameMaster types:

```csharp
// From GameMaster
public class RuntimeParticipant { /* Player identity */ }
public class RuntimeUnit { /* Active unit instance */ }
public class Definition_Unit { /* Unit template */ }
public class RuntimeWeaponSlotState { /* Weapon state */ }

// From GameMovement
public class GameMovement.ParticipantView
{
    public bool IsMovementPhaseActive { get; }
    public int MovementLimit { get; }
    public int MovesCommitted { get; }
    public bool MovementEnded { get; }
    public bool CanAct { get; }
    public float SecondsRemaining { get; }
    public UnitView[] Units { get; }
}

public class GameMovement.MovementPlan
{
    public string RuntimeUnitID { get; }
    public Vector2Int StartingMovementPivotCoordinate { get; }
    public UnitFacing StartingFacing { get; }
    public Vector2Int DestinationMovementPivotCoordinate { get; }
    public UnitFacing DestinationFacing { get; }
    public bool WouldConsumeMove { get; }
    public RouteStep[] Route { get; }
}

public class GameMovement.CommitResult
{
    public CommitOutcome Outcome { get; }
    public string RuntimeUnitID { get; }
    public Vector2Int FinalMovementPivotCoordinate { get; }
    public UnitFacing FinalFacing { get; }
    public bool MoveConsumed { get; }
    public RouteStep[] TraveledRoute { get; }
}

// From GameDeployment
public class GameDeployment.ParticipantView
{
    public int TotalPlacements { get; }
    public int CompletedPlacements { get; }
    public bool IsComplete { get; }
}

// From GameKnowledge
public bool IsUnitVisibleToParticipant(RuntimeUnit unit, RuntimeParticipant observer);
```

## Error Handling

### Validation Failure Recovery

**Movement Validation Failure**:
```csharp
bool TryMoveUnit(RuntimeUnit unit, AITurnContext context, FleetSnapshot snapshot)
{
    var decision = MovementPlanner.ChooseDestination(unit, context, snapshot, ...);
    
    var validationResult = gameMaster.GameMovement.ValidateMovement(
        context.OwnParticipant,
        unit.RuntimeID,
        decision.destination);
    
    if (!validationResult.IsValid)
    {
        Debug.LogWarning($"[AI] Movement validation failed for {unit.RuntimeID}: {validationResult.Message}");
        
        // Try alternative destinations in descending score order
        var alternatives = MovementPlanner.GetAlternativeDestinations(unit, context, snapshot, ...);
        
        foreach (var alt in alternatives)
        {
            var altValidation = gameMaster.GameMovement.ValidateMovement(
                context.OwnParticipant,
                unit.RuntimeID,
                alt.destination);
            
            if (altValidation.IsValid)
            {
                decision = alt;
                break;
            }
        }
        
        if (!validationResult.IsValid)
        {
            Debug.LogWarning($"[AI] No valid destinations found for {unit.RuntimeID}, skipping");
            return false;
        }
    }
    
    return TryCommitMovement(unit, decision);
}
```

**Commit Failure Recovery**:
```csharp
bool TryCommitMovement(RuntimeUnit unit, MovementPlanner.Decision decision)
{
    var commitResult = gameMaster.GameMovement.TryCommitMovement(
        context.OwnParticipant,
        unit.RuntimeID,
        decision.destination);
    
    if (commitResult.Outcome != CommitOutcome.Completed)
    {
        Debug.LogWarning($"[AI] Movement commit failed for {unit.RuntimeID}: {commitResult.Message}");
        Debug.LogWarning($"[AI] Outcome: {commitResult.Outcome}, Final position: {commitResult.FinalMovementPivotCoordinate}");
        return false;
    }
    
    // Success: update tracking
    movedUnitIDs.Add(unit.RuntimeID);
    movesCommitted++;
    
    if (decision.likelyTarget != null)
    {
        claimedTargets.Add(decision.likelyTarget);
    }
    
    Debug.Log($"[AI] Successfully moved {unit.RuntimeID} to {commitResult.FinalMovementPivotCoordinate}");
    return true;
}
```

### Graceful Degradation Strategies

**No Valid Destinations**:
- Log diagnostic message
- Skip unit
- Continue processing remaining units
- Do NOT halt entire AI turn

**All High-Priority Units Blocked**:
- MovementPrioritizer marks blocked units
- AIController skips blocked units
- Lower-priority unblocked units get movement slots
- Movement slots not wasted on units that can't move

**Zero Visibility (No Contacts)**:
- FleetCommander selects Hunt stance
- MovementPlanner adds exploration bonus to tile scoring
- EnemyMemory predictions used as secondary targets
- If no predictions: bias movement toward map center or edges
- AI continues to function in exploration mode

**GameMaster API Errors**:
```csharp
void OnPhaseChanged(GameplayPhase phase, RuntimeParticipant activeParticipant)
{
    try
    {
        if (activeParticipant != assignedParticipant)
        {
            return;  // Not our turn
        }
        
        if (phase == GameplayPhase.Move)
        {
            HandleMovementPhase();
        }
        // ... other phases
    }
    catch (Exception ex)
    {
        Debug.LogError($"[AI] Exception during phase {phase}: {ex.Message}");
        Debug.LogError($"[AI] Stack trace: {ex.StackTrace}");
        // Gracefully degrade: AI does nothing this phase rather than crashing game
    }
}
```

**Memory Prediction Expiration**:
- Predictions older than 5 turns automatically invalidated
- Invalid predictions removed from consideration
- Danger calculations and tile scoring fall back to visible contacts only
- AI operates conservatively when information is stale

## Correctness Properties

**Note on Property-Based Testing**: This feature is **NOT suitable for property-based testing** because it is an infrastructure integration system with external dependencies (GameMaster APIs), side effects (state modifications), and non-deterministic behavior (dynamic game state). The properties below document the correct behavior that will be validated through unit tests and integration tests instead.

### Architecture Properties

**Property 1: Movement Limit Compliance**

*For any* AI turn with N alive units, the AI SHALL NOT commit more than (N + 1) / 2 movements

**Validates: Requirement 1.1, 1.2, 1.3, 1.4**

**Property 2: Fog-of-War Isolation**

*For any* decision made by the AI, the decision SHALL consider only units that are visible to the AI's participant according to GameKnowledge

**Validates: Requirement 4.1, 4.2, 4.3**

**Property 3: Participant API Filtering**

*For any* GameMaster API call made by the AI, the call SHALL include the AI's assigned RuntimeParticipant identity

**Validates: Requirement 3.1, 3.2, 3.3, 3.4**

### Strategic Properties

**Property 4: Stance Hysteresis**

*For any* stance transition, if the MaterialScore moves back within the current stance's threshold (± HysteresisMargin), the AI SHALL maintain the current stance

**Validates: Requirement 5.8**

**Property 5: Stance Dwell Enforcement**

*For any* non-Retreat stance, the AI SHALL maintain that stance for at least MinDwellTurns before transitioning (except when transitioning to Retreat or exiting Hunt)

**Validates: Requirement 5.9, 5.10**

**Property 6: Retreat Immediacy**

*For any* turn where MaterialScore < RetreatScoreBelow OR FleetHpFraction < RetreatHpBelow, the AI SHALL select Retreat stance regardless of current stance or dwell counter

**Validates: Requirement 5.3**

### Tactical Properties

**Property 7: Priority Ordering**

*For any* two units A and B where Priority(A) > Priority(B), the AI SHALL attempt to move unit A before unit B (unless A is blocked)

**Validates: Requirement 2.7, 2.8, 15.1**

**Property 8: Scout Priority in Advance**

*For any* unit with role Scout when FleetStance is Advance or Hunt, the unit SHALL receive higher movement priority than non-Scout units with equivalent tactical scores

**Validates: Requirement 2.6**

**Property 9: Damaged Priority in Retreat**

*For any* unit where currentHealth < maxHealth * 0.4 when FleetStance is Retreat, the unit SHALL receive higher movement priority

**Validates: Requirement 2.5**

**Property 10: Target Spread**

*For any* two units A and B that both move during the same turn, if unit A approaches enemy X, then unit B SHALL prefer approaching a different enemy Y (unless X is the only target in range or X is a guaranteed kill)

**Validates: Requirement 10.4, 10.6**

**Property 11: Flee Override**

*For any* unit where currentHealth / maxHealth <= FleeHealthFraction AND no lethal attack is available, the AI SHALL select the safest destination regardless of attack opportunity score

**Validates: Requirement 28.1, 28.4**

### Formation Properties

**Property 12: Cohesion in Fallback**

*For any* unit movement when FleetStance is DefensiveFallback or Retreat, tiles closer to the fleet centroid SHALL receive higher cohesion scores than tiles farther away

**Validates: Requirement 9.2, 9.3**

**Property 13: Scout Forward Bias**

*For any* Scout unit movement when FleetStance is Hunt, tiles farther from fleet centroid SHALL receive positive cohesion adjustment (opposite of normal cohesion)

**Validates: Requirement 9.4**

**Property 14: Deployment Sector Assignment**

*For any* unit deployment, Anchor units SHALL be assigned to the center sector, Scout units SHALL be distributed between left and right sectors

**Validates: Requirement 12.1, 12.2**

### Memory and Prediction Properties

**Property 15: Memory Update on Visibility**

*For any* enemy unit that becomes visible, EnemyMemory SHALL record its current position and facing as the last known state

**Validates: Requirement 4.4**

**Property 16: Prediction Expiration**

*For any* EnemyMemory prediction older than 5 turns, the prediction SHALL be marked invalid and excluded from decision-making

**Validates: Requirement 4.7**

**Property 17: Prediction Refresh**

*For any* hidden enemy that becomes visible again, EnemyMemory SHALL update the last known position to the current visible position and reset the turn counter

**Validates: Requirement 4.8**

### Error Handling Properties

**Property 18: Validation Failure Recovery**

*For any* movement that fails validation, the AI SHALL attempt alternative destinations in descending score order before skipping the unit

**Validates: Requirement 20.2, 20.3**

**Property 19: Commit Failure Isolation**

*For any* movement commit that fails, the AI SHALL log the failure and continue processing remaining units without halting

**Validates: Requirement 20.4, 20.5**

**Property 20: Zero Contact Graceful Operation**

*For any* turn where no enemy contacts are visible, the AI SHALL select Hunt stance and bias movement toward exploration rather than erroring or halting

**Validates: Requirement 25.1, 25.2, 25.4**

## Testing Strategy

### Unit Testing

**Priority Scoring Tests**:
- Test priority calculation for each UnitRole
- Test stance modifiers (Retreat prioritizes damaged, Advance prioritizes scouts)
- Test threat proximity calculation
- Test attack opportunity scoring
- Test blockage detection
- Verify priority ordering is stable and deterministic

**Tile Scoring Tests**:
- Test attack opportunity calculation with various weapon ranges
- Test danger calculation with multiple threatening enemies
- Test cohesion scoring with various fleet centroids
- Test role bias (scout forward bonus, fragility pushback)
- Test directional bias for each stance
- Test flee logic override for critically damaged units
- Verify scores are comparable across different unit health scales

**Stance Selection Tests**:
- Test threshold triggers for each stance
- Test hysteresis prevents rapid flipping
- Test stance dwell enforcement
- Test immediate Retreat on critical damage
- Test Hunt selection when no contacts visible
- Verify MaterialScore and FleetHpFraction calculations

**Deployment Tests**:
- Test sector division for various deployment zone shapes
- Test role-based sector assignment (Anchors center, Scouts edges)
- Test spacing enforcement and relaxation
- Test facing calculation toward enemy
- Mock GameDeployment validation/commit workflow

### Integration Testing

**GameMaster API Integration**:
- Test phase event subscription and handling
- Test participant identity validation
- Test movement validation/commit workflow
- Test deployment validation/commit workflow
- Test fog-of-war queries
- Verify no unauthorized API access

**Sequential Movement Processing**:
- Test that moved units become obstacles for subsequent units
- Test MovementLimit enforcement
- Test that failed movements don't count against limit
- Test priority recomputation on blockage
- Verify claimed targets tracking prevents convergence

**End-to-End Movement Phase**:
- Setup: AI participant, enemy units, GameMaster mocks
- Execute: Complete movement phase
- Assert: Exactly MovementLimit units moved
- Assert: Units moved in correct priority order
- Assert: No validation errors
- Assert: Fleet maintains formation

**Fog-of-War Compliance**:
- Test AI reasons only over visible enemies
- Test EnemyMemory updates when contacts appear/disappear
- Test predictions used for danger calculation
- Test predictions expire after 5 turns
- Verify no direct access to enemy unit positions

### Property-Based Testing

This feature is **NOT suitable for property-based testing** because:

1. **External Dependencies**: The AI system integrates with GameMaster APIs (movement validation, fog-of-war queries, deployment commits) which are stateful external services, not pure functions
2. **Side Effects**: Movement commits modify GameMaster state (unit positions, move counters, fog state)
3. **Complex State Machines**: FleetStance transitions depend on accumulated turn history, hysteresis, and dwell counters
4. **Non-Deterministic Elements**: Priority scoring depends on dynamic game state (enemy positions, fog visibility, map layout) that varies between test runs

**Testing Approach**: Use unit tests for pure algorithms (scoring functions, calculations) and integration tests with mocked GameMaster APIs for workflow validation.

### Manual Testing Scenarios

**Scenario 1: Movement Limit Compliance**:
- Setup: Fleet with 7 units (MovementLimit = 4)
- Observe: AI attempts exactly 4 movements
- Observe: AI logs "Movement limit reached"
- Verify: 4 units have new positions, 3 remain in place

**Scenario 2: Priority System**:
- Setup: Mixed fleet (2 Scouts, 2 Anchors, 3 Strikers)
- Observe: Scouts move first, Anchors last
- Setup: Retreat stance with 2 damaged units
- Observe: Damaged units move first

**Scenario 3: Formation Cohesion**:
- Setup: Scattered fleet, DefensiveFallback stance
- Observe: Units move toward fleet centroid
- Measure: Average distance to centroid decreases each turn

**Scenario 4: Target Claiming**:
- Setup: 3 units, 2 enemies in range
- Observe: Units spread across both enemies
- Observe: Units don't all converge on closest enemy

**Scenario 5: Fog-of-War**:
- Setup: Enemy units hidden by fog
- Observe: AI doesn't path directly to hidden enemies
- Observe: EnemyMemory predictions used for danger
- Observe: Hunt stance activates when contacts lost

**Scenario 6: Graceful Degradation**:
- Setup: High-priority unit surrounded by obstacles
- Observe: AI marks unit as blocked
- Observe: AI moves lower-priority unblocked units instead
- Verify: Movement slots not wasted

## Configuration

### Tunable Constants

All weight and threshold constants are defined at class scope for easy tuning:

**FleetCommander Stance Thresholds**:
```csharp
private const float RetreatScoreBelow = 0.35f;      // Material below this triggers Retreat
private const float RetreatHpBelow = 0.30f;         // HP below this triggers Retreat
private const float FallbackScoreBelow = 0.60f;     // Material below this triggers Fallback
private const float AdvanceScoreAtLeast = 0.90f;    // Material above this triggers Advance
private const float HysteresisMargin = 0.08f;       // Current stance threshold widening
private const int MinDwellTurns = 2;                // Minimum turns before stance change
```

**MovementPrioritizer Weights**:
```csharp
private const float RoleWeightScout = 50f;
private const float RoleWeightStriker = 30f;
private const float RoleWeightAnchor = 20f;
private const float ThreatProximityWeight = 40f;
private const float AttackOpportunityWeight = 35f;
private const float DamagedPenalty = -20f;
private const float BlockedPenalty = -100f;
```

**MovementPlanner Tile Scoring Weights**:
```csharp
private const float KillBonus = 50f;
private const float MaxDamageScore = 40f;
private const float TargetWeaknessWeight = 10f;
private const float DangerWeight = 35f;
private const float SelfRiskWeight = 15f;
private const float AlreadyClaimedPenalty = 20f;
private const float CohesionBaseWeight = 8f;
private const float FragilityPushbackWeight = 12f;
private const float ScoutForwardWeight = 6f;
private const float FragilityThreshold = 0.6f;
private const float RetreatDirectionalWeight = 18f;
private const float FallbackDirectionalWeight = 10f;
private const float AdvanceDirectionalWeight = 10f;
```

**DeploymentPlanner Spacing**:
```csharp
private const int MinimumSpacing = 2;               // Initial spacing between units
private const int SpacingRelaxationStep = 1;        // Decrement per retry
```

**EnemyMemory Expiration**:
```csharp
private const int PredictionExpirationTurns = 5;    // Predictions older than this are invalid
```

**MovementPlanner Flee Threshold**:
```csharp
private const float FleeHealthFraction = 0.3f;      // HP below this triggers flee behavior
```

### Tuning Guidelines

**Increasing Aggression**:
- Increase `AdvanceScoreAtLeast` (e.g., 0.85) to enter Advance earlier
- Increase `AttackOpportunityWeight` to prioritize offense over safety
- Decrease `DangerWeight` to tolerate more risk
- Increase `AdvanceDirectionalWeight` to push forward faster

**Increasing Caution**:
- Increase `RetreatScoreBelow` (e.g., 0.40) to retreat sooner
- Increase `DangerWeight` to avoid risky positions
- Increase `CohesionBaseWeight` to keep fleet tighter
- Decrease `AttackOpportunityWeight` to prioritize safety

**Favoring Scouts**:
- Increase `RoleWeightScout` to move scouts first more consistently
- Increase `ScoutForwardWeight` to push scouts forward
- Decrease `FragilityPushbackWeight` to let scouts approach enemies

**Tighter Formations**:
- Increase `CohesionBaseWeight` for all stances
- Increase `MinimumSpacing` in deployment
- Increase cohesion multiplier for DefensiveFallback/Retreat

**Faster Exploration**:
- Decrease `MinDwellTurns` to allow quicker Hunt->Advance transition
- Increase exploration bonus weight (future feature)
- Decrease `FragilityPushbackWeight` to let scouts roam farther

## Implementation Notes

### Directory Structure

```
Assets/Scripts/Game/AI/
├── AIController.cs              // MonoBehaviour orchestrator
├── FleetCommander.cs            // Strategic stance manager
├── MovementPrioritizer.cs       // Priority scoring system
├── MovementPlanner.cs           // Tile scoring and destination selection
├── DeploymentPlanner.cs         // Zone analysis and placement
├── AITurnContext.cs             // Per-turn snapshot builder
├── FleetSnapshot.cs             // Immutable fleet state
├── EnemyMemory.cs               // Position tracking/prediction
└── README.md                    // Documentation
```

### Initialization Sequence

```csharp
// AIController.Awake()
void Awake()
{
    gameMaster = FindObjectOfType<GameMaster>();
    if (gameMaster == null)
    {
        Debug.LogError("[AI] GameMaster not found in scene");
        enabled = false;
        return;
    }
    
    if (assignedParticipant == null)
    {
        Debug.LogError("[AI] No participant assigned to AIController");
        enabled = false;
        return;
    }
}

// AIController.OnEnable()
void OnEnable()
{
    if (gameMaster != null)
    {
        gameMaster.PhaseChanged += OnPhaseChanged;
        Debug.Log($"[AI] Subscribed to PhaseChanged for participant {assignedParticipant.ID}");
    }
}

// Lazy initialization on first Move phase
void HandleMovementPhase()
{
    if (fleetCommander == null)
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        fleetCommander = new FleetCommander(gridManager, assignedParticipant.PlayerId);
        Debug.Log("[AI] FleetCommander initialized");
    }
    
    EnemyMemory.ClearForParticipant(assignedParticipant);  // Clear stale match state
    
    ProcessMovementPhase();
}
```

### Cleanup Sequence

```csharp
void OnDisable()
{
    if (gameMaster != null)
    {
        gameMaster.PhaseChanged -= OnPhaseChanged;
        Debug.Log("[AI] Unsubscribed from PhaseChanged");
    }
    
    // Clear turn state
    claimedTargets?.Clear();
    movedUnitIDs?.Clear();
    movesCommitted = 0;
}

void OnDestroy()
{
    // FleetCommander is a plain class, GC will collect
    fleetCommander = null;
    
    // Clear participant-specific memory
    if (assignedParticipant != null)
    {
        EnemyMemory.ClearForParticipant(assignedParticipant);
    }
}
```

### Phase State Management

Each phase start clears transient state:

```csharp
void OnPhaseChanged(GameplayPhase phase, RuntimeParticipant activeParticipant)
{
    if (activeParticipant != assignedParticipant)
    {
        return;
    }
    
    // Clear turn-scoped state at phase start
    claimedTargets.Clear();
    movedUnitIDs.Clear();
    movesCommitted = 0;
    
    switch (phase)
    {
        case GameplayPhase.Deployment:
            HandleDeploymentPhase();
            break;
        case GameplayPhase.Move:
            HandleMovementPhase();
            break;
        case GameplayPhase.Search:
            HandleSearchPhase();
            break;
        case GameplayPhase.Battle:
            HandleBattlePhase();
            break;
    }
}
```

### Logging Strategy

Comprehensive logging for debugging and telemetry:

```csharp
// Stance transitions
Debug.Log($"[AI][Fleet] stance={CurrentStance} (changed from {previous}) score={MaterialScore:F2} " +
          $"ownAlive={OwnAlive}/{OwnStartCount} enemyEst={EnemyEstimate} fleetHp={FleetHpFraction:F2}");

// Movement decisions
Debug.Log($"[AI][Move] Unit {unit.RuntimeID} priority={priority:F1} " +
          $"role={role} hp={currentHealth}/{maxHealth}");
Debug.Log($"[AI][Move] Destination={destination} target={targetID} score={score:F1} " +
          $"isFleeing={isFleeing}");

// Validation/commit outcomes
Debug.LogWarning($"[AI] Movement validation failed for {unitID}: {validationMessage}");
Debug.Log($"[AI] Successfully moved {unitID} to {finalPosition}");

// Movement limit tracking
Debug.Log($"[AI] Movement phase complete: {movesCommitted}/{movementLimit} moves used");
```

### Performance Considerations

**Candidate Tile Generation**:
- Limit candidate tiles to reachable range (movement budget)
- Filter out obviously invalid tiles (off-map, impassable terrain) early
- Typical candidate count: 10-30 tiles per unit

**Danger Calculation Optimization**:
- Calculate danger once per tile, reuse across all enemies
- Cache weapon ranges to avoid repeated definition lookups
- Early exit if danger exceeds flee threshold

**Priority Recomputation**:
- Only recompute when high-priority unit blocked
- Blocked units marked and moved to end of list
- Avoid sorting entire fleet repeatedly

**Memory Allocation**:
- Reuse collections across turns where possible
- Clear collections rather than allocating new ones
- FleetSnapshot generated once per turn, shared across subsystems

**Expected Performance**:
- Full movement phase for 10-unit fleet: < 100ms
- Typical case (4-5 moves): < 50ms
- No frame drops during AI processing

### Future Extensions

**Planned Features** (not in initial implementation):
1. **Active Scan Planning**: Search phase logic for revealing hidden enemies
2. **Flank Stance**: Coordinate multi-prong attacks (currently never selected)
3. **Formation Templates**: Pre-configured formations (line, wedge, screen)
4. **Advanced Predictions**: Use known enemy unit types to refine EnemyMemory predictions
5. **Difficulty Levels**: Tuning presets for Easy/Medium/Hard/Expert
6. **Behavioral Personalities**: Aggressive/Defensive/Balanced commander types
7. **Learning System**: Track which decisions led to victories/defeats

**Extension Points**:
- New stance types: Add enum value, update ChooseStance logic, add stance modifiers
- New unit roles: Update ClassifyRole criteria, add role-specific scoring terms
- New tile scoring terms: Add component function, integrate into ScoreTile
- Alternative prioritizers: Implement IPrioritizer interface, swap in AIController

## Appendix: Key Algorithms Summary

### Priority Scoring (MovementPrioritizer)

```
Priority = RoleWeight + ThreatProximity + AttackOpportunity + HealthFactor + StanceModifier

RoleWeight: Scout=50, Striker=30, Anchor=20
ThreatProximity: (1 - minDistToEnemy/mapDiag) * 40
AttackOpportunity: Sum(enemiesInRange * expectedDamage/maxHP) * 35
HealthFactor: HP < 40% ? -20 : 0

StanceModifier:
  Retreat + damaged: +60
  Advance + Scout: +30
  Advance + closeEnemy: +20
  Fallback + edgeUnit: +25
```

### Tile Scoring (MovementPlanner)

```
TileScore = AttackOpportunity - Danger + Cohesion + RoleBias + DirectionalBias

AttackOpportunity:
  For each enemy in weapon range:
    expectedDamage = weapon.ExpectedValue()
    if canKill: +50
    else: +(expectedDamage/maxHP * 40)
    if alreadyClaimed: -20

Danger:
  For each enemy weapon threatening tile:
    danger += (expectedIncoming/maxHP * 35)
  if Retreat/Fallback: danger *= 1.5

Cohesion:
  base = -dist(tile, fleetCentroid) * (8 / mapDiag)
  if Retreat/Fallback: base *= 2.0
  if Advance/Hunt: base *= 0.5
  if Scout + Hunt: base *= -1.0
  if damaged: base *= 1.5

RoleBias:
  if Scout + (Hunt|Advance):
    +(1 - dist/mapDiag) * 6
  if highFragility:
    -(1 - dist/mapDiag) * 12

DirectionalBias:
  Retreat: +dot(toHome, direction) * 18
  Fallback: +dot(toHome, direction) * 10
  Advance: +dot(toEnemy, direction) * 10
```

### Stance Selection (FleetCommander)

```
MaterialScore = OwnAlive / (OwnStartCount + EnemyEstimate)
FleetHpFraction = Sum(currentHP) / Sum(maxHP)
hysteresis = (current == candidate) ? 0.08 : 0

if MaterialScore < 0.35 + hysteresis OR FleetHp < 0.30 + hysteresis:
    return Retreat  (immediate, ignores dwell)

if MaterialScore < 0.60 + hysteresis:
    if turnsInStance >= 2 OR current == Retreat:
        return DefensiveFallback
    else:
        return current  (dwell lock)

if contacts.Count == 0:
    return Hunt  (immediate, ignores dwell)

if MaterialScore >= 0.90 - hysteresis:
    if turnsInStance >= 2 OR leaving Hunt:
        return Advance
    else:
        return current  (dwell lock)

return Hold
```

### Enemy Prediction (EnemyMemory)

```
For each hidden enemy with memory entry:
    turnsElapsed = currentTurn - lastSeenTurn
    
    if turnsElapsed > 5:
        expire entry
        continue
    
    assumedSpeed = 3  // conservative
    predictedDistance = min(turnsElapsed * assumedSpeed, 10)  // cap
    
    direction = FacingToVector(lastKnownFacing)
    predictedPosition = lastKnownPosition + (direction * predictedDistance)
    
Use predictedPosition in danger calculations and tile scoring
```
