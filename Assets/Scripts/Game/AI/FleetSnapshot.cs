using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable snapshot of fleet strategic state generated once per turn.
/// Contains all information needed by tactical subsystems to make decisions
/// without requiring repeated GameMaster queries. Shared across all AI subsystems
/// for consistent reasoning.
/// </summary>
public class FleetSnapshot
{
    /// <summary>
    /// The participant who owns this fleet.
    /// </summary>
    public RuntimeParticipant Owner { get; set; }

    /// <summary>
    /// Current high-level strategic posture guiding tactical behavior.
    /// </summary>
    public FleetStance CurrentStance { get; set; }

    /// <summary>
    /// Number of consecutive turns spent in the current stance.
    /// Used for stance dwell enforcement (minimum 2 turns before switching).
    /// </summary>
    public int TurnsInStance { get; set; }

    /// <summary>
    /// Number of fleet units at match start.
    /// Used as denominator in material score calculation.
    /// </summary>
    public int OwnStartCount { get; set; }

    /// <summary>
    /// Number of own units currently alive and active.
    /// </summary>
    public int OwnAlive { get; set; }

    /// <summary>
    /// Estimated number of living enemy units.
    /// Initialized to enemy starting fleet size, decremented on confirmed kills.
    /// May be inaccurate due to fog-of-war concealing casualties.
    /// </summary>
    public int EnemyAliveEstimate { get; set; }

    /// <summary>
    /// Fleet strength ratio: OwnAlive / (OwnStartCount + EnemyAliveEstimate).
    /// Values near 1.0 indicate even match, > 1.0 indicates advantage, &lt; 0.35 indicates critical disadvantage.
    /// Primary metric for stance selection.
    /// </summary>
    public float MaterialScore { get; set; }

    /// <summary>
    /// Aggregate health fraction across all living units: Sum(currentHealth) / Sum(maxHealth).
    /// Values below 0.30 trigger emergency Retreat stance.
    /// </summary>
    public float FleetHpFraction { get; set; }

    /// <summary>
    /// Mean position of all alive units (movement pivot coordinates).
    /// Used as reference point for formation cohesion calculations.
    /// </summary>
    public Vector2Int FleetCentroid { get; set; }

    /// <summary>
    /// Mean of unit starting positions, captured on first turn and fixed thereafter.
    /// Used as reference point for retreat directional bias and map progress.
    /// </summary>
    public Vector2Int HomeCentroid { get; set; }

    /// <summary>
    /// Distance from home centroid toward map center, normalized by map diagonal.
    /// 0.0 = at home deployment position, 1.0 = reached map center.
    /// Used to assess forward progress during Hunt and Advance stances.
    /// </summary>
    public float MapProgress { get; set; }

    /// <summary>
    /// List of currently visible enemy units (contacts).
    /// Empty during Hunt stance or when fog-of-war conceals all enemies.
    /// </summary>
    public List<RuntimeUnit> Contacts { get; set; }

    /// <summary>
    /// Minimum distance from any own unit to any visible contact.
    /// -1 indicates no contacts are currently visible.
    /// Used for threat proximity assessment and stance transitions.
    /// </summary>
    public float NearestContactDistance { get; set; }

    /// <summary>
    /// Metadata for each unit in the fleet with tactical classification and health status.
    /// Includes only alive units; destroyed units are excluded.
    /// </summary>
    public List<FleetMember> Members { get; set; }

    /// <summary>
    /// Initializes a new fleet snapshot with default empty collections.
    /// </summary>
    public FleetSnapshot()
    {
        Contacts = new List<RuntimeUnit>();
        Members = new List<FleetMember>();
        NearestContactDistance = -1f;
    }
}
