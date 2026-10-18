# AI Data Models Implementation Notes

## Task Completion Summary

Successfully implemented foundational data models for the Admirals AI system as specified in task "Set up project structure and data models".

## Files Created

### Enumerations
1. **UnitRole.cs** - Tactical role classification
   - Scout: Fast reconnaissance (maxSpeed >= 6 AND maxWeaponRange >= 4)
   - Anchor: Durable defensive (maxHealth >= 80 AND weaponCount >= 2)
   - Striker: Balanced tactical (default classification)

2. **FleetStance.cs** - Strategic posture enumeration
   - Hunt: Exploration when no contacts visible
   - Advance: Aggressive when MaterialScore >= 0.90
   - Hold: Balanced when 0.60 <= MaterialScore < 0.90
   - DefensiveFallback: Defensive withdrawal when MaterialScore < 0.60
   - Retreat: Emergency disengagement when MaterialScore < 0.35 OR FleetHpFraction < 0.30

### Data Classes
3. **FleetMember.cs** - Per-unit metadata snapshot
   - Properties: Unit, Role, HpFraction, EffectiveFragility, Position, Facing
   - EffectiveFragility calculation: Scouts 1.3x multiplier, Anchors 0.7x multiplier

4. **FleetSnapshot.cs** - Immutable per-turn fleet state
   - Strategic metrics: MaterialScore, FleetHpFraction, MapProgress
   - Positional data: FleetCentroid, HomeCentroid, NearestContactDistance
   - Collections: Members (alive units with metadata), Contacts (visible enemies)
   - Stance tracking: CurrentStance, TurnsInStance

5. **AITurnContext.cs** - Per-turn game state snapshot
   - Factory method: `Build(GameMaster, RuntimeParticipant)` queries GameMaster APIs
   - Participant-filtered data: OwnUnits, OwnAliveUnits, EnemyVisibleUnits
   - Movement phase state: MovementLimit, MovesCommitted, CanAct
   - References: GameMap, GridManager for spatial calculations

### Supporting Files
6. **README.md** - Directory structure and architecture overview
7. **AIDataModelsTest.cs** - Simple validation test for data models
8. **IMPLEMENTATION_NOTES.md** - This file

## Requirements Satisfied

✅ **Requirement 14.1** - Phase orchestration and turn context
- AITurnContext.Build() queries GameMaster.CurrentPhase and TurnNumber
- Context includes all phase-specific state needed for decision-making

✅ **Requirement 14.5** - Sequential movement processing
- AITurnContext tracks MovesCommitted to support sequential processing
- MovementLimit enforcement through context state

✅ **Requirement 16.1-16.7** - RuntimeUnit to snapshot data mapping
- FleetMember extracts: Position (16.1), Facing (16.2), Health (16.3)
- FleetMember extracts: Movement capability (16.4), RuntimeID (16.5), Definition (16.6)
- AITurnContext filters destroyed units (16.7) via LifeState check

✅ **Requirement 26.1-26.8** - AI system self-containment
- All files in Assets/Scripts/Game/AI/ directory (26.1-26.5)
- No modifications to existing game code (26.7)
- Participant-filtered GameMaster API access (26.6)
- Clean separation of concerns (26.8)

## Design Decisions

### Pattern Consistency
- Followed existing Fleet/ directory patterns for data structure design
- Used XML documentation comments throughout for API clarity
- Maintained consistency with GameMaster naming conventions (RuntimeUnit, RuntimeParticipant, etc.)

### Fog-of-War Compliance
- AITurnContext.EnemyVisibleUnits filtered via GameKnowledge.IsUnitVisibleToParticipant
- FleetSnapshot.Contacts contains only visible enemies, never direct enemy roster access
- No backdoor access to hidden enemy state

### Immutability and Snapshot Pattern
- FleetSnapshot designed as immutable after generation (per design spec)
- AITurnContext built once per turn to avoid inconsistent state
- All collections initialized in constructors to prevent null reference exceptions

### Factory Method Pattern
- AITurnContext.Build() encapsulates complex GameMaster query logic
- Single entry point for context construction ensures consistency
- Clear separation between data structure (AITurnContext) and construction logic (Build method)

## Integration Points

### GameMaster API Dependencies
- `GameMaster.CurrentPhase` - Current gameplay phase
- `GameMaster.TurnNumber` - Current turn number (1-indexed)
- `GameMaster.GameUnits.TryGetRoster()` - Query participant unit rosters
- `GameMaster.GameKnowledge.IsUnitVisibleToParticipant()` - Fog-of-war visibility check
- `GameMaster.GameMovement.TryGetView()` - Movement phase state query
- `GameMaster.GameMap` - Map reference for spatial calculations

### Type Dependencies
- `RuntimeParticipant` (Assets/Scripts/Game/GameMaster/RuntimeParticipant.cs)
- `RuntimeUnit` (Assets/Scripts/Game/GameMaster/GameUnits.cs)
- `GameplayPhase` (Assets/Scripts/Game/Definitions/GameplayTypes.cs)
- `UnitFacing` (Assets/Scripts/Game/GameMaster/GameUnits.cs)
- `RuntimeUnitLifeState` (Assets/Scripts/Game/GameMaster/GameUnits.cs)

## Next Steps

The following components should be implemented next (in priority order):

1. **FleetCommander.cs** - Strategic stance manager
   - Implements stance selection logic using FleetSnapshot metrics
   - Generates FleetSnapshot from AITurnContext
   - Manages stance dwell and hysteresis

2. **AIController.cs** - MonoBehaviour orchestrator
   - Subscribes to GameMaster.PhaseChanged events
   - Builds AITurnContext via Build() factory method
   - Coordinates subsystem calls (FleetCommander, MovementPrioritizer, MovementPlanner)

3. **MovementPrioritizer.cs** - Priority scoring for unit movement order
4. **MovementPlanner.cs** - Tile scoring and destination selection
5. **DeploymentPlanner.cs** - Initial fleet placement
6. **EnemyMemory.cs** - Position tracking for fog-of-war

## Testing Strategy

### Unit Tests (To Be Implemented)
- FleetMember.EffectiveFragility calculation verification
- FleetSnapshot metrics calculation (MaterialScore, FleetHpFraction, MapProgress)
- AITurnContext.Build() participant filtering logic
- Enumeration value validation

### Integration Tests (To Be Implemented)
- AITurnContext.Build() with mock GameMaster
- Fog-of-war visibility filtering accuracy
- Movement phase state query integration
- GridManager scene lookup handling

## Notes for Future Implementation

### GridManager Dependency
The current implementation uses `Object.FindObjectOfType<GridManager>()` for scene lookup. This is acceptable for initial implementation but should be refactored to use dependency injection when AIController is implemented.

### Error Handling
AITurnContext.Build() includes null checks and error logging but does not throw exceptions for missing optional components (GridManager). This graceful degradation allows partial functionality when scene setup is incomplete.

### Performance Considerations
- Collections are pre-allocated in constructors to avoid allocation overhead
- Factory method performs all queries in one pass to minimize API calls
- FleetSnapshot designed for single generation per turn, not incremental updates
