using UnityEngine;

/// <summary>
/// Evaluates defensive action options during Battle phase engagements.
/// Scores defensive systems and allied support based on negation capability,
/// damage mitigation, property stripping effectiveness, and reliability.
///
/// Scoring Algorithm:
/// - Negation (40%): Ability to completely or partially negate the incoming attack
/// - Mitigation (25%): Damage reduction value of the defensive system
/// - Property Strip (25%): Capability to strip dangerous attack properties (BypassDefense, GuaranteedHit)
/// - Reliability (10%): Consistency of outcome (flat vs D20-based)
/// - Allied Support Penalty: -10% to total score for support defenses (prefer own defenses)
/// </summary>
public static class DefenseScorer
{
    // Scoring weight constants for defensive evaluation
    private const float NegationWeight = 0.40f;
    private const float MitigationWeight = 0.25f;
    private const float PropertyStripWeight = 0.25f;
    private const float ReliabilityWeight = 0.10f;
    private const float AlliedSupportPenalty = 0.10f;

    /// <summary>
    /// Evaluates a defensive action option and returns a scored evaluation.
    /// </summary>
    /// <param name="option">The defensive action option to evaluate (defensive system or allied support).</param>
    /// <param name="defenderUnit">The unit snapshot owning this defensive system (for accessing system definitions).</param>
    /// <param name="incomingAttackName">Name of the incoming attack to defend against.</param>
    /// <param name="isAlliedSupport">Whether this defense comes from an allied unit's support pattern.</param>
    /// <param name="supportUnitID">Runtime identifier of the supporting unit (empty if using own defense).</param>
    /// <returns>DefenseEvaluation struct containing all scoring details and total score.</returns>
    public static DefenseEvaluation ScoreDefenseOption(
        GameBattle.ActionOption option,
        ParticipantUnitSnapshot defenderUnit,
        string incomingAttackName,
        bool isAlliedSupport,
        string supportUnitID = "")
    {
        DefenseEvaluation evaluation = new DefenseEvaluation
        {
            ActionKind = option.Kind,
            SlotIndex = option.SlotIndex,
            ActionName = option.Name,
            SupportUnitID = supportUnitID,
            IsAlliedSupport = isAlliedSupport,
            UsesD20 = option.UsesD20,

            // Calculate individual scores using helper methods
            NegationScore = GetNegationScore(option, incomingAttackName),
            MitigationScore = GetMitigationScore(option),
            PropertyStripScore = GetPropertyStripScore(option, defenderUnit, incomingAttackName),
            ReliabilityScore = GetReliabilityScore(option)
        };

        // Calculate weighted total score
        // Formula: (Negation × 0.40) + (Mitigation × 0.25) + (PropertyStrip × 0.25) + (Reliability × 0.10)
        float totalScore =
            (evaluation.NegationScore * NegationWeight) +
            (evaluation.MitigationScore * MitigationWeight) +
            (evaluation.PropertyStripScore * PropertyStripWeight) +
            (evaluation.ReliabilityScore * ReliabilityWeight);

        // Apply -10% penalty for allied support defenses (slight preference for own defenses)
        if (isAlliedSupport)
        {
            totalScore *= (1f - AlliedSupportPenalty);
        }

        evaluation.TotalScore = totalScore;

        return evaluation;
    }

    /// <summary>
    /// Calculates the negation score based on the defense's ability to negate the incoming attack.
    /// Full negation: 100 points, Partial negation: 50 points, No negation: 0 points.
    /// </summary>
    /// <param name="option">The defensive action option.</param>
    /// <param name="incomingAttackName">Name of the incoming attack.</param>
    /// <returns>Negation score (0-100).</returns>
    /// <remarks>
    /// Task 4.2 implementation. Since ActionOption doesn't expose defense outcome properties,
    /// we use name-based heuristics to estimate negation capability.
    /// - "Negate"/"Counter"/"Nullify" in name: Full negation (100 points)
    /// - "Deflect"/"Parry"/"Reduce" in name: Partial negation (50 points)
    /// - D20-based defenses without negation keywords: Partial (50 points, variable outcomes)
    /// - Flat defenses without keywords: Moderate (30 points)
    /// </remarks>
    private static float GetNegationScore(GameBattle.ActionOption option, string incomingAttackName)
    {
        if (option == null || option.Name == null)
            return 0f;

        string nameLower = option.Name.ToLower();

        // Full negation keywords (100 points)
        if (nameLower.Contains("negate") || nameLower.Contains("counter") ||
            nameLower.Contains("nullify") || nameLower.Contains("block"))
        {
            return 100f;
        }

        // Partial negation keywords (50 points)
        if (nameLower.Contains("deflect") || nameLower.Contains("parry") ||
            nameLower.Contains("reduce") || nameLower.Contains("intercept"))
        {
            return 50f;
        }

        // D20-based defenses tend to have variable negation potential (50 points)
        if (option.UsesD20)
        {
            return 50f;
        }

        // Flat defenses without specific keywords - moderate mitigation (30 points)
        return 30f;
    }

    /// <summary>
    /// Calculates the mitigation score based on the defense's damage reduction value.
    /// High mitigation (>10): 60 points, Medium (5-10): 40 points, Low (<5): 20 points.
    /// </summary>
    /// <param name="option">The defensive action option.</param>
    /// <returns>Mitigation score (0-60).</returns>
    /// <remarks>
    /// Task 4.3 implementation. Since ActionOption doesn't expose damage reduction values,
    /// we use name-based heuristics and defense type to estimate mitigation:
    /// - "Heavy"/"Reinforced"/"Armored" defenses: High mitigation (60 points)
    /// - "Standard"/"Medium" defenses: Medium mitigation (40 points)
    /// - "Light"/"Emergency" defenses: Low mitigation (20 points)
    /// - D20-based defenses: Assume medium mitigation (40 points)
    /// </remarks>
    private static float GetMitigationScore(GameBattle.ActionOption option)
    {
        if (option == null || option.Name == null)
            return 0f;

        string nameLower = option.Name.ToLower();

        // High mitigation keywords (60 points)
        if (nameLower.Contains("heavy") || nameLower.Contains("reinforced") ||
            nameLower.Contains("armored") || nameLower.Contains("fortress"))
        {
            return 60f;
        }

        // Low mitigation keywords (20 points)
        if (nameLower.Contains("light") || nameLower.Contains("emergency") ||
            nameLower.Contains("basic") || nameLower.Contains("minimal"))
        {
            return 20f;
        }

        // D20-based defenses or medium keywords - assume medium mitigation (40 points)
        if (option.UsesD20 || nameLower.Contains("standard") ||
            nameLower.Contains("medium") || nameLower.Contains("moderate"))
        {
            return 40f;
        }

        // Default - assume medium mitigation
        return 40f;
    }

    /// <summary>
    /// Calculates the property strip score based on the defense's ability to strip attack properties.
    /// Strips BypassDefense: +30, Strips GuaranteedHit: +20.
    /// </summary>
    /// <param name="option">The defensive action option.</param>
    /// <param name="defenderUnit">The unit snapshot for accessing system definitions.</param>
    /// <param name="incomingAttackName">Name of the incoming attack.</param>
    /// <returns>Property strip score (0-50).</returns>
    /// <remarks>
    /// Task 4.4 implementation. Checks system definitions for strip properties.
    /// Falls back to name heuristics if system definition is unavailable.
    /// </remarks>
    private static float GetPropertyStripScore(GameBattle.ActionOption option, ParticipantUnitSnapshot defenderUnit, string incomingAttackName)
    {
        // Validate inputs
        if (option == null || defenderUnit == null)
        {
            return 0f;
        }

        // Get the system definition from the defender's systems
        // The option.SlotIndex corresponds to the system slot index
        if (option.SlotIndex < 0 || option.SlotIndex >= defenderUnit.Systems.Length)
        {
            return 0f;
        }

        ParticipantSystemSnapshot systemSnapshot = defenderUnit.Systems[option.SlotIndex];
        if (systemSnapshot == null || systemSnapshot.Definition == null)
        {
            return 0f;
        }

        Definition_System systemDef = systemSnapshot.Definition;

        // Check if this is a defensive system
        if (systemDef.Defense == null || !systemDef.Defense.Enabled)
        {
            return 0f;
        }

        // Get the defense outcome to check strip properties
        // For simplicity, we'll check the flat outcome if it exists,
        // or sample the D20 outcomes if it's a D20-based defense
        DefenseOutcome outcome = null;

        if (!systemDef.Defense.UsesD20Roll)
        {
            // Flat outcome
            outcome = systemDef.Defense.DefenseOutcome;
        }
        else
        {
            // D20-based: check if ANY outcome can strip properties
            // This is a heuristic - we could sample the "most likely" outcome,
            // but for scoring purposes, we'll check if stripping is possible at all
            DefenseD20Outcome[] d20Outcomes = systemDef.Defense.D20Outcomes;
            if (d20Outcomes != null && d20Outcomes.Length > 0)
            {
                // Check the middle/average outcome (around roll 10-11)
                // This gives us a representative outcome for scoring
                foreach (DefenseD20Outcome d20Outcome in d20Outcomes)
                {
                    if (d20Outcome != null && d20Outcome.Outcome != null)
                    {
                        // Use the first valid outcome that covers the middle range
                        // In D20 systems, this is typically around 10-11
                        if (d20Outcome.MinimumRoll <= 11 && d20Outcome.MaximumRoll >= 10)
                        {
                            outcome = d20Outcome.Outcome;
                            break;
                        }
                    }
                }

                // If no middle outcome found, use the first available outcome
                if (outcome == null)
                {
                    foreach (DefenseD20Outcome d20Outcome in d20Outcomes)
                    {
                        if (d20Outcome != null && d20Outcome.Outcome != null)
                        {
                            outcome = d20Outcome.Outcome;
                            break;
                        }
                    }
                }
            }
        }

        if (outcome == null)
        {
            return 0f;
        }

        // Calculate score based on strip properties
        float score = 0f;

        if (outcome.StripBypassDefense)
        {
            score += 30f; // BypassDefense is very dangerous, high value to strip it
        }

        if (outcome.StripGuaranteedHit)
        {
            score += 20f; // GuaranteedHit is dangerous, good value to strip it
        }

        return score;
    }

    /// <summary>
    /// Calculates the reliability score based on the defense's outcome consistency.
    /// Flat outcomes: 40 points, D20 rolls: 25 points.
    /// </summary>
    /// <param name="option">The defensive action option.</param>
    /// <returns>Reliability score (0-40).</returns>
    /// <remarks>
    /// Task 4.5 implementation. Flat outcomes are more predictable and reliable
    /// for defensive planning than D20-based outcomes with variable results.
    /// </remarks>
    private static float GetReliabilityScore(GameBattle.ActionOption option)
    {
        if (option == null)
            return 0f;

        // Flat outcomes are highly reliable
        if (!option.UsesD20)
        {
            return 40f;
        }

        // D20-based outcomes have variable reliability
        return 25f;
    }
}