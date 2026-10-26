using System.Collections.Generic;
using UnityEngine;
using Admirals.AI;

/// <summary>
/// Strategic command layer that evaluates fleet state and selects high-level tactical stance.
/// Builds immutable FleetSnapshot instances containing all strategic state needed by tactical subsystems.
/// Tracks enemy alive estimates and manages stance transitions with hysteresis and dwell enforcement.
/// 
/// This is a plain class (not MonoBehaviour) instantiated by AIController on first Movement phase.
/// No static state - all state is instance-based to support clean initialization/cleanup between matches.
/// </summary>
public class FleetCommander
{
    // Stance threshold constants (Requirement 5)
    private const float RetreatScoreBelow = 0.35f;      // Material score triggering emergency retreat
    private const float RetreatHpBelow = 0.30f;         // HP fraction triggering emergency retreat
    private const float FallbackScoreBelow = 0.60f;     // Material score triggering defensive fallback
    private const float AdvanceScoreAtLeast = 0.90f;    // Material score triggering aggressive advance

    // Hysteresis margin: current stance's threshold is widened by this amount (Requirement 5.8)
    private const float HysteresisMargin = 0.08f;

    // Minimum turns a stance must be maintained before switching (Requirement 5.9)
    private const int MinDwellTurns = 2;

    // Role classification thresholds (Requirement 6.2, 6.3)
    private const int ScoutMinSpeed = 6;
    private const int ScoutMinRange = 4;
    private const int AnchorMinHealth = 80;
    private const int AnchorMinWeapons = 2;

    // Role fragility multipliers for EffectiveFragility calculation (Requirement 7.10)
    private const float ScoutFragilityMultiplier = 1.3f;   // Scouts are squishier
    private const float AnchorFragilityMultiplier = 0.7f;  // Anchors are more durable
    private const float StrikerFragilityMultiplier = 1.0f; // Strikers are balanced

    private readonly RuntimeParticipant owner;
    private readonly int startingFleetSize;

    private int enemyAliveEstimate;
    private Vector2Int homeCentroid;
    private bool homeSet;
    private int turnsInStance;

    /// <summary>
    /// Current fleet strategic stance.
    /// </summary>
    public FleetStance CurrentStance { get; private set; } = FleetStance.Hunt;

    /// <summary>
    /// Last generated fleet snapshot for use by other subsystems.
    /// </summary>
    public FleetSnapshot LastSnapshot { get; private set; }

    /// <summary>
    /// Current estimate of living enemy units (decremented on confirmed kills).
    /// </summary>
    public int EnemyAliveEstimate => enemyAliveEstimate;

    /// <summary>
    /// Initializes a new FleetCommander for the specified participant using ParticipantGameSnapshot.
    /// This constructor extracts unit data from snapshot.OwnUnits instead of direct GameMaster queries.
    /// </summary>
    /// <param name="snapshot">The participant game snapshot containing unit data.</param>
    /// <param name="owner">The RuntimeParticipant this commander controls.</param>
    /// <param name="enemyParticipant">The opponent RuntimeParticipant.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if snapshot or owner is null.</exception>
    public FleetCommander(ParticipantGameSnapshot snapshot, RuntimeParticipant owner, RuntimeParticipant enemyParticipant)
    {
        if (snapshot == null)
        {
            throw new System.ArgumentNullException(nameof(snapshot));
        }

        if (owner == null)
        {
            throw new System.ArgumentNullException(nameof(owner));
        }

        this.owner = owner;
        
        // Count all units in snapshot (includes active and destroyed)
        this.startingFleetSize = snapshot.OwnUnits != null ? snapshot.OwnUnits.Length : 0;

        // Initialize enemy estimate to enemy participant's starting fleet size
        // Assume symmetric match for initial estimate
        this.enemyAliveEstimate = this.startingFleetSize;

        Debug.Log($"[AI][Fleet] Initialized FleetCommander from snapshot: owner={owner.Participant.DisplayName} " +
                  $"startingFleetSize={this.startingFleetSize} enemyAliveEstimate={this.enemyAliveEstimate}");
    }

    /// <summary>
    /// Called by AIController after a confirmed kill of an enemy unit.
    /// Decrements enemy alive estimate for accurate Material Score calculations.
    /// </summary>
    public void NoteKillConfirmed()
    {
        enemyAliveEstimate = Mathf.Max(0, enemyAliveEstimate - 1);
        Debug.Log($"[AI][Fleet] Kill confirmed. Enemy units estimated alive: {enemyAliveEstimate}/{startingFleetSize}");
    }

    /// <summary>
    /// Evaluates current fleet state and generates a FleetSnapshot with selected stance.
    /// Called once per Movement phase by AIController.
    /// Uses ParticipantGameSnapshot for decoupled data access.
    /// </summary>
    /// <param name="snapshot">The participant game snapshot containing unit lists and game state.</param>
    /// <param name="debugForceStance">Optional stance override for testing purposes.</param>
    /// <returns>An immutable FleetSnapshot containing all strategic state for this turn.</returns>
    public FleetSnapshot Evaluate(ParticipantGameSnapshot snapshot, FleetStance? debugForceStance = null)
    {
        // Performance logging (Requirement 20.1)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (snapshot == null)
        {
            throw new System.ArgumentNullException(nameof(snapshot));
        }

        FleetSnapshot fleetSnap = BuildSnapshotFromParticipantSnapshot(snapshot);
        LastSnapshot = fleetSnap;

        if (fleetSnap.Members.Count == 0)
        {
            Debug.Log("[AI][Fleet] No living units; nothing to evaluate.");
            return fleetSnap;
        }

        // Choose stance based on tactical situation
        FleetStance desired = debugForceStance ?? ChooseStance(fleetSnap);
        if (debugForceStance.HasValue)
        {
            Debug.Log($"[AI][Fleet] Debug override active: forcing stance={debugForceStance.Value}");
        }

        FleetStance previous = CurrentStance;
        FleetStance chosen = desired;
        string dwellNote = "";

        // Apply stance dwell enforcement (except for safety transitions)
        if (desired != CurrentStance && IsLockedByDwell(desired))
        {
            dwellNote = $" (wanted {desired}, held by dwell {turnsInStance}/{MinDwellTurns})";
            chosen = CurrentStance;
        }

        // Update stance and dwell counter
        string transitionNote;
        if (chosen != CurrentStance)
        {
            CurrentStance = chosen;
            turnsInStance = 1;
            transitionNote = $" (changed from {previous})";
        }
        else
        {
            turnsInStance++;
            transitionNote = $" (turn {turnsInStance} in stance)";
        }

        // Log stance evaluation for debugging and telemetry (Requirement 18)
        Debug.Log($"[AI][Fleet] stance={CurrentStance}{transitionNote}{dwellNote} " +
                  $"score={fleetSnap.MaterialScore:F2} ownAlive={fleetSnap.OwnAlive}/{fleetSnap.OwnStartCount} " +
                  $"enemyEst={fleetSnap.EnemyAliveEstimate} fleetHp={fleetSnap.FleetHpFraction:F2} " +
                  $"contacts={fleetSnap.Contacts.Count} nearest={fleetSnap.NearestContactDistance:F1} " +
                  $"progress={fleetSnap.MapProgress:F2} centroid={fleetSnap.FleetCentroid} home={fleetSnap.HomeCentroid}");

        LogRosterFromParticipantSnapshot(fleetSnap);
        LogContactsFromParticipantSnapshot(fleetSnap, snapshot);

        // Log performance if threshold exceeded (Requirement 20.1)
        stopwatch.Stop();
        const float thresholdMs = 50f;
        if (stopwatch.Elapsed.TotalMilliseconds > thresholdMs)
        {
            Debug.Log($"[AI][Fleet][Perf] Evaluate took {stopwatch.Elapsed.TotalMilliseconds:F1}ms (threshold: {thresholdMs}ms)");
        }

        return fleetSnap;
    }

    /// <summary>
    /// Refreshes contact list during Battle phase after Search may have revealed new enemies.
    /// Does not change stance or tick dwell counter (stances are once-per-turn at Movement).
    /// Uses ParticipantGameSnapshot for decoupled data access.
    /// </summary>
    /// <param name="snapshot">The participant game snapshot with updated enemy visible units.</param>
    /// <returns>An updated FleetSnapshot with refreshed contacts.</returns>
    public FleetSnapshot RefreshContacts(ParticipantGameSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new System.ArgumentNullException(nameof(snapshot));
        }

        FleetSnapshot fleetSnap = BuildSnapshotFromParticipantSnapshot(snapshot);
        LastSnapshot = fleetSnap;

        Debug.Log($"[AI][Fleet] Battle refresh: stance={CurrentStance} contacts={fleetSnap.Contacts.Count} " +
                  $"nearest={fleetSnap.NearestContactDistance:F1}");
        LogContactsFromParticipantSnapshot(fleetSnap, snapshot);

        return fleetSnap;
    }

    // ---- Stance Selection (Requirement 5) -------------------------------------------------------

    /// <summary>
    /// Selects appropriate FleetStance based on Material Score, HP fraction, and contact visibility.
    /// Applies hysteresis to prevent rapid stance flipping.
    /// </summary>
    private FleetStance ChooseStance(FleetSnapshot snapshot)
    {
        float score = snapshot.MaterialScore;
        float hp = snapshot.FleetHpFraction;
        FleetStance cur = CurrentStance;

        // Safety conditions: Retreat on critical damage or overwhelming odds (Requirement 5.3)
        // Retreat transitions are never delayed (Requirement 5.10)
        float retreatStay = cur == FleetStance.Retreat ? HysteresisMargin : 0f;
        if (score < RetreatScoreBelow + retreatStay || hp < RetreatHpBelow + retreatStay)
        {
            return FleetStance.Retreat;
        }

        // Defensive fallback on material disadvantage (Requirement 5.4)
        float fallbackStay = cur == FleetStance.DefensiveFallback ? HysteresisMargin : 0f;
        if (score < FallbackScoreBelow + fallbackStay)
        {
            return FleetStance.DefensiveFallback;
        }

        // Hunt when no contacts visible (Requirement 5.6)
        // Hunt exits are never delayed (Requirement 5.10)
        if (snapshot.Contacts.Count == 0)
        {
            return FleetStance.Hunt;
        }

        // Advance on strong position (Requirement 5.5)
        float advanceStay = cur == FleetStance.Advance ? HysteresisMargin : 0f;
        if (score >= AdvanceScoreAtLeast - advanceStay)
        {
            return FleetStance.Advance;
        }

        // Default: Hold position with balanced posture (Requirement 5.7)
        return FleetStance.Hold;
    }

    /// <summary>
    /// Checks if stance transition is blocked by minimum dwell enforcement.
    /// Retreat and Hunt exits are never locked (safety and information overrides).
    /// </summary>
    private bool IsLockedByDwell(FleetStance desired)
    {
        if (CurrentStance == FleetStance.Hunt) return false;        // Information arrived
        if (desired == FleetStance.Retreat) return false;           // Safety override
        return turnsInStance < MinDwellTurns;
    }

    // ---- Snapshot Generation (Requirement 7) ---------------------------------------------------

    /// <summary>
    /// Builds an immutable FleetSnapshot from ParticipantGameSnapshot.
    /// Calculates all strategic metrics, classifications, and derived state.
    /// This version extracts data from snapshot arrays instead of querying GameMaster.
    /// </summary>
    private FleetSnapshot BuildSnapshotFromParticipantSnapshot(ParticipantGameSnapshot snapshot)
    {
        var snap = new FleetSnapshot
        {
            Owner = owner,
            CurrentStance = CurrentStance,
            TurnsInStance = turnsInStance,
            OwnStartCount = startingFleetSize,
            EnemyAliveEstimate = enemyAliveEstimate
        };

        int totalCurrentHP = 0;
        int totalMaxHP = 0;
        int sumX = 0;
        int sumY = 0;

        // Build fleet member list with role classification and fragility (Requirement 6, 7.2)
        // Extract from snapshot.OwnUnits instead of context.OwnAliveUnits
        foreach (ParticipantUnitSnapshot unitSnap in snapshot.OwnUnits)
        {
            if (unitSnap == null || unitSnap.LifeState != RuntimeUnitLifeState.Active)
                continue;

            if (!unitSnap.IsOnMap || unitSnap.OccupiedCoordinates == null || unitSnap.OccupiedCoordinates.Length == 0)
            {
                Debug.LogWarning($"[AI][Fleet] Unit {unitSnap.InstanceName} not on map or has no coordinates");
                continue;
            }

            Vector2Int position = unitSnap.MovementPivotCoordinate;

            // Classify role based on unit capabilities (Requirement 6.1-6.4)
            UnitRole role = ClassifyRoleFromSnapshot(unitSnap);

            // Calculate health metrics (Requirement 16.3)
            float hpFraction = unitSnap.MaxHealth > 0
                ? Mathf.Clamp01((float)unitSnap.CurrentHealth / unitSnap.MaxHealth)
                : 0f;

            // Calculate effective fragility with role multipliers (Requirement 7.10)
            float effectiveFragility = CalculateEffectiveFragilityFromSnapshot(unitSnap, role, hpFraction);

            snap.Members.Add(new FleetMember
            {
                Unit = null, // RuntimeUnit not available from snapshot
                UnitSnapshot = unitSnap, // Store snapshot reference instead
                Role = role,
                HpFraction = hpFraction,
                EffectiveFragility = effectiveFragility,
                Position = position,
                Facing = unitSnap.Facing
            });

            totalCurrentHP += unitSnap.CurrentHealth;
            totalMaxHP += unitSnap.MaxHealth;
            sumX += position.x;
            sumY += position.y;
        }

        snap.OwnAlive = snap.Members.Count;

        // Calculate fleet aggregate health (Requirement 5.2, 7.4)
        snap.FleetHpFraction = totalMaxHP > 0
            ? Mathf.Clamp01((float)totalCurrentHP / totalMaxHP)
            : 0f;

        // Calculate material score: own strength relative to combined forces (Requirement 5.1, 7.4)
        float ratio = (float)snap.OwnAlive / Mathf.Max(1, snap.EnemyAliveEstimate);
        snap.MaterialScore = ratio * (0.5f + 0.5f * snap.FleetHpFraction);

        // Calculate fleet centroid (mean position of alive units) (Requirement 7.6)
        if (snap.OwnAlive > 0)
        {
            snap.FleetCentroid = new Vector2Int(
                Mathf.RoundToInt((float)sumX / snap.OwnAlive),
                Mathf.RoundToInt((float)sumY / snap.OwnAlive));

            // Capture home centroid on first evaluation (Requirement 7.7)
            if (!homeSet)
            {
                homeCentroid = snap.FleetCentroid;
                homeSet = true;
            }
        }

        snap.HomeCentroid = homeCentroid;

        // Calculate map progress: how far fleet has advanced from home (Requirement 7.9)
        snap.MapProgress = ComputeMapProgress(snap.FleetCentroid, snapshot);

        // Populate contacts from visible enemies (Requirement 7.3)
        // Extract from snapshot.VisibleEnemies instead of context.EnemyVisibleUnits
        snap.Contacts = new List<RuntimeUnit>();
        snap.EnemySnapshots = new List<VisibleEnemyUnitSnapshot>();
        
        if (snapshot.VisibleEnemies != null)
        {
            foreach (VisibleEnemyUnitSnapshot enemySnap in snapshot.VisibleEnemies)
            {
                if (enemySnap != null)
                {
                    snap.EnemySnapshots.Add(enemySnap);
                }
            }
        }

        // Find nearest contact for threat assessment (Requirement 7.8)
        FindNearestContactFromSnapshot(snap);

        return snap;
    }

    // ---- Role Classification (Requirement 6) ---------------------------------------------------

    /// <summary>
    /// Classifies a unit's tactical role based on speed, range, health, and weapon count.
    /// Uses ParticipantUnitSnapshot instead of RuntimeUnit.
    /// Scout: Fast reconnaissance (speed >= 6, range >= 4)
    /// Anchor: Durable battle line (HP >= 80, weapons >= 2)
    /// Striker: Balanced tactical unit (default)
    /// </summary>
    private static UnitRole ClassifyRoleFromSnapshot(ParticipantUnitSnapshot unitSnap)
    {
        if (unitSnap == null || unitSnap.Definition == null)
            return UnitRole.Striker;

        Definition_Unit def = unitSnap.Definition;

        // Extract capabilities from definition
        int maxSpeed = GetMovementBudgetFromDefinition(def);
        int maxRange = GetMaxWeaponRangeFromSnapshot(unitSnap);
        int maxHealth = def.Health;
        int weaponCount = CountEquippedWeaponsFromSnapshot(unitSnap);

        // Scout: Fast + Long Range (Requirement 6.2)
        if (maxSpeed >= ScoutMinSpeed && maxRange >= ScoutMinRange)
        {
            return UnitRole.Scout;
        }

        // Anchor: Durable + Multi-weapon (Requirement 6.3)
        if (maxHealth >= AnchorMinHealth && weaponCount >= AnchorMinWeapons)
        {
            return UnitRole.Anchor;
        }

        // Default: Striker (Requirement 6.4)
        return UnitRole.Striker;
    }

    /// <summary>
    /// Gets the movement budget (maximum movement per turn) from the unit definition.
    /// The movement budget is determined by the strongest layer in the movement pattern.
    /// </summary>
    private static int GetMovementBudgetFromDefinition(Definition_Unit definition)
    {
        if (definition == null || definition.MovementPattern == null)
            return 0;

        Definition_Pattern pattern = definition.MovementPattern;
        if (pattern.Layers == null || pattern.Layers.Length == 0)
            return 0;

        int maxStrength = 0;
        foreach (var layer in pattern.Layers)
        {
            if (layer != null)
            {
                maxStrength = Mathf.Max(maxStrength, layer.Strength);
            }
        }

        return maxStrength;
    }

    /// <summary>
    /// Gets the maximum weapon range across all equipped weapons from a ParticipantUnitSnapshot.
    /// </summary>
    private static int GetMaxWeaponRangeFromSnapshot(ParticipantUnitSnapshot unitSnap)
    {
        if (unitSnap.Weapons == null || unitSnap.Weapons.Length == 0)
            return 0;

        int maxRange = 0;
        foreach (var weaponSnap in unitSnap.Weapons)
        {
            if (weaponSnap != null && weaponSnap.Definition != null)
            {
                maxRange = Mathf.Max(maxRange, 5 /* Default range - Definition_Weapon.Range/MaximumRange not accessible */);
            }
        }

        return maxRange;
    }

    /// <summary>
    /// Counts the number of equipped weapon slots from a ParticipantUnitSnapshot.
    /// </summary>
    private static int CountEquippedWeaponsFromSnapshot(ParticipantUnitSnapshot unitSnap)
    {
        if (unitSnap.Weapons == null || unitSnap.Weapons.Length == 0)
            return 0;

        int count = 0;
        foreach (var weaponSnap in unitSnap.Weapons)
        {
            if (weaponSnap != null && weaponSnap.Definition != null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Calculates effective fragility considering both current health and role characteristics.
    /// Uses ParticipantUnitSnapshot instead of RuntimeUnit.
    /// Scouts feel damage more (1.3x multiplier), Anchors tolerate more (0.7x multiplier).
    /// Range: 0.0 (undamaged Anchor) to 1.3+ (critically damaged Scout)
    /// </summary>
    private static float CalculateEffectiveFragilityFromSnapshot(ParticipantUnitSnapshot unitSnap, UnitRole role, float hpFraction)
    {
        // Raw fragility: how damaged the unit is (1.0 = fully damaged, 0.0 = undamaged)
        float rawFragility = 1.0f - hpFraction;

        // Apply role-based multiplier
        float roleMultiplier = role switch
        {
            UnitRole.Scout => ScoutFragilityMultiplier,   // Scouts are squishier
            UnitRole.Anchor => AnchorFragilityMultiplier, // Anchors are more durable
            UnitRole.Striker => StrikerFragilityMultiplier, // Strikers are balanced
            _ => StrikerFragilityMultiplier
        };

        return Mathf.Clamp01(rawFragility * roleMultiplier);
    }

    // ---- Spatial Calculations ------------------------------------------------------------------

    /// <summary>
    /// Computes how far the fleet has advanced from home toward map center.
    /// 0.0 = at home deployment position, 1.0 = reached map center.
    /// Derives map dimensions from ParticipantGameSnapshot.Tiles array.
    /// </summary>
    /// <param name="centroid">The current fleet centroid position.</param>
    /// <param name="snapshot">The participant game snapshot containing tile data.</param>
    private float ComputeMapProgress(Vector2Int centroid, ParticipantGameSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Tiles == null || snapshot.Tiles.Length == 0)
            return 0f;

        // Derive map dimensions from snapshot.Tiles array (Design Pattern from RandomRoboAIController)
        int maxX = 0;
        int maxY = 0;
        
        foreach (ParticipantTileSnapshot tile in snapshot.Tiles)
        {
            if (tile != null)
            {
                maxX = Mathf.Max(maxX, tile.Coordinate.x);
                maxY = Mathf.Max(maxY, tile.Coordinate.y);
            }
        }

        if (maxX == 0 && maxY == 0)
            return 0f; // No valid tiles

        var center = new Vector2Int(maxX / 2, maxY / 2);
        Vector2 toCenter = center - homeCentroid;
        float lengthSquared = toCenter.sqrMagnitude;

        if (lengthSquared < 0.01f)
            return 0f; // Home is at map center (shouldn't happen)

        Vector2 moved = centroid - homeCentroid;
        return Mathf.Clamp(Vector2.Dot(moved, toCenter) / lengthSquared, -1f, 2f);
    }

    /// <summary>
    /// Finds the nearest enemy contact and calculates minimum distance to fleet.
    /// Uses snapshot data instead of RuntimeUnit queries.
    /// Sets NearestContactDistance to -1 if no contacts are visible.
    /// </summary>
    private void FindNearestContactFromSnapshot(FleetSnapshot snap)
    {
        if (snap.EnemySnapshots == null || snap.EnemySnapshots.Count == 0)
        {
            snap.NearestContactDistance = -1f;
            return;
        }

        float minDistance = float.MaxValue;

        // Check distance from each own unit to each enemy contact
        foreach (FleetMember member in snap.Members)
        {
            foreach (VisibleEnemyUnitSnapshot enemySnap in snap.EnemySnapshots)
            {
                if (enemySnap == null || enemySnap.OccupiedCoordinates == null || enemySnap.OccupiedCoordinates.Length == 0)
                    continue;

                Vector2Int enemyPos = enemySnap.MovementPivotCoordinate;
                int distance = SpatialCalculations.ManhattanDistance(member.Position, enemyPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        snap.NearestContactDistance = minDistance < float.MaxValue ? minDistance : -1f;
    }

    // ---- Logging (Requirement 18) --------------------------------------------------------------

    /// <summary>
    /// Logs the current fleet roster with roles and fragility for debugging.
    /// Uses snapshot data instead of RuntimeUnit.
    /// </summary>
    private static void LogRosterFromParticipantSnapshot(FleetSnapshot snap)
    {
        var sb = new System.Text.StringBuilder("[AI][Fleet] roster:");
        foreach (FleetMember m in snap.Members)
        {
            string unitName = m.UnitSnapshot != null ? m.UnitSnapshot.InstanceName : "Unknown";
            sb.Append($" {unitName}={m.Role} frag={m.EffectiveFragility:F2}(hp {m.HpFraction:F2});");
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Logs the current contact list with positions for debugging.
    /// Uses snapshot data instead of RuntimeUnit.
    /// </summary>
    private void LogContactsFromParticipantSnapshot(FleetSnapshot snap, ParticipantGameSnapshot gameSnapshot)
    {
        if (snap.EnemySnapshots == null || snap.EnemySnapshots.Count == 0)
        {
            Debug.Log("[AI][Fleet] contacts: none visible");
            return;
        }

        for (int i = 0; i < snap.EnemySnapshots.Count; i++)
        {
            VisibleEnemyUnitSnapshot enemySnap = snap.EnemySnapshots[i];
            if (enemySnap != null && enemySnap.OccupiedCoordinates != null && enemySnap.OccupiedCoordinates.Length > 0)
            {
                Vector2Int pos = enemySnap.MovementPivotCoordinate;
                // Note: VisibleEnemyUnitSnapshot doesn't expose health, so we can't log it
                Debug.Log($"[AI][Fleet] contact#{i} {enemySnap.InstanceName} pos={pos}");
            }
        }
    }

}
