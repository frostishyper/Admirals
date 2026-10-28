using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores target evaluation data used for prioritizing enemy units during Battle phase.
/// Combines threat assessment, fragility analysis, distance scoring, and fleet coordination
/// penalties to determine optimal engagement targets.
/// </summary>
public struct TargetEvaluation
{
    /// <summary>
    /// Map coordinate of the potential target.
    /// </summary>
    public Vector2Int Coordinate;

    /// <summary>
    /// Runtime identifier of the target unit (empty if contact-only target without absolute visibility).
    /// </summary>
    public string RuntimeUnitID;

    /// <summary>
    /// Combat domain of the target (Surface, Subsurface, Air).
    /// </summary>
    public Domain TargetDomain;

    /// <summary>
    /// Threat score (0-100) based on enemy weapons, unit type, and domain advantage.
    /// Higher values indicate more dangerous targets.
    /// </summary>
    public float ThreatScore;

    /// <summary>
    /// Fragility score (0-100) based on remaining health percentage.
    /// Higher values indicate targets closer to destruction.
    /// </summary>
    public float FragilityScore;

    /// <summary>
    /// Distance score (0-50) based on Manhattan distance from attacker.
    /// Higher values indicate closer targets.
    /// </summary>
    public float DistanceScore;

    /// <summary>
    /// Coordination penalty (-40 to 0) discouraging multiple units from targeting the same enemy.
    /// Applied based on how many times this coordinate was engaged this Battle phase.
    /// </summary>
    public float CoordinationPenalty;

    /// <summary>
    /// Combined total score used for target prioritization.
    /// Calculated from weighted combination of all component scores.
    /// </summary>
    public float TotalScore;

    /// <summary>
    /// Whether the target has absolute visibility (not just contact-based detection).
    /// Absolutely visible targets receive a bonus when scores are similar.
    /// </summary>
    public bool IsAbsolutelyVisible;
}

/// <summary>
/// Stores attack action evaluation for weapon and offensive ability selection.
/// Scores attack options based on damage potential, reliability, special properties,
/// and ammunition economy to select optimal offensive actions.
/// </summary>
public struct AttackEvaluation
{
    /// <summary>
    /// Type of combat action (Weapon, Ability).
    /// </summary>
    public GameBattle.CombatActionKind ActionKind;

    /// <summary>
    /// Zero-based slot index of the action in the unit's loadout.
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// Display name of the weapon or ability.
    /// </summary>
    public string ActionName;

    /// <summary>
    /// Expected damage score (0-100) based on weapon damage value or ability offensive power.
    /// </summary>
    public float ExpectedDamageScore;

    /// <summary>
    /// Reliability score (0-50) indicating outcome consistency.
    /// Flat outcomes score higher than D20-based rolls.
    /// </summary>
    public float ReliabilityScore;

    /// <summary>
    /// Property bonus score (0-40) for special attack properties.
    /// BypassDefense, GuaranteedHit, and Critical properties increase this value.
    /// </summary>
    public float PropertyBonusScore;

    /// <summary>
    /// Charge economy score (0-30) encouraging preservation of limited-use weapons.
    /// Higher charge counts and abilities (no charges) score higher.
    /// </summary>
    public float ChargeEconomyScore;

    /// <summary>
    /// Combined total score used for attack option prioritization.
    /// </summary>
    public float TotalScore;

    /// <summary>
    /// Whether this action uses D20 roll mechanics (affects reliability scoring).
    /// </summary>
    public bool UsesD20;
}

/// <summary>
/// Stores defensive action evaluation for system and allied support selection.
/// Scores defensive options based on negation capability, damage mitigation,
/// property stripping, and reliability to select optimal defensive responses.
/// </summary>
public struct DefenseEvaluation
{
    /// <summary>
    /// Type of combat action (System for defensive systems).
    /// </summary>
    public GameBattle.CombatActionKind ActionKind;

    /// <summary>
    /// Zero-based slot index of the defensive system.
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// Display name of the defensive system.
    /// </summary>
    public string ActionName;

    /// <summary>
    /// Runtime identifier of the supporting unit (empty if using own defense).
    /// </summary>
    public string SupportUnitID;

    /// <summary>
    /// Whether this defense comes from an allied unit's support pattern.
    /// </summary>
    public bool IsAlliedSupport;

    /// <summary>
    /// Negation score (0-100) indicating ability to completely negate the attack.
    /// Full negation: 100, partial negation: 50, no negation: 0.
    /// </summary>
    public float NegationScore;

    /// <summary>
    /// Mitigation score (0-60) based on damage reduction value.
    /// Higher damage mitigation produces higher scores.
    /// </summary>
    public float MitigationScore;

    /// <summary>
    /// Property strip score (0-50) for removing dangerous attack properties.
    /// Stripping BypassDefense or GuaranteedHit increases this value.
    /// </summary>
    public float PropertyStripScore;

    /// <summary>
    /// Reliability score (0-40) indicating defensive outcome consistency.
    /// Flat outcomes score higher than D20-based rolls.
    /// </summary>
    public float ReliabilityScore;

    /// <summary>
    /// Combined total score used for defense option prioritization.
    /// Allied support options receive a -10% penalty.
    /// </summary>
    public float TotalScore;

    /// <summary>
    /// Whether this defense uses D20 roll mechanics (affects reliability scoring).
    /// </summary>
    public bool UsesD20;
}

/// <summary>
/// Maintains context and state tracking for Battle phase decision-making.
/// Provides access to game state snapshot, participant API, and fleet coordination tracking
/// to support target selection and engagement orchestration.
/// </summary>
public struct BattleContext
{
    /// <summary>
    /// Current game state snapshot from the participant's perspective.
    /// Contains own units, visible enemies, contacts, and phase-specific views.
    /// </summary>
    public ParticipantGameSnapshot Snapshot;

    /// <summary>
    /// Participant API access for submitting commands and querying additional game state.
    /// </summary>
    public GameParticipantAPI.ParticipantAccess Access;

    /// <summary>
    /// Set of target coordinates that have been engaged this Battle phase.
    /// Used to apply coordination penalties and prevent excessive focus fire.
    /// Cleared at the start of each Battle phase.
    /// </summary>
    public HashSet<Vector2Int> EngagedTargetsThisPhase;

    /// <summary>
    /// Initializes a new BattleContext with the specified snapshot and access.
    /// Engaged targets tracking is initialized to an empty set.
    /// </summary>
    /// <param name="snapshot">Current game state snapshot.</param>
    /// <param name="access">Participant API access.</param>
    public BattleContext(ParticipantGameSnapshot snapshot, GameParticipantAPI.ParticipantAccess access)
    {
        Snapshot = snapshot;
        Access = access;
        EngagedTargetsThisPhase = new HashSet<Vector2Int>();
    }
}
