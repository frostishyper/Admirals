using UnityEngine;

/// <summary>
/// Simple validation test for AI data models.
/// Verifies that all classes instantiate correctly and properties are accessible.
/// This file can be removed after unit tests are implemented.
/// </summary>
public class AIDataModelsTest : MonoBehaviour
{
    /// <summary>
    /// Validates that all AI data model classes can be instantiated.
    /// </summary>
    public static void ValidateDataModels()
    {
        // Test enumerations
        UnitRole testRole = UnitRole.Scout;
        FleetStance testStance = FleetStance.Hunt;

        Debug.Log($"[AI][DataModels] UnitRole enumeration accessible: {testRole}");
        Debug.Log($"[AI][DataModels] FleetStance enumeration accessible: {testStance}");

        // Test FleetMember
        FleetMember member = new FleetMember
        {
            Role = UnitRole.Anchor,
            HpFraction = 0.75f,
            EffectiveFragility = 0.3f,
            Position = new Vector2Int(10, 10),
            Facing = UnitFacing.North
        };
        Debug.Log($"[AI][DataModels] FleetMember instantiated: Role={member.Role}, HpFraction={member.HpFraction}");

        // Test FleetSnapshot
        FleetSnapshot snapshot = new FleetSnapshot
        {
            CurrentStance = FleetStance.Advance,
            TurnsInStance = 2,
            OwnStartCount = 5,
            OwnAlive = 4,
            EnemyAliveEstimate = 3,
            MaterialScore = 0.8f,
            FleetHpFraction = 0.85f,
            FleetCentroid = new Vector2Int(15, 15),
            HomeCentroid = new Vector2Int(5, 5),
            MapProgress = 0.5f,
            NearestContactDistance = 8.5f
        };
        Debug.Log($"[AI][DataModels] FleetSnapshot instantiated: Stance={snapshot.CurrentStance}, MaterialScore={snapshot.MaterialScore:F2}");

        // Test AITurnContext
        AITurnContext context = new AITurnContext
        {
            CurrentPhase = GameplayPhase.Movement,
            TurnNumber = 3,
            MovementLimit = 3,
            MovesCommitted = 1,
            CanAct = true
        };
        Debug.Log($"[AI][DataModels] AITurnContext instantiated: Phase={context.CurrentPhase}, Turn={context.TurnNumber}");

        Debug.Log("[AI][DataModels] All data model validation tests passed!");
    }
}
