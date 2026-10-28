/// <summary>
/// Tactical role classification for units based on their combat characteristics.
/// Used by the AI to determine movement priority and tactical positioning.
/// </summary>
public enum UnitRole
{
    /// <summary>
    /// Fast reconnaissance units with long-range sensors.
    /// Prioritized for forward positioning and exploration.
    /// Classified when: maxSpeed >= 6 AND maxWeaponRange >= 4
    /// </summary>
    Scout,

    /// <summary>
    /// Durable defensive units that form the battle line.
    /// Heavy armor and multiple weapon systems.
    /// Classified when: maxHealth >= 80 AND weaponCount >= 2
    /// </summary>
    Anchor,

    /// <summary>
    /// Balanced tactical units for flexible engagement.
    /// Default classification when Scout and Anchor criteria are not met.
    /// </summary>
    Striker
}
