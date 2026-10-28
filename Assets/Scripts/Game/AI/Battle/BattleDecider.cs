using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main orchestration class for AI Battle phase decision-making.
/// Coordinates target selection, attack selection, defense selection, and engagement resolution
/// to produce tactical combat decisions that mirror human player strategic thinking.
/// </summary>
/// <remarks>
/// Decision Flow:
/// 1. Check if engagement is active
///    - If active and resolved → Wait or Continue
///    - If active and attacker's turn → Select attack
///    - If active and defender's turn → Select defense
/// 2. If no engagement active
///    - Discover all valid targets
///    - Score targets using TargetScorer
///    - Begin engagement with best target
///    - Or end Battle if no targets
///
/// This class maintains no state; all context is passed through BattleContext parameter.
/// Target coordination tracking (EngagedTargetsThisPhase) is maintained in BattleContext
/// and should be cleared at the start of each Battle phase.
///
/// Requirements: 1.1, 6.1
/// </remarks>
public static class BattleDecider
{
    // Configuration constants
    private const float DefaultUseDefenseChance = 0.75f;  // 75% chance to use defense
    private const float DefaultUseAllySupportChance = 0.25f;  // 25% chance to prefer ally support
    private const float ContinueEngagementDelay = 1.5f;  // Seconds to wait before continuing

    /// <summary>
    /// Main entry point for Battle phase AI decision-making.
    /// Evaluates current battle state and returns the next tactical command.
    /// </summary>
    /// <param name="snapshot">Current game state snapshot from participant's perspective.</param>
    /// <param name="access">Participant API access for submitting commands and queries.</param>
    /// <param name="context">Battle context containing coordination tracking and state.</param>
    /// <returns>
    /// Participant command representing the next tactical decision:
    /// - BeginEngagement: Start combat with selected target
    /// - LockBattleAttack: Commit to specific attack action
    /// - LockBattleDefense: Commit to specific defense action (or No Defense)
    /// - ContinueEngagement: Continue after resolution display
    /// - CancelEngagement: Abort engagement if no valid attack available
    /// - EndBattle: Complete Battle phase when no valid targets remain
    /// - null: No action needed at this moment (waiting for game state update)
    /// </returns>
    public static GameParticipantAPI.ParticipantCommand DecideBattle(
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        BattleContext context)
    {
        // Get Battle view
        GameBattle.ParticipantView battleView = snapshot.Battle;
        if (battleView == null || !battleView.IsBattlePhaseActive)
        {
            return null;
        }

        // Check if engagement is active
        if (battleView.EngagementActive && battleView.Engagement != null)
        {
            return HandleEngagement(battleView.Engagement, snapshot, access, context);
        }

        // No engagement active - initiate new engagement or end Battle
        return HandleOverworldState(battleView, snapshot, access, context);
    }

    /// <summary>
    /// Handles active engagement state (attack selection, defense selection, resolution).
    /// Task 10.1 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand HandleEngagement(
        GameBattle.EngagementView engagement,
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        BattleContext context)
    {
        // Check if engagement is resolved (waiting for resolution display)
        if (engagement.Resolved)
        {
            return HandleResolution(engagement, ref context);
        }

        // Engagement is active - determine if we need to select attack or defense
        if (engagement.IsAttacker && !engagement.AttackerLocked)
        {
            // Our turn to select attack
            // Find attacker unit
            ParticipantUnitSnapshot attacker = null;
            for (int i = 0; i < snapshot.OwnUnits.Length; i++)
            {
                if (snapshot.OwnUnits[i].RuntimeUnitID == engagement.AttackerRuntimeUnitID)
                {
                    attacker = snapshot.OwnUnits[i];
                    break;
                }
            }
            return SelectAttack(engagement, snapshot, attacker);
        }
        else if (!engagement.IsAttacker && !engagement.DefenderLocked && engagement.AttackerLocked)
        {
            // Our turn to select defense
            return SelectDefense(engagement, snapshot, DefaultUseDefenseChance, DefaultUseAllySupportChance);
        }

        // Waiting for other participant
        return null;
    }

    /// <summary>
    /// Handles overworld state (no active engagement) - discovers targets and begins new engagement.
    /// Task 10.2 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand HandleOverworldState(
        GameBattle.ParticipantView battleView,
        ParticipantGameSnapshot snapshot,
        GameParticipantAPI.ParticipantAccess access,
        BattleContext context)
    {
        // Find units that haven't attacked yet
        foreach (GameBattle.UnitView unitView in battleView.Units)
        {
            if (unitView.HasAttacked)
                continue;

            // Find the unit in our snapshot
            // Find unit in snapshot
            ParticipantUnitSnapshot attacker = null;
            for (int i = 0; i < snapshot.OwnUnits.Length; i++)
            {
                if (snapshot.OwnUnits[i].RuntimeUnitID == unitView.RuntimeUnitID)
                {
                    attacker = snapshot.OwnUnits[i];
                    break;
                }
            }
            if (attacker == null)
                continue;

            // Check if unit is operational
            if (attacker == null || !attacker.IsOnMap || attacker.LifeState != RuntimeUnitLifeState.Active)
                continue;

            // Discover and score targets
            List<TargetEvaluation> targets = DiscoverAndScoreTargets(attacker, context);

            // If we have valid targets, begin engagement with the best one
            if (targets.Count > 0)
            {
                TargetEvaluation bestTarget = targets[0];  // Already sorted by score descending
                return BeginEngagement(attacker, bestTarget, access, context);
            }
        }

        // No units left to attack or no valid targets - end Battle phase
        return GameParticipantAPI.ParticipantCommand.EndBattle(snapshot);
    }

    /// <summary>
    /// Discovers all valid enemy targets and scores them for engagement priority.
    /// Returns sorted list (highest score first).
    /// Task 6.2 implementation.
    /// </summary>
    private static List<TargetEvaluation> DiscoverAndScoreTargets(
        ParticipantUnitSnapshot attacker,
        BattleContext context)
    {
        List<TargetEvaluation> evaluations = new List<TargetEvaluation>();

        // Collect all known enemy coordinates from contacts and visible enemies
        HashSet<Vector2Int> knownEnemyCoordinates = new HashSet<Vector2Int>();

        // Add contact coordinates
        for (int i = 0; i < context.Snapshot.Contacts.Length; i++)
        {
            knownEnemyCoordinates.Add(context.Snapshot.Contacts[i].Coordinate);
        }

        // Add visible enemy coordinates
        for (int i = 0; i < context.Snapshot.VisibleEnemies.Length; i++)
        {
            // Add all coordinates occupied by visible enemy
            for (int j = 0; j < context.Snapshot.VisibleEnemies[i].OccupiedCoordinates.Length; j++)
            {
                knownEnemyCoordinates.Add(context.Snapshot.VisibleEnemies[i].OccupiedCoordinates[j]);
            }
        }

        // Score each known enemy coordinate
        foreach (Vector2Int coordinate in knownEnemyCoordinates)
        {
            // Check if this coordinate is reachable by attacker's fire control
            // For now, we'll assume all known enemies are within range
            // The actual TryBeginEngagement will validate this

            // Score the target
            TargetEvaluation evaluation = TargetScorer.ScoreTarget(coordinate, attacker, context);
            evaluations.Add(evaluation);
        }

        // Sort by total score descending (highest score first)
        evaluations.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

        return evaluations;
    }

    /// <summary>
    /// Begins engagement with the selected target.
    /// Task 6.3 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand BeginEngagement(
        ParticipantUnitSnapshot attacker,
        TargetEvaluation target,
        GameParticipantAPI.ParticipantAccess access,
        BattleContext context)
    {
        // Track this target as engaged for coordination
        context.EngagedTargetsThisPhase.Add(target.Coordinate);

        // Begin engagement using ParticipantAccess
        return GameParticipantAPI.ParticipantCommand.BeginEngagement(
            context.Snapshot,
            attacker.RuntimeUnitID,
            target.Coordinate
        );
    }

    /// <summary>
    /// Selects the best attack option from available weapons/abilities.
    /// Task 7.1 and 7.2 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand SelectAttack(
        GameBattle.EngagementView engagement,
        ParticipantGameSnapshot snapshot,
        ParticipantUnitSnapshot attacker)
    {
        // Check if we have attack options available
        if (engagement.AttackerOptions == null || engagement.AttackerOptions.Length == 0)
        {
            // No valid attack options - cancel engagement
            return GameParticipantAPI.ParticipantCommand.CancelEngagement(snapshot);
        }

        // Create target view for scoring
        // Score all attack options
        AttackEvaluation bestAttack = default;
        float bestScore = float.MinValue;

        foreach (GameBattle.ActionOption option in engagement.AttackerOptions)
        {
            AttackEvaluation evaluation = AttackScorer.ScoreAttackOption(option, null, attacker);

            if (evaluation.TotalScore > bestScore)
            {
                bestScore = evaluation.TotalScore;
                bestAttack = evaluation;
            }
        }

        // Lock the best attack
        return GameParticipantAPI.ParticipantCommand.LockBattleAttack(
            snapshot,
            bestAttack.ActionKind,
            bestAttack.SlotIndex
        );
    }

    /// <summary>
    /// Selects the best defense option (or chooses no defense).
    /// Task 8.1 and 8.2 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand SelectDefense(
        GameBattle.EngagementView engagement,
        ParticipantGameSnapshot snapshot,
        float useDefenseChance,
        float useAllySupportChance)
    {
        // Random roll to decide if we should defend at all
        float defendRoll = Random.Range(0f, 1f);
        if (defendRoll > useDefenseChance)
        {
            // Choose not to defend
            return GameParticipantAPI.ParticipantCommand.LockBattleDefense(
                snapshot,
                GameBattle.CombatActionKind.None,
                -1,
                string.Empty,
                GameBattle.CombatActionKind.None,
                -1
            );
        }

        // We're defending - find the defender unit
        // Find defender unit
        ParticipantUnitSnapshot defender = null;
        for (int i = 0; i < snapshot.OwnUnits.Length; i++)
        {
            if (snapshot.OwnUnits[i].RuntimeUnitID == engagement.DefenderRuntimeUnitID)
            {
                defender = snapshot.OwnUnits[i];
                break;
            }
        }
        string incomingAttackName = engagement.SelectedAttackName;

        // Score own defense options
        DefenseEvaluation bestOwnDefense = default;
        float bestOwnScore = float.MinValue;
        bool hasOwnDefense = false;

        if (engagement.DefenderOptions != null && engagement.DefenderOptions.Length > 0)
        {
            foreach (GameBattle.ActionOption option in engagement.DefenderOptions)
            {
                DefenseEvaluation evaluation = DefenseScorer.ScoreDefenseOption(
                    option, defender, incomingAttackName, false, string.Empty
                );

                if (evaluation.TotalScore > bestOwnScore)
                {
                    bestOwnScore = evaluation.TotalScore;
                    bestOwnDefense = evaluation;
                    hasOwnDefense = true;
                }
            }
        }

        // Score allied support options
        DefenseEvaluation bestAllyDefense = default;
        float bestAllyScore = float.MinValue;
        bool hasAllySupport = false;

        if (engagement.AllySupport != null && engagement.AllySupport.Length > 0)
        {
            foreach (GameBattle.AllySupportGroup supportGroup in engagement.AllySupport)
            {
                if (supportGroup.Options == null)
                    continue;

                foreach (GameBattle.ActionOption option in supportGroup.Options)
                {
                    DefenseEvaluation evaluation = DefenseScorer.ScoreDefenseOption(
                        option, defender, incomingAttackName, true, supportGroup.RuntimeUnitID
                    );

                    if (evaluation.TotalScore > bestAllyScore)
                    {
                        bestAllyScore = evaluation.TotalScore;
                        bestAllyDefense = evaluation;
                        hasAllySupport = true;
                    }
                }
            }
        }

        // Decide between own defense and ally support
        if (!hasOwnDefense && !hasAllySupport)
        {
            // No defense options available
            return GameParticipantAPI.ParticipantCommand.LockBattleDefense(
                snapshot,
                GameBattle.CombatActionKind.None,
                -1,
                string.Empty,
                GameBattle.CombatActionKind.None,
                -1
            );
        }

        // Choose the best defense option
        DefenseEvaluation selectedDefense;
        if (hasOwnDefense && (!hasAllySupport || bestOwnScore >= bestAllyScore))
        {
            selectedDefense = bestOwnDefense;
        }
        else
        {
            selectedDefense = bestAllyDefense;
        }

        // Lock the selected defense
        return GameParticipantAPI.ParticipantCommand.LockBattleDefense(
            snapshot,
            selectedDefense.ActionKind,
            selectedDefense.SlotIndex,
            selectedDefense.IsAlliedSupport ? selectedDefense.SupportUnitID : string.Empty,
            selectedDefense.IsAlliedSupport ? selectedDefense.ActionKind : GameBattle.CombatActionKind.None,
            selectedDefense.IsAlliedSupport ? selectedDefense.SlotIndex : -1
        );
    }

    /// <summary>
    /// Handles engagement resolution state (waiting, then continuing).
    /// Task 9.1 and 9.2 implementation.
    /// </summary>
    private static GameParticipantAPI.ParticipantCommand HandleResolution(
        GameBattle.EngagementView engagement,
        ref BattleContext context)
    {
        // Implementation note: Since BattleContext is a struct and we can't modify fields directly,
        // and we need to track resolution wait state, we'll use a simple time-based approach.
        // In a real implementation, you might want to add wait tracking to BattleContext.

        // For now, simply wait a moment before continuing
        // The actual wait timing will be handled by the AI controller's update loop

        if (engagement.IsAttacker)
        {
            // As attacker, we need to submit Continue command
            return GameParticipantAPI.ParticipantCommand.ContinueEngagement(context.Snapshot);
        }

        // As defender, we just wait for attacker to continue
        return null;
    }
}
