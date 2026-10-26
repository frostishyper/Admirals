/// <summary>
/// High-level strategic posture that guides fleet tactical behavior.
/// Stance selection is based on material score, fleet health, and contact visibility.
/// </summary>
public enum FleetStance
{
    /// <summary>
    /// Exploration mode when no enemy contacts are visible.
    /// Fleet spreads out to discover enemy positions.
    /// Selected when: contacts.Count == 0
    /// </summary>
    Hunt,

    /// <summary>
    /// Aggressive advance when holding a strong position.
    /// Fleet presses the attack with coordinated movement.
    /// Selected when: MaterialScore >= 0.90
    /// </summary>
    Advance,

    /// <summary>
    /// Balanced defensive posture with maintained formation.
    /// Fleet holds position and waits for enemy approach.
    /// Selected when: 0.60 <= MaterialScore < 0.90
    /// </summary>
    Hold,

    /// <summary>
    /// Defensive withdrawal while maintaining cohesion.
    /// Fleet pulls back toward home position and stays compact.
    /// Selected when: MaterialScore < 0.60
    /// </summary>
    DefensiveFallback,

    /// <summary>
    /// Emergency disengagement due to critical damage or overwhelming odds.
    /// Prioritizes survival over tactical objectives.
    /// Selected when: MaterialScore < 0.35 OR FleetHpFraction < 0.30
    /// </summary>
    Retreat
}/// <summary>
/// AI formation mode determining ship cohesion behavior.
/// Selected randomly at match start to provide gameplay variety.
/// </summary>
public enum FormationMode
{
    /// <summary>
    /// Ships operate independently with minimal cohesion.
    /// Spread across the map, explore separately.
    /// 50% more aggressive forward push.
    /// </summary>
    Dispersed,
    /// <summary>
    /// Ships maintain moderate spacing (~6 tiles from fleet center).
    /// Balanced approach between independence and coordination.
    /// Normal forward aggression, 2.0x cohesion weight.
    /// </summary>
    Loose,
    /// <summary>
    /// Ships stay tightly grouped (~4 tiles from fleet center).
    /// Coordinated movement as a unified formation.
    /// Reduced forward aggression, 4.0x cohesion weight.
    /// </summary>
    Tight
}