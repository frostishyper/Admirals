using UnityEngine;

/// <summary>
/// Metadata snapshot for a single unit within the AI's fleet.
/// Contains tactical classification, health status, and position information
/// extracted from RuntimeUnit or ParticipantUnitSnapshot at snapshot generation time.
/// Supports both legacy RuntimeUnit-based and new snapshot-based architectures.
/// </summary>
public class FleetMember
{
    /// <summary>
    /// Reference to the RuntimeUnit instance (legacy architecture).
    /// Null when using snapshot-based architecture.
    /// </summary>
    public RuntimeUnit Unit { get; set; }

    /// <summary>
    /// Snapshot of the unit's state at the time of fleet snapshot generation.
    /// Uses ParticipantUnitSnapshot for snapshot-based architecture compatibility.
    /// Null when using legacy RuntimeUnit-based architecture.
    /// </summary>
    public ParticipantUnitSnapshot UnitSnapshot { get; set; }

    /// <summary>
    /// Tactical role classification based on unit capabilities.
    /// Determines movement priority and positioning behavior.
    /// </summary>
    public UnitRole Role { get; set; }

    /// <summary>
    /// Current health as a fraction of maximum health (0.0 to 1.0).
    /// Used for prioritization and vulnerability assessment.
    /// </summary>
    public float HpFraction { get; set; }

    /// <summary>
    /// Combined vulnerability metric factoring both role and current health.
    /// Scouts are inherently more fragile (multiplier 1.3),
    /// Anchors are more durable (multiplier 0.7),
    /// and damage further increases fragility.
    /// Range: 0.0 (undamaged Anchor) to 1.0+ (critically damaged Scout)
    /// </summary>
    public float EffectiveFragility { get; set; }

    /// <summary>
    /// Current movement pivot coordinate on the game map.
    /// Cached from RuntimeUnit or ParticipantUnitSnapshot for efficient access during scoring.
    /// </summary>
    public Vector2Int Position { get; set; }

    /// <summary>
    /// Current unit facing direction (North, East, South, West).
    /// Cached from RuntimeUnit or ParticipantUnitSnapshot for efficient access during scoring.
    /// </summary>
    public UnitFacing Facing { get; set; }
}
