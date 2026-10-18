# DeploymentPlanner Implementation Notes

## Task 8.1 - Zone Analysis

This task implements the core zone analysis functionality for the DeploymentPlanner static class, fulfilling Requirements 11.1-11.6.

### Implemented Methods

#### `ExecuteDeployment`
Main orchestration method for deployment phase processing.
- Queries GameDeployment for deployment zone
- Calculates zone centroid
- Estimates enemy direction
- Calculates unit facing
- Returns success/failure status
- **Requirement 11.1**: Main method implementation

#### `CalculateDeploymentCentroid`
Calculates the geometric centroid of deployment zone tiles.
- Computes mean position of all tiles in zone
- Handles empty zones gracefully (returns Vector2Int.zero)
- **Requirement 11.2**: Deployment zone centroid calculation

#### `EstimateEnemyDirection`
Estimates enemy location by finding opposite map edge.
- Calculates distance to each map edge (North, South, East, West)
- Identifies closest edge to deployment centroid
- Returns normalized direction toward opposite edge
- **Requirement 11.5**: Enemy direction estimation algorithm

#### `CalculateFacingToward`
Converts direction vector to UnitFacing enum value.
- Uses dot product to find best cardinal alignment
- Handles zero vectors (defaults to North)
- Returns North, East, South, or West facing
- **Requirement 11.6**: Facing calculation toward enemy

#### `NormalizeDirection` (Private Helper)
Normalizes direction vectors to unit length with integer components.
- Handles zero magnitude vectors (defaults to East)
- Returns normalized Vector2Int for direction calculations

### Design Patterns

The implementation follows patterns from the existing AI codebase:

1. **Static Utility Class**: DeploymentPlanner is static like MovementPrioritizer and MovementPlanner
2. **Comprehensive XML Documentation**: All public methods have detailed summaries, parameter docs, and requirement references
3. **Error Handling**: Null checks and graceful degradation for edge cases
4. **Logging**: Debug logging for key decisions and error conditions

### Test Coverage

Comprehensive unit tests in `DeploymentPlannerTest.cs`:

- **Centroid Tests**: Single tile, multiple tiles, empty zone
- **Direction Tests**: North, South, East, West deployments
- **Facing Tests**: All cardinal directions, diagonals, zero vector

All tests can be run via the `TestRunner` MonoBehaviour component.

### Remaining Work

Future tasks will implement:
- **Task 8.2**: Sector division (Left/Center/Right) and unit assignment by role
- **Task 8.3**: Placement with spacing enforcement and validation

### Integration Points

- **GameDeployment API**: Uses `TryGetView` to query deployment zone
- **GridManager**: Provides map dimensions for direction estimation
- **AITurnContext**: Provides participant context and game state
- **FleetSnapshot**: Will be used for unit role information in Task 8.2

### Algorithm Details

#### Enemy Direction Estimation Algorithm

```
1. Calculate distance to each map edge:
   - distToNorth = mapHeight - centroidY
   - distToSouth = centroidY
   - distToEast = mapWidth - centroidX
   - distToWest = centroidX

2. Find closest edge (minimum distance)

3. Select opposite edge center:
   - If closest is North → opposite is South at (mapWidth/2, 0)
   - If closest is South → opposite is North at (mapWidth/2, mapHeight-1)
   - If closest is East → opposite is West at (0, mapHeight/2)
   - If closest is West → opposite is East at (mapWidth-1, mapHeight/2)

4. Calculate direction vector from centroid to opposite edge
5. Normalize to unit vector
```

#### Facing Calculation Algorithm

```
1. Define cardinal direction vectors:
   - North: (0, 1)
   - East: (1, 0)
   - South: (0, -1)
   - West: (-1, 0)

2. Calculate dot product with input direction for each cardinal

3. Return the cardinal direction with maximum dot product (best alignment)
```

### Notes

- The implementation is self-contained in `Assets/Scripts/Game/AI/`
- No modifications to existing game code required
- All state is passed as parameters (no static mutable state)
- Compatible with Unity's coordinate system conventions
