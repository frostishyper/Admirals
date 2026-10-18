using System.Collections.Generic;
using UnityEngine;

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

    private readonly GridManager gridManager;
    private readonly GameMap gameMap;
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
    /// Initializes a new FleetCommander for the specified participant.
    /// </summary>
    /// <param name="gridManager">The grid manager for spatial calculations.</param>
    /// <param name="gameMap">The game map for unit position queries.</param>
    /// <param name="owner">The RuntimeParticipant this commander controls.</param>
    /// <param name="startingFleetSize">The number of units in the fleet at match start.</param>
    public FleetCommander(GridManager gridManager, GameMap gameMap, RuntimeParticipant owner, int startingFleetSize)
    {
        this.gridManager = gridManager;
        this.gameMap = gameMap;
        this.owner = owner;
        this.startingFleetSize = startingFleetSize;

        // Initialize enemy estimate to same size as own fleet (symmetric match assumption)
        enemyAliveEstimate = startingFleetSize;
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
    /// </summary>
    /// <param name="context">The turn context containing unit lists and game state.</param>
    /// <param name="debugForceStance">Optional stance override for testing purposes.</param>
    /// <returns>An immutable FleetSnapshot containing all strategic state for this turn.</returns>
    public FleetSnapshot Evaluate(AITurnContext context, FleetStance? debugForceStance = null)
    {
        FleetSnapshot snapshot = BuildSnapshot(context);
        LastSnapshot = snapshot;

        if (snapshot.Members.Count == 0)
        {
            Debug.Log("[AI][Fleet] No living units; nothing to evaluate.");
            return snapshot;
        }

        // Choose stance based on tactical situation
        FleetStance desired = debugForceStance ?? ChooseStance(snapshot);
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
                  $"score={snapshot.MaterialScore:F2} ownAlive={snapshot.OwnAlive}/{snapshot.OwnStartCount} " +
                  $"enemyEst={snapshot.EnemyAliveEstimate} fleetHp={snapshot.FleetHpFraction:F2} " +
                  $"contacts={snapshot.Contacts.Count} nearest={snapshot.NearestContactDistance:F1} " +
                  $"progress={snapshot.MapProgress:F2} centroid={snapshot.FleetCentroid} home={snapshot.HomeCentroid}");

        LogRoster(snapshot);
        LogContacts(snapshot);

        return snapshot;
    }

    /// <summary>
    /// Refreshes contact list during Battle phase after Search may have revealed new enemies.
    /// Does not change stance or tick dwell counter (stances are once-per-turn at Movement).
    /// </summary>
    /// <param name="context">The turn context with updated enemy visible units.</param>
    /// <returns>An updated FleetSnapshot with refreshed contacts.</returns>
    public FleetSnapshot RefreshContacts(AITurnContext context)
    {
        FleetSnapshot snapshot = BuildSnapshot(context);
        LastSnapshot = snapshot;

        Debug.Log($"[AI][Fleet] Battle refresh: stance={CurrentStance} contacts={snapshot.Contacts.Count} " +
                  $"nearest={snapshot.NearestContactDistance:F1}");
        LogContacts(snapshot);

        return snapshot;
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
    /// Builds an immutable FleetSnapshot from current AITurnContext.
    /// Calculates all strategic metrics, classifications, and derived state.
    /// </summary>
    private FleetSnapshot BuildSnapshot(AITurnContext context)
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
        foreach (RuntimeUnit unit in context.OwnAliveUnits)
        {
            if (unit == null || !unit.IsActive)
                continue;

            // Get unit position from GameMap (Requirement 16.1)
            if (!gameMap.TryGetUnitCoordinates(unit, out Vector2Int[] coordinates) || coordinates.Length == 0)
            {
                Debug.LogWarning($"[AI][Fleet] Could not get coordinates for unit {unit.RuntimeID}");
                continue;
            }

            Vector2Int position = coordinates[0]; // Use first footprint cell as position

            // Classify role based on unit capabilities (Requirement 6.1-6.4)
            UnitRole role = ClassifyRole(unit);

            // Calculate health metrics (Requirement 16.3)
            float hpFraction = unit.MaxHealth > 0
                ? Mathf.Clamp01((float)unit.CurrentHealth / unit.MaxHealth)
                : 0f;

            // Calculate effective fragility with role multipliers (Requirement 7.10)
            float effectiveFragility = CalculateEffectiveFragility(unit, role, hpFraction);

            snap.Members.Add(new FleetMember
            {
                Unit = unit,
                Role = role,
                HpFraction = hpFraction,
                EffectiveFragility = effectiveFragility,
                Position = position,
                Facing = unit.Facing
            });

            totalCurrentHP += unit.CurrentHealth;
            totalMaxHP += unit.MaxHealth;
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
            // This is where the fleet starts, determines retreat direction
            if (!homeSet)
            {
                homeCentroid = snap.FleetCentroid;
                homeSet = true;
            }
        }

        snap.HomeCentroid = homeCentroid;

        // Calculate map progress: how far fleet has advanced from home (Requirement 7.9)
        snap.MapProgress = ComputeMapProgress(snap.FleetCentroid);

        // Populate contacts from visible enemies (Requirement 7.3)
        snap.Contacts = new List<RuntimeUnit>(context.EnemyVisibleUnits);

        // Find nearest contact for threat assessment (Requirement 7.8)
        FindNearestContact(snap);

        return snap;
    }

    // ---- Role Classification (Requirement 6) ---------------------------------------------------

    /// <summary>
    /// Classifies a unit's tactical role based on speed, range, health, and weapon count.
    /// Scout: Fast reconnaissance (speed >= 6, range >= 4)
    /// Anchor: Durable battle line (HP >= 80, weapons >= 2)
    /// Striker: Balanced tactical unit (default)
    /// </summary>
    private static UnitRole ClassifyRole(RuntimeUnit unit)
    {
        if (unit == null || unit.Definition == null)
            return UnitRole.Striker;

        Definition_Unit def = unit.Definition;

        // Extract capabilities from definition
        int maxSpeed = GetMovementBudget(unit); // Movement budget per turn
        int maxRange = GetMaxWeaponRange(unit);
        int maxHealth = def.Health;
        int weaponCount = CountEquippedWeapons(unit);

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
    /// Gets the movement budget (maximum movement per turn) from the unit's movement pattern.
    /// The movement budget is determined by the strongest layer in the movement pattern.
    /// </summary>
    private static int GetMovementBudget(RuntimeUnit unit)
    {
        if (unit == null || unit.Definition == null || unit.Definition.MovementPattern == null)
            return 0;

        Definition_Pattern pattern = unit.Definition.MovementPattern;
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
    /// Gets the maximum weapon range across all equipped weapons.
    /// </summary>
    private static int GetMaxWeaponRange(RuntimeUnit unit)
    {
        if (unit.WeaponSlots == null || unit.WeaponSlots.Length == 0)
            return 0;

        int maxRange = 0;
        foreach (var slot in unit.WeaponSlots)
        {
            if (slot != null && slot.Weapon != null)
            {
                maxRange = Mathf.Max(maxRange, slot.Weapon.Range);
            }
        }

        return maxRange;
    }

    /// <summary>
    /// Counts the number of equipped weapon slots.
    /// </summary>
    private static int CountEquippedWeapons(RuntimeUnit unit)
    {
        if (unit.WeaponSlots == null || unit.WeaponSlots.Length == 0)
            return 0;

        int count = 0;
        foreach (var slot in unit.WeaponSlots)
        {
            if (slot != null && slot.Weapon != null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Calculates effective fragility considering both current health and role characteristics.
    /// Scouts feel damage more (1.3x multiplier), Anchors tolerate more (0.7x multiplier).
    /// Range: 0.0 (undamaged Anchor) to 1.3+ (critically damaged Scout)
    /// </summary>
    private static float CalculateEffectiveFragility(RuntimeUnit unit, UnitRole role, float hpFraction)
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
    /// </summary>
    private float ComputeMapProgress(Vector2Int centroid)
    {
        if (gridManager == null)
            return 0f;

        var center = new Vector2Int(gridManager.width / 2, gridManager.height / 2);
        Vector2 toCenter = center - homeCentroid;
        float lengthSquared = toCenter.sqrMagnitude;

        if (lengthSquared < 0.01f)
            return 0f; // Home is at map center (shouldn't happen)

        Vector2 moved = centroid - homeCentroid;
        return Mathf.Clamp(Vector2.Dot(moved, toCenter) / lengthSquared, -1f, 2f);
    }

    /// <summary>
    /// Finds the nearest enemy contact and calculates minimum distance to fleet.
    /// Sets NearestContactDistance to -1 if no contacts are visible.
    /// </summary>
    private void FindNearestContact(FleetSnapshot snap)
    {
        if (snap.Contacts.Count == 0)
        {
            snap.NearestContactDistance = -1f;
            return;
        }

        float minDistance = float.MaxValue;

        // Check distance from each own unit to each enemy contact
        foreach (FleetMember member in snap.Members)
        {
            foreach (RuntimeUnit enemy in snap.Contacts)
            {
                if (enemy == null || !enemy.IsActive)
                    continue;

                if (!gameMap.TryGetUnitCoordinates(enemy, out Vector2Int[] enemyCoords) || enemyCoords.Length == 0)
                    continue;

                Vector2Int enemyPos = enemyCoords[0];
                int distance = gridManager.DistanceBetween(member.Position, enemyPos);

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
    /// </summary>
    private static void LogRoster(FleetSnapshot snap)
    {
        var sb = new System.Text.StringBuilder("[AI][Fleet] roster:");
        foreach (FleetMember m in snap.Members)
        {
            sb.Append($" {m.Unit.InstanceName}={m.Role} frag={m.EffectiveFragility:F2}(hp {m.HpFraction:F2});");
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Logs the current contact list with positions for debugging.
    /// </summary>
    private void LogContacts(FleetSnapshot snap)
    {
        if (snap.Contacts.Count == 0)
        {
            Debug.Log("[AI][Fleet] contacts: none visible");
            return;
        }

        for (int i = 0; i < snap.Contacts.Count; i++)
        {
            RuntimeUnit contact = snap.Contacts[i];
            if (contact != null && gameMap.TryGetUnitCoordinates(contact, out Vector2Int[] coords) && coords.Length > 0)
            {
                Debug.Log($"[AI][Fleet] contact#{i} {contact.InstanceName} pos={coords[0]} hp={contact.CurrentHealth}/{contact.MaxHealth}");
            }
        }
    }
}
