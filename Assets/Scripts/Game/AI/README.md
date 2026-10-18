# Admirals AI System

This directory contains the AI opponent system for the Admirals turn-based naval warfare game.

## Architecture Overview

The AI system is stratified into three layers:

1. **Strategic Layer (FleetCommander)**: Evaluates fleet state and selects high-level stances
2. **Tactical Layer (MovementPrioritizer, MovementPlanner)**: Ranks units and selects destinations
3. **Orchestration Layer (AIController)**: Coordinates phase transitions and manages commit workflow

## Directory Structure

### Data Models
- `UnitRole.cs` - Tactical role classification (Scout, Anchor, Striker)
- `FleetStance.cs` - Strategic posture enumeration (Hunt, Advance, Hold, DefensiveFallback, Retreat)
- `FleetMember.cs` - Per-unit metadata snapshot with tactical classification
- `FleetSnapshot.cs` - Immutable per-turn fleet state shared across subsystems
- `AITurnContext.cs` - Per-turn game state snapshot built from GameMaster APIs

### Core Components (To Be Implemented)
- `AIController.cs` - MonoBehaviour orchestrator, subscribes to GameMaster.PhaseChanged events
- `FleetCommander.cs` - Strategic stance manager and snapshot generator
- `MovementPrioritizer.cs` - Priority scoring system for unit movement order
- `MovementPlanner.cs` - Tile scoring and destination selection
- `DeploymentPlanner.cs` - Initial fleet placement strategy
- `EnemyMemory.cs` - Position tracking and prediction for fog-of-war

## Key Design Principles

- **Fog-of-war compliance**: All decisions reason only over visible enemy units
- **GameMaster API integration**: All operations validated through participant-filtered APIs
- **Self-contained implementation**: No modifications to existing game code outside this directory
- **No static state leakage**: All state is instance-based or participant-keyed
- **Graceful degradation**: System handles validation failures and low-visibility scenarios

## Requirements Mapping

This implementation satisfies:
- Requirement 14.1: Phase orchestration and turn context
- Requirement 14.5: Sequential movement processing
- Requirement 16.1-16.7: RuntimeUnit to snapshot data mapping
- Requirement 26.1-26.8: AI system self-containment

## Usage

The AI system is initialized by attaching `AIController` to a GameObject in the scene and assigning it a `RuntimeParticipant` reference. The controller subscribes to `GameMaster.PhaseChanged` events and coordinates all AI subsystems automatically.

## Testing

Unit tests focus on:
- Priority scoring calculations
- Tile scoring algorithms
- Stance selection logic
- Deployment sector assignment

Integration tests validate:
- GameMaster API integration
- Fog-of-war compliance
- Movement limit enforcement
- Sequential movement processing
