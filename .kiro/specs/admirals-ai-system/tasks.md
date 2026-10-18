# Implementation Plan: Admirals AI System

## Overview

This implementation plan breaks down the Admirals AI system into incremental tasks that build upon each other. The system will be self-contained in `Assets/Scripts/Game/AI/` and will integrate with GameMaster's participant-filtered APIs while respecting fog-of-war rules and movement limitations.

The implementation follows a bottom-up approach: data structures first, then supporting utilities, tactical subsystems, strategic layer, and finally the orchestrator that wires everything together.

## Tasks

- [x] 1. Set up project structure and data models
  - Create `Assets/Scripts/Game/AI/` directory structure
  - Define enumerations: `UnitRole`, `FleetStance`
  - Create `FleetMember` class with unit metadata
  - Create `FleetSnapshot` class with immutable fleet state
  - Create `AITurnContext` class with factory method to query GameMaster APIs
  - Add XML documentation comments for all public APIs
  - _Requirements: 14.1, 14.5, 16.1-16.7, 26.1-26.8_

- [x] 2. Implement EnemyMemory tracking system
  - [x] 2.1 Create EnemyMemory static class with participant-keyed storage
    - Implement `MemoryEntry` internal class with position, facing, last seen turn
    - Implement `UpdateContacts` method to record visible enemy positions
    - Implement prediction expiration logic (5 turn threshold)
    - Implement `GetPredictedPositions` method with linear prediction algorithm
    - Implement `ClearForParticipant` and `ClearAll` cleanup methods
    - _Requirements: 4.4, 4.7, 4.8, 26.6, 27.3_
  
  - [ ]* 2.2 Write unit tests for EnemyMemory
    - Test memory entry creation and updates
    - Test prediction calculation accuracy
    - Test expiration logic after 5 turns
    - Test participant-keyed isolation
    - _Requirements: 4.4, 4.7, 4.8_

- [x] 3. Implement FleetCommander strategic layer
  - [x] 3.1 Create FleetCommander class with stance selection logic
    - Implement constructor with GridManager and PlayerId
    - Implement `BuildSnapshot` method to generate FleetSnapshot from AITurnContext
    - Implement role classification logic (Scout: speed ≥ 6 and range ≥ 4, Anchor: HP ≥ 80 and weapons ≥ 2)
    - Implement effective fragility calculation with role multipliers
    - Implement fleet centroid and home centroid calculations
    - _Requirements: 5.1, 5.2, 6.1-6.6, 7.1-7.10_
  
  - [x] 3.2 Implement stance selection with hysteresis and dwell enforcement
    - Implement MaterialScore calculation: OwnAlive / (OwnStartCount + EnemyEstimate)
    - Implement FleetHpFraction calculation: sum(currentHP) / sum(maxHP)
    - Implement stance thresholds: Retreat < 0.35, Fallback < 0.60, Advance ≥ 0.90
    - Implement hysteresis margin (±0.08 for current stance)
    - Implement stance dwell enforcement (minimum 2 turns, except Retreat and Hunt exit)
    - Implement `ChooseStance` method with all transition rules
    - _Requirements: 5.3-5.10, 18.1-18.5_
  
  - [x] 3.3 Add enemy alive estimation and kill tracking
    - Initialize enemy alive estimate to starting fleet size
    - Implement `NoteKillConfirmed` method to decrement estimate
    - Prevent estimate from going below zero
    - Use estimate in MaterialScore denominator
    - _Requirements: 17.1-17.5_
  
  - [x] 3.4 Implement target prioritization for Battle phase
    - Create `PrioritizeTargets` method that sorts enemies by threat
    - Calculate threat based on weapon ranges and damage potential
    - Return sorted list of ShipInstance targets
    - _Requirements: 5.1, 7.3_
  
  - [ ]* 3.5 Write unit tests for FleetCommander
    - Test role classification for various unit configurations
    - Test MaterialScore and FleetHpFraction calculations
    - Test stance selection thresholds
    - Test hysteresis prevents rapid flipping
    - Test stance dwell enforcement
    - Test immediate Retreat on critical damage
    - Test Hunt selection when no contacts visible
    - _Requirements: 5.1-5.10, 6.1-6.6_

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement MovementPrioritizer tactical system
  - [x] 5.1 Create MovementPrioritizer static class with priority scoring
    - Implement `PrioritizeUnits` method that returns sorted RuntimeUnit list
    - Implement role weight calculation (Scout: 50, Striker: 30, Anchor: 20)
    - Implement threat proximity scoring (1 - distance/diagonal) * 40
    - Implement attack opportunity scoring based on enemies in weapon range
    - Implement health factor penalty for damaged units (-20 when HP < 40%)
    - _Requirements: 2.1-2.8, 15.1, 30.5_
  
  - [x] 5.2 Add stance-specific priority modifiers
    - Implement Retreat modifier: +60 for damaged units (HP < 40%)
    - Implement Advance modifier: +30 for Scouts, +20 for units near enemies
    - Implement DefensiveFallback modifier: +25 for edge units
    - Implement Hold modifier: 0 (default priority only)
    - _Requirements: 2.5, 2.6, 2.8_
  
  - [x] 5.3 Add blockage detection and priority recomputation
    - Check each high-priority unit has at least one reachable tile
    - Mark units with no valid destinations as blocked (priority penalty -100)
    - Move blocked units to end of priority list
    - _Requirements: 2.4, 23.1-23.5_
  
  - [ ]* 5.4 Write unit tests for MovementPrioritizer
    - Test priority calculation for each UnitRole
    - Test stance modifiers (Retreat prioritizes damaged, Advance prioritizes scouts)
    - Test threat proximity calculation
    - Test attack opportunity scoring
    - Test blockage detection
    - Test priority ordering stability
    - _Requirements: 2.1-2.8, 15.1_

- [ ] 6. Implement MovementPlanner tile scoring system
  - [x] 6.1 Create MovementPlanner static class with tile scoring
    - Create `Decision` struct with destination, likelyTarget, and isFleeing fields
    - Implement `ChooseDestination` main method
    - Implement `GetPlaceableReachableTiles` to generate candidate tiles within movement budget
    - Implement `ScoreTile` method with weighted sum of factors
    - _Requirements: 8.1-8.10, 13.1-13.6, 30.1-30.5_
  
  - [x] 6.2 Implement attack opportunity scoring
    - Calculate distance to each visible enemy from candidate tile
    - Check if enemies are within weapon range
    - Add kill bonus (+50) if expected damage >= enemy currentHealth
    - Add damage score (expectedDamage / maxHP * 40) otherwise
    - Add target weakness weight (+10) for low-HP enemies
    - Reduce score (-20) if target is already claimed
    - Scale by FleetStance: Advance * 1.5, Retreat * 0.3
    - _Requirements: 22.1-22.7_
  
  - [x] 6.3 Implement danger assessment
    - For each visible enemy unit, enumerate weapon slots
    - Check if candidate tile is within enemy weapon range
    - Add danger penalty scaled by expected incoming damage
    - Aggregate danger from multiple threatening enemies
    - Multiply danger weight by 1.5 when stance is Retreat or DefensiveFallback
    - Include predicted enemy positions from EnemyMemory in danger calculations
    - _Requirements: 21.1-21.7_
  
  - [x] 6.4 Implement formation cohesion scoring
    - Calculate base cohesion as -distance(tile, fleetCentroid) * (8 / mapDiagonal)
    - Multiply cohesion weight by 2.0 when stance is DefensiveFallback or Retreat
    - Multiply cohesion weight by 0.5 when stance is Advance or Hunt
    - Apply negative cohesion weight for Scout units in Hunt stance (push forward)
    - Increase cohesion weight by 1.5 for damaged units (HP < 40%)
    - _Requirements: 9.1-9.6_
  
  - [x] 6.5 Implement role bias and directional bias
    - Add scout forward weight: +(1 - dist/mapDiag) * 6 for Scouts in Hunt/Advance
    - Add fragility pushback: -(1 - dist/mapDiag) * 12 for high-fragility units
    - Add Retreat directional bias: +dot(toHome, direction) * 18
    - Add Fallback directional bias: +dot(toHome, direction) * 10
    - Add Advance directional bias: +dot(toEnemy, direction) * 10
    - _Requirements: 8.6, 8.7, 28.1-28.5, 29.1-29.5_
  
  - [x] 6.6 Implement flee logic override for critically damaged units
    - Check if unit HP / maxHP <= 0.3 (critically low)
    - Check if best tile offers lethal attack (can kill target)
    - If critically low AND no lethal attack available, find safest tile
    - Implement `FindSafestTile` method (minimum danger regardless of attack)
    - Return Decision with isFleeing = true
    - _Requirements: 28.1, 28.4_
  
  - [ ]* 6.7 Write unit tests for MovementPlanner
    - Test attack opportunity calculation with various weapon ranges
    - Test danger calculation with multiple threatening enemies
    - Test cohesion scoring with various fleet centroids
    - Test role bias (scout forward bonus, fragility pushback)
    - Test directional bias for each stance
    - Test flee logic override for critically damaged units
    - Test tile scoring is comparable across different unit health scales
    - _Requirements: 8.1-8.10, 21.1-21.7, 22.1-22.7, 28.1-28.5_

- [ ] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Implement DeploymentPlanner initial placement system
  - [x] 8.1 Create DeploymentPlanner static class with zone analysis
    - Implement `ExecuteDeployment` main method
    - Implement `CalculateDeploymentCentroid` from zone tile list
    - Implement `EstimateEnemyDirection` by finding opposite map edge
    - Implement `CalculateFacingToward` to convert direction vector to UnitFacing
    - _Requirements: 11.1-11.6_
  
  - [x] 8.2 Implement sector division and unit assignment
    - Implement `DivideIntoSectors` to split zone into Left/Center/Right
    - Use perpendicular axis to enemy direction for sector classification
    - Implement `AssignUnitsToSectors`: Anchors to Center, Scouts to Left/Right
    - Assign Strikers to least populated sector for balance
    - _Requirements: 12.1-12.3_
  
  - [x] 8.3 Implement placement with spacing enforcement
    - Implement `TryPlaceUnit` with minimum spacing (2 tiles initially)
    - For each tile in assigned sector, check distance to occupied positions
    - Validate placement with GameDeployment.ValidatePlacement
    - Commit with GameDeployment.TrySubmitPlacement
    - Relax spacing by 1 tile per retry if no valid placement found
    - Log placement failures for diagnostics
    - _Requirements: 12.4-12.10, 19.5_
  
  - [ ]* 8.4 Write unit tests for DeploymentPlanner
    - Test sector division for various deployment zone shapes
    - Test role-based sector assignment (Anchors center, Scouts edges)
    - Test spacing enforcement and relaxation
    - Test facing calculation toward enemy
    - Mock GameDeployment validation/commit workflow
    - _Requirements: 11.1-11.6, 12.1-12.10_

- [ ] 9. Implement AIController orchestration layer
  - [x] 9.1 Create AIController MonoBehaviour with lifecycle management
    - Add `[SerializeField] RuntimeParticipant assignedParticipant` field
    - Implement `Awake` to find GameMaster in scene
    - Implement `OnEnable` to subscribe to GameMaster.PhaseChanged event
    - Implement `OnDisable` to unsubscribe and clear turn state
    - Validate assignedParticipant is not null during initialization
    - _Requirements: 14.1, 14.5, 14.6, 26.1, 27.5_
  
  - [x] 9.2 Implement phase transition handlers
    - Implement `OnPhaseChanged` dispatcher method
    - Validate activeParticipant matches assignedParticipant before processing
    - Implement `HandleDeploymentPhase` to invoke DeploymentPlanner
    - Implement `HandleMovementPhase` to invoke FleetCommander and movement processing
    - Implement `HandleSearchPhase` stub (future: active scan planning)
    - Implement `HandleBattlePhase` stub (future: target selection)
    - Clear turn-scoped state (claimedTargets, movedUnitIDs, movesCommitted) at phase start
    - _Requirements: 14.2-14.4, 14.7, 27.4_
  
  - [x] 9.3 Implement movement limit enforcement and tracking
    - Query GameMovement.ParticipantView to read MovementLimit
    - Track MovesCommitted counter
    - Track movedUnitIDs set to prevent duplicate attempts
    - Stop processing when MovesCommitted == MovementLimit
    - Only increment counter on successful commits
    - _Requirements: 1.1-1.5_
  
  - [x] 9.4 Implement sequential movement processing
    - Implement `ProcessMovementPhase` main loop
    - Lazy-initialize FleetCommander on first Move phase
    - Build AITurnContext from GameMaster APIs
    - Generate FleetSnapshot from FleetCommander.Evaluate
    - Get prioritized unit list from MovementPrioritizer
    - Process units one at a time in priority order
    - Wait for each movement to commit before processing next unit
    - _Requirements: 15.1-15.5_
  
  - [x] 9.5 Implement unit movement workflow with validation and commit
    - Implement `TryMoveUnit` method for single unit processing
    - Call MovementPlanner.ChooseDestination to get best tile and target
    - Call GameMovement.ValidateMovement to check pathfinding and budget
    - Handle validation failures by trying alternative destinations
    - Call GameMovement.TryCommitMovement to execute movement atomically
    - Handle commit failures gracefully (log and continue to next unit)
    - Update claimedTargets set when unit moves toward specific enemy
    - Skip unit if no valid destinations exist
    - _Requirements: 3.4-3.7, 13.1-13.6, 20.1-20.5_
  
  - [x] 9.6 Add target claiming coordination
    - Maintain HashSet<ShipInstance> claimedTargets for Move phase
    - When MovementPlanner returns likelyTarget, add to claimedTargets
    - Pass claimedTargets to MovementPlanner for tile scoring adjustments
    - Clear claimedTargets at end of Move phase
    - _Requirements: 10.1-10.6_
  
  - [x] 9.7 Implement BuildTurnContext factory method
    - Query GameMaster.GameUnits.GetAllUnits() for unit list
    - Filter own units by RuntimeParticipant match
    - Filter alive units by LifeState == Active
    - Query GameKnowledge.IsUnitVisibleToParticipant for enemy visibility
    - Build visible enemy list (contacts)
    - Query GameMovement.GetParticipantView for movement phase state
    - Query GameMap and GridManager references
    - Return populated AITurnContext instance
    - _Requirements: 3.1-3.3, 4.1-4.3, 16.1-16.7_
  
  - [ ]* 9.8 Write integration tests for AIController
    - Test phase event subscription and handling
    - Test participant identity validation
    - Test movement validation/commit workflow
    - Test deployment validation/commit workflow
    - Test fog-of-war queries
    - Test MovementLimit enforcement (fleet with 7 units = limit 4)
    - Test priority ordering (Scouts move first, Anchors last)
    - Test claimed targets prevents convergence
    - _Requirements: 1.1-1.5, 3.1-3.8, 14.1-14.7, 15.1-15.5_

- [x] 10. Add comprehensive logging and diagnostics
  - [x] 10.1 Add stance transition logging to FleetCommander
    - Log stance changes with previous and new stance
    - Log MaterialScore, FleetHpFraction, and trigger conditions
    - Log stance dwell blocks with desired stance and remaining turns
    - Log alive own units, enemy estimate, and contact count
    - _Requirements: 14.7, 18.1-18.5_
  
  - [x] 10.2 Add movement decision logging to AIController
    - Log unit priority scores with role and health
    - Log destination selection with target and tile score
    - Log validation failures with error messages
    - Log commit successes with final positions
    - Log movement phase completion with moves used / limit
    - _Requirements: 14.7, 20.1, 20.4_
  
  - [x] 10.3 Add deployment logging to DeploymentPlanner
    - Log sector assignments for each unit
    - Log placement attempts with spacing information
    - Log validation and commit outcomes
    - Log placement failures with diagnostic context
    - _Requirements: 12.10, 19.4_

- [x] 11. Implement graceful degradation and error handling
  - [x] 11.1 Add low visibility handling to MovementPlanner
    - When contacts list is empty, use Hunt stance exploration bias
    - Use EnemyMemory predictions as secondary targets
    - Bias movement toward map center when no contacts or predictions
    - Never error or halt on zero visible enemies
    - _Requirements: 25.1-25.5_
  
  - [x] 11.2 Add validation failure recovery to AIController
    - Try alternative destinations in descending score order
    - Mark rejected destinations as invalid for current turn
    - Skip unit and continue processing if no valid destinations exist
    - Never halt entire AI turn due to single unit failure
    - _Requirements: 20.2, 20.3, 20.5_
  
  - [x] 11.3 Add exception handling to phase handlers
    - Wrap phase processing in try-catch blocks
    - Log exceptions with stack traces
    - Gracefully degrade (AI does nothing) rather than crashing game
    - _Requirements: 3.8, 14.7_

- [x] 12. Final integration and wiring
  - [x] 12.1 Create AIController prefab or scene setup
    - Add AIController component to appropriate GameObject
    - Assign RuntimeParticipant reference in Inspector
    - Verify GameMaster is findable in scene
    - Test initialization in play mode
    - _Requirements: 26.1, 27.1_
  
  - [x] 12.2 Verify complete GameMaster API integration
    - Test all GameMovement API calls (ValidateMovement, TryCommitMovement, GetParticipantView)
    - Test all GameDeployment API calls (ValidatePlacement, TrySubmitPlacement, GetParticipantView)
    - Test all GameKnowledge API calls (IsUnitVisibleToParticipant)
    - Test all GameUnits queries (GetAllUnits, unit filtering)
    - Verify participant filtering works correctly
    - _Requirements: 3.1-3.8_
  
  - [x] 12.3 Verify fog-of-war compliance
    - Test AI reasons only over visible enemies
    - Test EnemyMemory updates when contacts appear/disappear
    - Test predictions used for danger calculation
    - Test predictions expire after 5 turns
    - Verify no direct access to enemy positions without visibility checks
    - _Requirements: 4.1-4.8_
  
  - [ ]* 12.4 Write end-to-end integration tests
    - Test complete movement phase: setup AI participant, mock GameMaster, execute phase
    - Assert exactly MovementLimit units moved
    - Assert units moved in correct priority order
    - Assert no validation errors occurred
    - Assert fleet maintains formation cohesion
    - Test complete deployment phase with sector assignments and spacing
    - _Requirements: 1.1-1.5, 11.1-12.10, 14.1-14.7_

- [x] 13. Checkpoint - Final validation
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 14. Documentation and configuration
  - [ ] 14.1 Create README.md in Assets/Scripts/Game/AI/
    - Document architecture overview with component responsibilities
    - Document data flow for Movement phase processing
    - Document stance state machine transitions
    - Document key algorithms (priority scoring, tile scoring, stance selection)
    - Document configuration constants and tuning guidelines
    - _Requirements: 24.1-24.6_
  
  - [ ] 14.2 Add XML documentation to all public APIs
    - Document all public methods with <summary> tags
    - Document parameters with <param> tags
    - Document return values with <returns> tags
    - Document important fields and properties
    - _Requirements: 26.1-26.7_
  
  - [ ] 14.3 Document configuration constants with tuning guidance
    - Add comments explaining tactical purpose of each weight
    - Document thresholds and their effects on behavior
    - Provide guidance for increasing aggression vs. caution
    - Provide guidance for favoring specific unit roles
    - Provide guidance for formation tightness
    - _Requirements: 24.1-24.6_

## Notes

- Tasks marked with `*` are optional test-related sub-tasks that can be skipped for faster MVP delivery
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation throughout implementation
- The design document specifies this is NOT suitable for property-based testing due to external dependencies, so all testing uses unit tests and integration tests
- The implementation is fully self-contained in `Assets/Scripts/Game/AI/` with no modifications to existing game code
- All state management is instance-based or participant-keyed to support clean initialization/cleanup between matches
- The system integrates with existing code in `c:\Users\user\Admirals\AI\` and `c:\Users\user\Admirals\Fleet\` as reference for patterns and conventions

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "3.1"] },
    { "id": 2, "tasks": ["2.2", "3.2"] },
    { "id": 3, "tasks": ["3.3", "3.4", "5.1"] },
    { "id": 4, "tasks": ["3.5", "5.2"] },
    { "id": 5, "tasks": ["5.3", "6.1"] },
    { "id": 6, "tasks": ["5.4", "6.2"] },
    { "id": 7, "tasks": ["6.3", "6.4"] },
    { "id": 8, "tasks": ["6.5", "6.6"] },
    { "id": 9, "tasks": ["6.7", "8.1"] },
    { "id": 10, "tasks": ["8.2", "8.3"] },
    { "id": 11, "tasks": ["8.4", "9.1"] },
    { "id": 12, "tasks": ["9.2", "9.3"] },
    { "id": 13, "tasks": ["9.4", "9.5"] },
    { "id": 14, "tasks": ["9.6", "9.7"] },
    { "id": 15, "tasks": ["9.8", "10.1"] },
    { "id": 16, "tasks": ["10.2", "10.3"] },
    { "id": 17, "tasks": ["11.1", "11.2"] },
    { "id": 18, "tasks": ["11.3", "12.1"] },
    { "id": 19, "tasks": ["12.2", "12.3"] },
    { "id": 20, "tasks": ["12.4", "14.1"] },
    { "id": 21, "tasks": ["14.2", "14.3"] }
  ]
}
```


