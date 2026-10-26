using UnityEngine;

/// <summary>
/// Provides scoring algorithms for attack action selection during Battle phase engagements.
/// Evaluates weapons and offensive abilities based on damage potential, reliability,
/// special properties, and ammunition economy to determine optimal offensive choices.
/// </summary>
public static class AttackScorer
{
    // Scoring weight constants for weighted total calculation
    private const float DamageWeight = 0.45f;
    private const float ReliabilityWeight = 0.25f;
    private const float PropertyWeight = 0.20f;
    private const float ChargeWeight = 0.10f;

    /// <summary>
    /// Scores an attack action option for target engagement.
    /// Evaluates the option based on expected damage, outcome reliability,
    /// special attack properties, and charge economy.
    /// </summary>
    /// <param name="option">The attack action option (weapon or offensive ability) to evaluate.</param>
    /// <param name="target">The target view containing target coordinate and domain information.</param>
    /// <param name="attacker">Snapshot of the attacking unit for context.</param>
    /// <returns>AttackEvaluation struct with component scores and weighted total score.</returns>
    /// <remarks>
    /// Scoring Algorithm:
    /// - Expected Damage Score (0-100): Weapon damage value scaled to 0-100, or fixed +50 for offensive abilities
    /// - Reliability Score (0-65): Flat outcomes = 50, D20 rolls = 35, +15 for GuaranteedHit (heuristic)
    /// - Property Bonus Score (0-45): Heuristic bonuses for special properties
    /// - Charge Economy Score (0-30): >3 charges = 30, 2-3 charges = 20, 1 charge = 10, abilities = 30
    ///
    /// Total Score = (ExpectedDamage × 0.45) + (Reliability × 0.25) + (PropertyBonus × 0.20) + (ChargeEconomy × 0.10)
    /// </remarks>
    public static AttackEvaluation ScoreAttackOption(
        GameBattle.ActionOption option,
        GameBattle.TargetView target,
        ParticipantUnitSnapshot attacker)
    {
        AttackEvaluation evaluation = new AttackEvaluation
        {
            ActionKind = option.Kind,
            SlotIndex = option.SlotIndex,
            ActionName = option.Name,
            UsesD20 = option.UsesD20,

            // Scoring components
            ExpectedDamageScore = GetWeaponDamage(option),
            ReliabilityScore = GetReliabilityScore(option),
            PropertyBonusScore = GetPropertyBonusScore(option),
            ChargeEconomyScore = GetChargeEconomyScore(option)
        };

        // Calculate weighted total score
        evaluation.TotalScore =
            (evaluation.ExpectedDamageScore * DamageWeight) +
            (evaluation.ReliabilityScore * ReliabilityWeight) +
            (evaluation.PropertyBonusScore * PropertyWeight) +
            (evaluation.ChargeEconomyScore * ChargeWeight);

        return evaluation;
    }

    /// <summary>
    /// Extracts weapon damage value from an action option and scales to 0-100 range.
    /// Uses heuristics since direct damage values are not available from ActionOption.
    /// </summary>
    /// <param name="option">The action option to evaluate.</param>
    /// <returns>
    /// Damage score scaled to 0-100 range:
    /// - Ability actions: 50 (offensive abilities baseline)
    /// - D20-based weapons: 60 (moderate-high damage assumed)
    /// - Flat damage weapons with charges: 50 (moderate damage assumed)
    /// - Other weapons: 50 (default moderate)
    /// </returns>
    /// <remarks>
    /// Since ActionOption doesn't expose direct damage values, we use tactical heuristics:
    /// D20-based attacks typically have higher damage potential, flat attacks are more consistent.
    /// This is realistic for AI making decisions with limited information about enemy capabilities.
    /// </remarks>
    private static float GetWeaponDamage(GameBattle.ActionOption option)
    {
        // Abilities have a baseline offensive value
        if (option.Kind == GameBattle.CombatActionKind.Ability)
        {
            return 50f;
        }

        // D20-based weapons tend to have higher damage potential
        if (option.UsesD20)
        {
            return 60f;
        }

        // Flat damage weapons or weapons with charges
        if (option.MaxCharges > 0)
        {
            return 50f;
        }

        // Default for other weapon types
        return 50f;
    }

    /// <summary>
    /// Calculates reliability score based on outcome type.
    /// Flat outcomes are more reliable than D20-based attacks.
    /// </summary>
    /// <param name="option">The action option to evaluate.</param>
    /// <returns>
    /// Reliability score:
    /// - Flat outcomes: 50 points (highly reliable)
    /// - D20-based outcomes: 35 points (variable outcomes)
    /// - Name-based heuristic bonus: +15 for "Accurate" or "Precise" in name (simulates GuaranteedHit)
    /// </returns>
    /// <remarks>
    /// Task 3.3 implementation. Since ActionOption doesn't expose attack properties like
    /// GuaranteedHit, we use outcome type (UsesD20) and name-based heuristics as proxies.
    /// Flat outcomes are inherently more reliable for tactical planning.
    /// </remarks>
    private static float GetReliabilityScore(GameBattle.ActionOption option)
    {
        float score;

        // Base reliability score
        if (!option.UsesD20)
        {
            // Flat outcome = high reliability
            score = 50f;
        }
        else
        {
            // D20-based outcome = moderate reliability (variable results)
            score = 35f;
        }

        // Heuristic bonus for names suggesting accuracy/guaranteed hits
        // This simulates the GuaranteedHit property we can't directly access
        if (option.Name != null)
        {
            string nameLower = option.Name.ToLower();
            if (nameLower.Contains("accurate") || nameLower.Contains("precise") ||
                nameLower.Contains("guided") || nameLower.Contains("homing"))
            {
                score += 15f;
            }
        }

        return score;
    }

    /// <summary>
    /// Calculates property bonus score for special attack properties.
    /// Uses heuristic proxies since ActionOption doesn't expose attack properties directly.
    /// </summary>
    /// <param name="option">The action option to evaluate.</param>
    /// <returns>
    /// Property bonus score based on name heuristics:
    /// - "Armor Piercing"/"Bypass"/"Penetrating": +20 (simulates BypassDefense)
    /// - "Accurate"/"Guided"/"Homing": +15 (simulates GuaranteedHit)
    /// - "Critical"/"Devastating"/"Heavy": +10 (simulates Critical property)
    /// </returns>
    /// <remarks>
    /// Task 3.4 implementation. ActionOption doesn't expose properties like BypassDefense,
    /// GuaranteedHit, or Critical. We use weapon name heuristics as tactical proxies.
    /// This simulates the AI inferring weapon capabilities from naming conventions.
    /// </remarks>
    private static float GetPropertyBonusScore(GameBattle.ActionOption option)
    {
        float score = 0f;

        if (option.Name == null)
            return score;

        string nameLower = option.Name.ToLower();

        // Bypass Defense property heuristic (+20)
        // Armor-piercing weapons typically bypass defensive systems
        if (nameLower.Contains("armor piercing") || nameLower.Contains("bypass") ||
            nameLower.Contains("penetrating") || nameLower.Contains("piercing"))
        {
            score += 20f;
        }

        // GuaranteedHit property heuristic (+15)
        // Guided/homing weapons have high hit probability
        if (nameLower.Contains("accurate") || nameLower.Contains("guided") ||
            nameLower.Contains("homing") || nameLower.Contains("precise"))
        {
            score += 15f;
        }

        // Critical property heuristic (+10)
        // Heavy/devastating weapons typically have critical hit potential
        if (nameLower.Contains("critical") || nameLower.Contains("devastating") ||
            nameLower.Contains("heavy") || nameLower.Contains("massive"))
        {
            score += 10f;
        }

        return score;
    }

    /// <summary>
    /// Calculates charge economy score based on remaining ammunition.
    /// Encourages using weapons with more charges available.
    /// </summary>
    /// <param name="option">The action option to evaluate.</param>
    /// <returns>
    /// Charge economy score:
    /// - Abilities (no charges): 30 points (unlimited use)
    /// - >3 charges remaining: 30 points (plentiful)
    /// - 2-3 charges: 20 points (moderate)
    /// - 1 charge: 10 points (scarce - save for critical moments)
    /// - 0 charges: 0 points (depleted)
    /// </returns>
    /// <remarks>
    /// Task 3.5 implementation. Encourages the AI to prefer weapons with more ammunition
    /// available, while still allowing use of scarce resources when tactically sound.
    /// Abilities without charge limits score highest for sustainability.
    /// </remarks>
    private static float GetChargeEconomyScore(GameBattle.ActionOption option)
    {
        // Abilities have no charge limit - score highest
        if (option.Kind == GameBattle.CombatActionKind.Ability || option.MaxCharges == 0)
        {
            return 30f;
        }

        // Score based on remaining charges
        int remaining = option.CurrentCharges;

        if (remaining > 3)
            return 30f;  // Plentiful ammunition
        else if (remaining >= 2)
            return 20f;  // Moderate ammunition
        else if (remaining == 1)
            return 10f;  // Scarce - use carefully
        else
            return 0f;   // Depleted
    }
}
