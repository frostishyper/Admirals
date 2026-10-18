# Requirements Document

## Introduction

This document specifies the requirements for an AI opponent system for the Admirals turn-based naval warfare game. The AI must operate within the game's movement limitation system (only a subset of units can move per turn), work through GameMaster's participant-filtered APIs, respect fog-of-war rules, and make intelligent strategic and tactical decisions for fleet deployment, movement, and coordination.

## Glossary

- **AI_Controller**: The main orchestrator that handles phase transitions and coordinates AI subsystems
- **Fleet_Commander**: Strategic layer that evaluates fleet state and selects high-level stances
- **Movement_Prioritizer**: Subsystem that ranks units by movement priority when only N of M units can move
- **Movement_Planner**: Tactical layer that scores tiles and selects destinations for individual units
- **Deployment_Planner**: Subsystem that handles initial fleet placement on the deployment zone
- **GameMaster**: Central authoritative orchestrator exposing participant-filtered gameplay APIs
- **GameMovement**: Authoritative movement phase subsystem with validation and commit operations
- **GameDeployment**: Authoritative deployment phase subsystem with preparation and commit operations
- **GameKnowledge**: Fog-of-war subsystem tracking per-participant visibility
- **RuntimeUnit**: Active unit instance with definition template, position, facing, and runtime state
- **RuntimeParticipant**: Player identity with fleet roster and runtime state
- **MovementLimit**: The maximum number of units that can move in a turn, calculated as (ActiveUnits + 1) / 2
- **FleetStance**: High-level strategic posture (Hunt, Advance, Hold, DefensiveFallback, Retreat)
- **UnitRole**: Tactical classification (Scout, Anchor, Striker) based on speed, durability, and firepower
- **FleetSnapshot**: Per-turn summary of fleet state including positions, health, contacts, and derived metrics
- **Contact**: An enemy unit that is currently visible to the AI through fog-of-war
- **EnemyMemory**: AI-maintained record of last known positions and headings for units no longer visible
- **TileScore**: Numeric evaluation of a destination tile based on attack opportunity, danger, and formation
- **Stance_Dwell**: Minimum number of turns a stance must be maintained before switching (hysteresis)
- **Material_Score**: Fleet strength ratio comparing own alive units to enemy estimated alive units

## Requirements

### Requirement 1: Movement Limit Compliance

**User Story:** As the AI system, I want to respect the game's movement limitation rules, so that I operate within valid gameplay constraints.

#### Acceptance Criteria

1. THE AI_Controller SHALL query GameMovement.ParticipantView to read MovementLimit before planning moves
2. WHEN MovesCommitted equals MovementLimit, THE AI_Controller SHALL stop attempting unit movements for the current turn
3. THE AI_Controller SHALL track which RuntimeUnits have successfully moved to prevent duplicate move attempts
4. WHEN GameMovement.TryCommitMovement returns success, THE AI_Controller SHALL increment its internal move counter
5. THE AI_Controller SHALL handle movement rejection gracefully without counting failed attempts against the limit

### Requirement 2: Movement Priority System

**User Story:** As the AI system, I want to prioritize which units move when only a subset can move, so that the most tactically valuable movements happen first.

#### Acceptance Criteria

1. THE Movement_Prioritizer SHALL assign each RuntimeUnit a numeric priority score before movement planning begins
2. THE Movement_Prioritizer SHALL incorporate UnitRole into priority calculation with Scouts receiving highest base priority
3. THE Movement_Prioritizer SHALL increase priority for units that can reach attack range of visible enemies
4. THE Movement_Prioritizer SHALL decrease priority for units that are blocked or have no safe destinations
5. WHEN FleetStance is Retreat, THE Movement_Prioritizer SHALL prioritize damaged units (currentHealth < maxHealth * 0.4)
6. WHEN FleetStance is Advance or Hunt, THE Movement_Prioritizer SHALL prioritize units closest to enemy contacts
7. THE Movement_Prioritizer SHALL sort RuntimeUnits by descending priority and return the ordered list to AI_Controller
8. THE AI_Controller SHALL process unit movements in priority order until MovementLimit is reached

### Requirement 3: GameMaster API Integration

**User Story:** As the AI system, I want to interact with GameMaster subsystems through participant-filtered APIs, so that I access only information my participant should know and operations are validated authoritatively.

#### Acceptance Criteria

1. THE AI_Controller SHALL query GameMaster.GameMovement to access the Movement subsystem
2. THE AI_Controller SHALL query GameMaster.GameDeployment to access the Deployment subsystem
3. THE AI_Controller SHALL query GameMaster.GameKnowledge to access the Knowledge subsystem
4. THE AI_Controller SHALL pass its RuntimeParticipant identity to all API calls requiring participant context
5. WHEN calling GameMovement.ValidateMovement, THE AI_Controller SHALL receive a MovementPlan and validation message
6. WHEN calling GameMovement.TryCommitMovement, THE AI_Controller SHALL receive a CommitResult and outcome message
7. THE AI_Controller SHALL respect validation failures and not attempt unauthorized operations
8. THE AI_Controller SHALL handle API errors gracefully with logged diagnostics

### Requirement 4: Fog-of-War Compliance

**User Story:** As the AI system, I want to reason only over information visible to my participant, so that I play fairly within fog-of-war rules.

#### Acceptance Criteria

1. THE AI_Controller SHALL query GameKnowledge.IsUnitVisibleToParticipant before considering an enemy unit in decision-making
2. THE AI_Controller SHALL build a contacts list containing only visible enemy RuntimeUnits
3. THE AI_Controller SHALL NOT access enemy unit positions directly from GameMaster.GameUnits without visibility checks
4. WHEN a previously visible enemy unit becomes hidden, THE EnemyMemory SHALL record its last known position and facing
5. THE EnemyMemory SHALL predict likely current positions for hidden enemies based on last known heading and speed
6. WHEN evaluating tile danger, THE Movement_Planner SHALL consider both visible contacts and predicted enemy positions
7. THE AI_Controller SHALL clear EnemyMemory predictions older than 5 turns
8. THE EnemyMemory SHALL update predictions when a previously hidden unit becomes visible again

### Requirement 5: Fleet Strategic Stance Selection

**User Story:** As the AI system, I want to evaluate fleet state and select an appropriate strategic stance, so that the fleet behaves coherently according to tactical conditions.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL calculate Material_Score as (own alive count) / (own start count + enemy estimated alive count)
2. THE Fleet_Commander SHALL calculate FleetHpFraction as (sum of currentHealth) / (sum of maxHealth) for alive units
3. WHEN Material_Score < 0.35 OR FleetHpFraction < 0.30, THE Fleet_Commander SHALL select Retreat stance
4. WHEN Material_Score < 0.60 AND current stance is not Retreat, THE Fleet_Commander SHALL select DefensiveFallback stance
5. WHEN Material_Score >= 0.90 AND contacts list is not empty, THE Fleet_Commander SHALL select Advance stance
6. WHEN contacts list is empty AND current stance is not Retreat or DefensiveFallback, THE Fleet_Commander SHALL select Hunt stance
7. WHEN none of the above conditions apply, THE Fleet_Commander SHALL select Hold stance
8. THE Fleet_Commander SHALL apply hysteresis by widening current stance thresholds by 0.08 to prevent rapid stance flipping
9. THE Fleet_Commander SHALL enforce minimum Stance_Dwell of 2 turns for non-Retreat stances
10. WHEN transitioning to Retreat or leaving Hunt, THE Fleet_Commander SHALL ignore Stance_Dwell and transition immediately

### Requirement 6: Fleet Unit Role Classification

**User Story:** As the AI system, I want to classify each unit by tactical role, so that movement and formation logic can treat units appropriately.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL assign each RuntimeUnit a UnitRole based on its Definition_Unit properties
2. WHEN a unit's maxSpeed >= 6 AND its weapon range >= 4, THE Fleet_Commander SHALL classify it as Scout
3. WHEN a unit's maxHealth >= 80 AND its weapon count >= 2, THE Fleet_Commander SHALL classify it as Anchor
4. WHEN a unit does not meet Scout or Anchor criteria, THE Fleet_Commander SHALL classify it as Striker
5. THE Fleet_Commander SHALL recalculate UnitRole assignments at the start of each Move phase
6. THE Fleet_Commander SHALL include UnitRole in the FleetSnapshot for use by other subsystems

### Requirement 7: Fleet Snapshot Generation

**User Story:** As the AI system, I want to build a comprehensive snapshot of fleet state each turn, so that all subsystems reason over consistent data.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL generate a FleetSnapshot at the start of each Move phase
2. THE FleetSnapshot SHALL include a list of all alive RuntimeUnits belonging to the AI participant
3. THE FleetSnapshot SHALL include a list of all visible enemy RuntimeUnits (contacts)
4. THE FleetSnapshot SHALL include Material_Score and FleetHpFraction calculations
5. THE FleetSnapshot SHALL include current FleetStance and turns spent in current stance
6. THE FleetSnapshot SHALL include fleet centroid position calculated as mean position of alive units
7. THE FleetSnapshot SHALL include home centroid position calculated as mean of starting positions (captured on first turn)
8. THE FleetSnapshot SHALL include nearest contact distance (minimum distance from any own unit to any contact)
9. THE FleetSnapshot SHALL include map progress ratio calculated as (fleet centroid distance from home) / (map diagonal)
10. THE FleetSnapshot SHALL be immutable after generation and shared across all subsystems for the turn

### Requirement 8: Movement Tile Scoring

**User Story:** As the AI system, I want to evaluate potential destination tiles with multiple factors, so that unit movements balance offense, safety, and formation cohesion.

#### Acceptance Criteria

1. THE Movement_Planner SHALL generate candidate tiles within the unit's movement range
2. FOR EACH candidate tile, THE Movement_Planner SHALL calculate a TileScore as the weighted sum of multiple factors
3. THE Movement_Planner SHALL add attack opportunity score based on weapon range to visible enemies from the candidate tile
4. THE Movement_Planner SHALL subtract danger score based on enemy weapon threat range from the candidate tile
5. THE Movement_Planner SHALL add cohesion bonus based on proximity to fleet centroid (stronger when FleetStance is DefensiveFallback or Retreat)
6. THE Movement_Planner SHALL add forward bonus for Scouts when FleetStance is Hunt or Advance
7. THE Movement_Planner SHALL add fragility pushback penalty for low-health units positioned far from fleet centroid
8. THE Movement_Planner SHALL select the candidate tile with the highest TileScore as the destination
9. WHEN all candidate tiles have negative TileScore AND FleetStance is Retreat, THE Movement_Planner SHALL select the tile with least negative score
10. WHEN all candidate tiles are invalid or blocked, THE Movement_Planner SHALL return the unit's current position as the destination

### Requirement 9: Formation Cohesion Maintenance

**User Story:** As the AI system, I want to maintain fleet formation during movement, so that units support each other and avoid isolation.

#### Acceptance Criteria

1. THE Movement_Planner SHALL calculate cohesion score as negative squared distance from candidate tile to fleet centroid
2. WHEN FleetStance is DefensiveFallback or Retreat, THE Movement_Planner SHALL multiply cohesion weight by 2.0
3. WHEN FleetStance is Hunt or Advance, THE Movement_Planner SHALL multiply cohesion weight by 0.5
4. WHEN a unit's UnitRole is Scout AND FleetStance is Hunt, THE Movement_Planner SHALL apply negative cohesion weight to encourage forward scouting
5. WHEN a unit's currentHealth < maxHealth * 0.4, THE Movement_Planner SHALL increase cohesion weight by 1.5 to pull damaged units toward fleet
6. THE Movement_Planner SHALL clamp maximum cohesion influence to prevent overriding critical safety considerations

### Requirement 10: Target Claiming for Movement Coordination

**User Story:** As the AI system, I want to track which enemies are being approached by moving units, so that the fleet spreads across multiple targets rather than converging on one.

#### Acceptance Criteria

1. THE AI_Controller SHALL maintain a claimed targets set for the duration of the Move phase
2. WHEN Movement_Planner selects a destination that closes distance to a specific enemy, THE Movement_Planner SHALL identify that enemy as the likely target
3. WHEN a unit successfully commits a movement toward a likely target, THE AI_Controller SHALL add that enemy to the claimed targets set
4. WHEN evaluating attack opportunity score for a candidate tile, THE Movement_Planner SHALL reduce the score by 50% if the nearest enemy is already claimed
5. THE AI_Controller SHALL clear the claimed targets set at the end of the Move phase
6. THE Movement_Planner SHALL NOT reduce attack opportunity score for claimed targets when FleetStance is Advance and Material_Score > 0.85

### Requirement 11: Deployment Zone Analysis

**User Story:** As the AI system, I want to analyze the deployment zone geometry, so that initial placement is intelligent and balanced.

#### Acceptance Criteria

1. THE Deployment_Planner SHALL query GameDeployment to determine the deployment zone tile list
2. THE Deployment_Planner SHALL calculate the deployment zone centroid as the mean position of all deployment tiles
3. THE Deployment_Planner SHALL divide the deployment zone into 3 sectors: left, center, right based on perpendicular axis to enemy direction
4. THE Deployment_Planner SHALL assign each deployment tile to one of the 3 sectors
5. THE Deployment_Planner SHALL estimate enemy direction by finding the opposite edge of the map from deployment zone centroid
6. THE Deployment_Planner SHALL select facing direction toward the estimated enemy direction for all units

### Requirement 12: Deployment Placement Strategy

**User Story:** As the AI system, I want to distribute units across deployment sectors with appropriate spacing, so that the fleet starts in a tactically sound formation.

#### Acceptance Criteria

1. THE Deployment_Planner SHALL assign Anchor units to the center sector
2. THE Deployment_Planner SHALL assign Scout units evenly between left and right sectors
3. THE Deployment_Planner SHALL assign Striker units to fill gaps in sector distribution
4. FOR EACH unit, THE Deployment_Planner SHALL select a placement tile within its assigned sector
5. THE Deployment_Planner SHALL enforce minimum spacing of 2 tiles between unit placement positions
6. WHEN no valid tile exists with minimum spacing, THE Deployment_Planner SHALL progressively relax spacing by 1 tile increments
7. THE Deployment_Planner SHALL validate each placement with GameDeployment.ValidatePlacement before committing
8. WHEN validation fails, THE Deployment_Planner SHALL select an alternative tile in the same sector
9. THE Deployment_Planner SHALL commit all placements using GameDeployment.TrySubmitPlacement
10. THE Deployment_Planner SHALL log placement failures for diagnostic purposes

### Requirement 13: Movement Pathfinding Integration

**User Story:** As the AI system, I want to leverage GameMovement's pathfinding validation, so that planned movements are always executable.

#### Acceptance Criteria

1. WHEN selecting a destination, THE Movement_Planner SHALL call GameMovement.ValidateMovement to generate a MovementPlan
2. THE Movement_Planner SHALL use the MovementPlan.Route to verify a path exists to the destination
3. WHEN MovementPlan.Route is empty AND destination is not the current position, THE Movement_Planner SHALL mark the destination as blocked
4. THE Movement_Planner SHALL exclude blocked destinations from candidate tile selection
5. WHEN all high-priority destinations are blocked, THE Movement_Planner SHALL fall back to lower-priority tiles with valid paths
6. THE Movement_Planner SHALL consider MovementPlan.WouldConsumeMove to avoid wasting move attempts on in-place rotations during critical turns

### Requirement 14: Phase Orchestration and Turn Context

**User Story:** As the AI system, I want to react to phase transitions and build consistent turn context, so that decision-making is synchronized with game flow.

#### Acceptance Criteria

1. THE AI_Controller SHALL subscribe to GameMaster.PhaseChanged events during initialization
2. WHEN PhaseChanged fires with Phase.Deployment, THE AI_Controller SHALL invoke Deployment_Planner
3. WHEN PhaseChanged fires with Phase.Move, THE AI_Controller SHALL invoke Fleet_Commander and Movement_Prioritizer
4. WHEN PhaseChanged fires with Phase.Search, THE AI_Controller SHALL handle active scan planning (if applicable)
5. THE AI_Controller SHALL check that the current participant matches its assigned RuntimeParticipant before processing any phase
6. THE AI_Controller SHALL unsubscribe from PhaseChanged events during cleanup to prevent memory leaks
7. THE AI_Controller SHALL log phase transitions and major decisions for debugging and telemetry

### Requirement 15: Sequential Movement Processing

**User Story:** As the AI system, I want to process unit movements sequentially, so that each unit's pathfinding sees previously moved units in their new positions.

#### Acceptance Criteria

1. THE AI_Controller SHALL process unit movements one at a time in priority order
2. WHEN a unit's movement commits successfully, THE AI_Controller SHALL wait for GameMovement state updates before processing the next unit
3. THE Movement_Planner SHALL query current unit positions from GameMaster.GameUnits before scoring tiles
4. THE Movement_Planner SHALL treat previously moved friendly units as obstacles when evaluating candidate tiles
5. THE AI_Controller SHALL NOT parallelize movement decisions to avoid race conditions

### Requirement 16: RuntimeUnit to Snapshot Data Mapping

**User Story:** As the AI system, I want to map between GameMaster's RuntimeUnit instances and my internal snapshot data structures, so that I can reason over consistent unit state.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL extract position from RuntimeUnit.CurrentTileCoordinate
2. THE Fleet_Commander SHALL extract facing from RuntimeUnit.CurrentFacing
3. THE Fleet_Commander SHALL extract health from RuntimeUnit.CurrentHealth and RuntimeUnit.MaxHealth
4. THE Fleet_Commander SHALL extract movement capability from RuntimeUnit.RemainingMovementBudget
5. THE Fleet_Commander SHALL extract unit identity from RuntimeUnit.RuntimeID
6. THE Fleet_Commander SHALL extract unit definition from RuntimeUnit.Definition
7. THE Fleet_Commander SHALL handle destroyed units (LifeState == Destroyed) by excluding them from alive unit lists

### Requirement 17: Enemy Alive Estimation

**User Story:** As the AI system, I want to estimate the number of living enemy units, so that Material_Score calculations remain accurate despite fog-of-war.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL initialize enemy alive estimate to the starting fleet size at the beginning of the match
2. WHEN a friendly unit destroys an enemy unit, THE Fleet_Commander SHALL decrement enemy alive estimate by 1
3. THE Fleet_Commander SHALL NOT decrement enemy alive estimate below zero
4. THE Fleet_Commander SHALL use enemy alive estimate in Material_Score calculation as the denominator component
5. THE Fleet_Commander SHALL adjust enemy alive estimate when fog lifts and previously unknown casualties are revealed

### Requirement 18: Stance Transition Logging

**User Story:** As the AI system, I want to log stance transitions with context, so that AI behavior is transparent and debuggable.

#### Acceptance Criteria

1. WHEN FleetStance changes, THE Fleet_Commander SHALL log the previous stance and new stance
2. THE Fleet_Commander SHALL log Material_Score and FleetHpFraction values that triggered the stance change
3. WHEN a stance change is blocked by Stance_Dwell, THE Fleet_Commander SHALL log the desired stance and remaining dwell turns
4. THE Fleet_Commander SHALL log the count of alive own units and estimated enemy units
5. THE Fleet_Commander SHALL log the count of visible contacts and nearest contact distance

### Requirement 19: Deployment Phase Completion Detection

**User Story:** As the AI system, I want to detect when all units are placed, so that deployment phase completes correctly.

#### Acceptance Criteria

1. THE Deployment_Planner SHALL query GameDeployment.ParticipantView to read placement progress
2. WHEN all fleet roster entries have been placed, THE Deployment_Planner SHALL signal completion to AI_Controller
3. THE AI_Controller SHALL invoke GameDeployment.TryCommitDeployment to finalize placement
4. WHEN deployment commit fails, THE AI_Controller SHALL log the failure reason
5. THE Deployment_Planner SHALL handle partial placement failures by attempting alternative tiles

### Requirement 20: Movement Validation Error Handling

**User Story:** As the AI system, I want to handle movement validation failures gracefully, so that transient errors do not halt AI processing.

#### Acceptance Criteria

1. WHEN GameMovement.ValidateMovement returns failure, THE Movement_Planner SHALL log the validation message
2. THE Movement_Planner SHALL mark the rejected destination as invalid for the current turn
3. THE Movement_Planner SHALL attempt alternative destinations in descending TileScore order
4. WHEN GameMovement.TryCommitMovement returns failure, THE AI_Controller SHALL log the commit message and continue to next unit
5. WHEN a unit has no valid destinations, THE AI_Controller SHALL skip that unit and continue processing remaining units

### Requirement 21: Danger Assessment from Enemy Threats

**User Story:** As the AI system, I want to evaluate danger at candidate tiles based on enemy weapon ranges, so that units avoid moving into high-threat areas.

#### Acceptance Criteria

1. FOR EACH visible enemy unit, THE Movement_Planner SHALL enumerate its weapon slots
2. FOR EACH weapon slot with a Definition_Weapon, THE Movement_Planner SHALL extract the weapon's maximum range
3. WHEN a candidate tile is within enemy weapon range, THE Movement_Planner SHALL add a danger penalty to the TileScore
4. THE Movement_Planner SHALL scale danger penalty by the expected damage value of the threatening weapon
5. THE Movement_Planner SHALL aggregate danger from multiple threatening enemies
6. WHEN FleetStance is Retreat or DefensiveFallback, THE Movement_Planner SHALL multiply danger weight by 1.5
7. THE Movement_Planner SHALL include predicted enemy positions from EnemyMemory in danger calculations

### Requirement 22: Attack Opportunity Assessment

**User Story:** As the AI system, I want to evaluate attack opportunities from candidate tiles, so that units position for effective engagement.

#### Acceptance Criteria

1. FOR EACH candidate tile, THE Movement_Planner SHALL calculate the distance to each visible enemy unit
2. FOR EACH weapon slot on the moving unit, THE Movement_Planner SHALL check if any enemy is within weapon range from the candidate tile
3. WHEN an enemy is in range, THE Movement_Planner SHALL add attack opportunity bonus to the TileScore
4. THE Movement_Planner SHALL scale attack opportunity bonus by the expected damage value of the weapon
5. THE Movement_Planner SHALL prioritize closer enemies by applying a distance decay factor to the bonus
6. WHEN FleetStance is Advance, THE Movement_Planner SHALL multiply attack opportunity weight by 1.5
7. WHEN FleetStance is Retreat, THE Movement_Planner SHALL multiply attack opportunity weight by 0.3

### Requirement 23: Priority Recomputation on Blockage

**User Story:** As the AI system, I want to dynamically recompute movement priorities when high-priority units are blocked, so that movement slots are not wasted.

#### Acceptance Criteria

1. WHEN a high-priority unit has no valid destinations, THE Movement_Prioritizer SHALL mark that unit as blocked
2. THE AI_Controller SHALL skip blocked units during movement processing
3. THE Movement_Prioritizer SHALL recalculate priorities for remaining unblocked units
4. THE AI_Controller SHALL continue processing units in updated priority order until MovementLimit is reached
5. WHEN all unblocked units have moved, THE AI_Controller SHALL stop processing even if moves remain available

### Requirement 24: Configuration and Tuning Parameters

**User Story:** As the AI system, I want to expose key decision parameters as configurable constants, so that AI behavior can be tuned without structural changes.

#### Acceptance Criteria

1. THE Fleet_Commander SHALL define stance threshold constants (RetreatScoreBelow, FallbackScoreBelow, AdvanceScoreAtLeast) at class scope
2. THE Fleet_Commander SHALL define hysteresis margin and Stance_Dwell values as configurable constants
3. THE Movement_Planner SHALL define tile scoring weights (attack opportunity, danger, cohesion, forward bonus) as configurable constants
4. THE Deployment_Planner SHALL define minimum spacing and spacing relaxation step as configurable constants
5. THE EnemyMemory SHALL define prediction expiration time (turns) as a configurable constant
6. All configuration constants SHALL be documented with comments explaining their tactical purpose

### Requirement 25: Graceful Degradation Under Low Visibility

**User Story:** As the AI system, I want to make reasonable decisions when fog-of-war provides limited information, so that gameplay continues smoothly in low-information states.

#### Acceptance Criteria

1. WHEN the contacts list is empty, THE Fleet_Commander SHALL select Hunt stance to encourage exploration
2. WHEN the contacts list is empty, THE Movement_Planner SHALL bias tile scores toward unexplored map regions
3. THE Movement_Planner SHALL use EnemyMemory predictions as secondary targets when no contacts are visible
4. WHEN both contacts and predictions are unavailable, THE Movement_Planner SHALL bias movement toward the map center
5. THE AI_Controller SHALL NOT halt or error when operating with zero visible enemies

### Requirement 26: AI System Self-Containment

**User Story:** As the AI system, I want to be fully contained within the Assets/Scripts/Game/AI/ directory, so that I do not require modifications to existing game code.

#### Acceptance Criteria

1. THE AI_Controller SHALL be implemented as a new MonoBehaviour class in Assets/Scripts/Game/AI/
2. THE Fleet_Commander SHALL be implemented as a plain C# class in Assets/Scripts/Game/AI/
3. THE Movement_Prioritizer SHALL be implemented as a static utility class in Assets/Scripts/Game/AI/
4. THE Movement_Planner SHALL be implemented as a static utility class in Assets/Scripts/Game/AI/
5. THE Deployment_Planner SHALL be implemented as a static utility class in Assets/Scripts/Game/AI/
6. THE EnemyMemory SHALL be implemented as a static utility class with participant-keyed storage in Assets/Scripts/Game/AI/
7. All AI classes SHALL interact with GameMaster and its subsystems exclusively through public APIs
8. THE AI system SHALL NOT modify any files outside Assets/Scripts/Game/AI/

### Requirement 27: AI Initialization and Lifecycle

**User Story:** As the AI system, I want to initialize cleanly at match start and clean up at match end, so that state does not leak between games.

#### Acceptance Criteria

1. THE AI_Controller SHALL initialize Fleet_Commander lazily on the first Move phase event
2. THE Fleet_Commander constructor SHALL capture starting fleet size and initialize enemy alive estimate
3. THE EnemyMemory SHALL clear all participant-keyed storage at the start of a new match
4. THE AI_Controller SHALL reset all turn-scoped state (claimed targets, move counters) at the start of each phase
5. WHEN AI_Controller is disabled or destroyed, THE AI_Controller SHALL unsubscribe from all events
6. THE AI system SHALL NOT use static mutable state that persists between matches

### Requirement 28: Retreat Movement Behavior

**User Story:** As the AI system, I want to move units away from enemies when in Retreat stance, so that damaged fleets disengage safely.

#### Acceptance Criteria

1. WHEN FleetStance is Retreat, THE Movement_Planner SHALL add retreat vector term to tile scoring
2. THE retreat vector term SHALL be calculated as distance from candidate tile to nearest enemy contact
3. THE Movement_Planner SHALL weight retreat vector more heavily than attack opportunity when in Retreat stance
4. WHEN a unit's currentHealth < maxHealth * 0.4, THE Movement_Planner SHALL double the retreat vector weight for that unit
5. THE Movement_Planner SHALL combine retreat vector with cohesion to keep retreating units together

### Requirement 29: Hunt Movement Behavior

**User Story:** As the AI system, I want to explore the map systematically when in Hunt stance, so that the AI discovers enemy positions.

#### Acceptance Criteria

1. WHEN FleetStance is Hunt AND contacts list is empty, THE Movement_Planner SHALL add exploration bonus to tile scoring
2. THE exploration bonus SHALL favor tiles that are farther from home centroid
3. THE exploration bonus SHALL favor tiles closer to map edges and corners where enemies may deploy
4. THE Movement_Planner SHALL use EnemyMemory predictions to bias exploration toward last known enemy regions
5. WHEN a contact is detected during Hunt, THE Movement_Planner SHALL transition to approaching that contact on subsequent moves

### Requirement 30: Movement Budget Awareness

**User Story:** As the AI system, I want to respect individual unit movement budgets, so that fast and slow units are handled correctly.

#### Acceptance Criteria

1. THE Movement_Planner SHALL query RuntimeUnit.RemainingMovementBudget before generating candidate tiles
2. THE Movement_Planner SHALL generate candidate tiles only within the unit's movement budget radius
3. WHEN a unit has zero RemainingMovementBudget, THE Movement_Planner SHALL skip that unit
4. THE Movement_Planner SHALL use GameMovement.ValidateMovement to confirm the path length is within budget
5. THE Movement_Prioritizer SHALL consider RemainingMovementBudget when calculating priority (higher budget = higher opportunity)
