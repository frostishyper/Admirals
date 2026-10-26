using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// Unit tests for BattleAction data structures.
/// Validates struct initialization and data integrity.
/// </summary>
public class BattleActionTests
{
    [Test]
    public void BattleContext_Constructor_InitializesEngagedTargetsHashSet()
    {
        // Arrange & Act
        var context = new BattleContext(null, null);

        // Assert
        Assert.IsNotNull(context.EngagedTargetsThisPhase, "EngagedTargetsThisPhase should be initialized");
        Assert.AreEqual(0, context.EngagedTargetsThisPhase.Count, "EngagedTargetsThisPhase should be empty initially");
    }

    [Test]
    public void BattleContext_EngagedTargetsThisPhase_CanAddCoordinates()
    {
        // Arrange
        var context = new BattleContext(null, null);
        var coord1 = new Vector2Int(5, 10);
        var coord2 = new Vector2Int(8, 12);

        // Act
        context.EngagedTargetsThisPhase.Add(coord1);
        context.EngagedTargetsThisPhase.Add(coord2);

        // Assert
        Assert.AreEqual(2, context.EngagedTargetsThisPhase.Count, "Should contain 2 coordinates");
        Assert.IsTrue(context.EngagedTargetsThisPhase.Contains(coord1), "Should contain first coordinate");
        Assert.IsTrue(context.EngagedTargetsThisPhase.Contains(coord2), "Should contain second coordinate");
    }

    [Test]
    public void BattleContext_EngagedTargetsThisPhase_PreventsDuplicates()
    {
        // Arrange
        var context = new BattleContext(null, null);
        var coord = new Vector2Int(3, 7);

        // Act
        context.EngagedTargetsThisPhase.Add(coord);
        context.EngagedTargetsThisPhase.Add(coord); // Add same coordinate again

        // Assert
        Assert.AreEqual(1, context.EngagedTargetsThisPhase.Count, "HashSet should prevent duplicates");
    }

    [Test]
    public void TargetEvaluation_StructInitialization_AllFieldsAccessible()
    {
        // Arrange & Act
        var target = new TargetEvaluation
        {
            Coordinate = new Vector2Int(10, 15),
            RuntimeUnitID = "test-unit-123",
            TargetDomain = Domain.Surface,
            ThreatScore = 75.0f,
            FragilityScore = 50.0f,
            DistanceScore = 35.0f,
            CoordinationPenalty = -20.0f,
            TotalScore = 85.0f,
            IsAbsolutelyVisible = true
        };

        // Assert
        Assert.AreEqual(new Vector2Int(10, 15), target.Coordinate);
        Assert.AreEqual("test-unit-123", target.RuntimeUnitID);
        Assert.AreEqual(Domain.Surface, target.TargetDomain);
        Assert.AreEqual(75.0f, target.ThreatScore);
        Assert.AreEqual(50.0f, target.FragilityScore);
        Assert.AreEqual(35.0f, target.DistanceScore);
        Assert.AreEqual(-20.0f, target.CoordinationPenalty);
        Assert.AreEqual(85.0f, target.TotalScore);
        Assert.IsTrue(target.IsAbsolutelyVisible);
    }

    [Test]
    public void AttackEvaluation_StructInitialization_AllFieldsAccessible()
    {
        // Arrange & Act
        var attack = new AttackEvaluation
        {
            ActionKind = GameBattle.CombatActionKind.Weapon,
            SlotIndex = 2,
            ActionName = "Torpedo",
            ExpectedDamageScore = 80.0f,
            ReliabilityScore = 50.0f,
            PropertyBonusScore = 30.0f,
            ChargeEconomyScore = 20.0f,
            TotalScore = 90.0f,
            UsesD20 = false
        };

        // Assert
        Assert.AreEqual(GameBattle.CombatActionKind.Weapon, attack.ActionKind);
        Assert.AreEqual(2, attack.SlotIndex);
        Assert.AreEqual("Torpedo", attack.ActionName);
        Assert.AreEqual(80.0f, attack.ExpectedDamageScore);
        Assert.AreEqual(50.0f, attack.ReliabilityScore);
        Assert.AreEqual(30.0f, attack.PropertyBonusScore);
        Assert.AreEqual(20.0f, attack.ChargeEconomyScore);
        Assert.AreEqual(90.0f, attack.TotalScore);
        Assert.IsFalse(attack.UsesD20);
    }

    [Test]
    public void DefenseEvaluation_StructInitialization_AllFieldsAccessible()
    {
        // Arrange & Act
        var defense = new DefenseEvaluation
        {
            ActionKind = GameBattle.CombatActionKind.System,
            SlotIndex = 1,
            ActionName = "ECM Suite",
            SupportUnitID = "ally-unit-456",
            IsAlliedSupport = true,
            NegationScore = 100.0f,
            MitigationScore = 40.0f,
            PropertyStripScore = 25.0f,
            ReliabilityScore = 40.0f,
            TotalScore = 185.5f,
            UsesD20 = false
        };

        // Assert
        Assert.AreEqual(GameBattle.CombatActionKind.System, defense.ActionKind);
        Assert.AreEqual(1, defense.SlotIndex);
        Assert.AreEqual("ECM Suite", defense.ActionName);
        Assert.AreEqual("ally-unit-456", defense.SupportUnitID);
        Assert.IsTrue(defense.IsAlliedSupport);
        Assert.AreEqual(100.0f, defense.NegationScore);
        Assert.AreEqual(40.0f, defense.MitigationScore);
        Assert.AreEqual(25.0f, defense.PropertyStripScore);
        Assert.AreEqual(40.0f, defense.ReliabilityScore);
        Assert.AreEqual(185.5f, defense.TotalScore);
        Assert.IsFalse(defense.UsesD20);
    }
}
