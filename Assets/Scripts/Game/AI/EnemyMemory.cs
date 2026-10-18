using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI-maintained memory of enemy unit positions for prediction when fog conceals them.
/// 
/// This system tracks the last known position and facing of enemy units that have
/// been observed. When a unit disappears from visibility (fog-of-war), the AI can
/// use this historical data to estimate where the unit might currently be based on
/// its last known heading and movement capability.
/// 
/// Memory entries are participant-keyed to support multiple AI opponents in the same
/// match without cross-contamination. Predictions expire after 5 turns to prevent
/// stale data from influencing decisions.
/// 
/// This is deliberately separate from FogGrid's visibility system - FogGrid is
/// authoritative and non-sticky (cells revert to Unknown immediately when not
/// detected), while EnemyMemory is AI-specific predictive tracking.
/// </summary>
public static class EnemyMemory
{
    /// <summary>
    /// Internal storage for a single enemy sighting record.
    /// Tracks position history, movement capability, and staleness.
    /// </summary>
    private class MemoryEntry
    {
        /// <summary>The enemy unit being tracked.</summary>
        public RuntimeUnit Unit { get; set; }

        /// <summary>Last observed map coordinate (movement pivot).</summary>
        public Vector2Int LastKnownPosition { get; set; }

        /// <summary>Previously observed position (one sighting earlier).
        /// Used to infer heading direction for prediction.</summary>
        public Vector2Int PreviousPosition { get; set; }

        /// <summary>Last observed facing direction.</summary>
        public UnitFacing LastKnownFacing { get; set; }

        /// <summary>Movement budget from the unit's definition.
        /// Used to cap predicted displacement (unit can't exceed this per turn).</summary>
        public int MovementBudget { get; set; }

        /// <summary>Turn number when this unit was last visible.
        /// Used to calculate staleness and enforce expiration.</summary>
        public int LastSeenTurn { get; set; }

        /// <summary>Number of turns that have elapsed since last sighting.
        /// Incremented each turn the unit remains hidden.</summary>
        public int TurnsElapsed { get; set; }
    }

    /// <summary>
    /// Participant-keyed storage of enemy memory entries.
    /// First key: Observing participant's RuntimeID
    /// Second key: Enemy unit's RuntimeID
    /// Value: Memory entry with sighting history
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, MemoryEntry>> memory =
        new Dictionary<string, Dictionary<string, MemoryEntry>>();

    /// <summary>
    /// Number of turns after which a prediction is considered too stale to use.
    /// Prevents the AI from acting on outdated information about enemy positions.
    /// </summary>
    private const int PredictionExpirationTurns = 5;

    /// <summary>
    /// Updates memory with currently visible enemy units and ages hidden enemies.
    /// 
    /// This method should be called at the start of each phase where the AI makes
    /// decisions (typically Movement and Battle phases). It performs three actions:
    /// 1. Records new sightings for visible enemies (refreshes position and heading)
    /// 2. Ages existing entries for enemies that remain hidden (increments TurnsElapsed)
    /// 3. Removes entries that have exceeded the expiration threshold
    /// </summary>
    /// <param name="observer">The AI participant observing enemies.</param>
    /// <param name="allLivingEnemies">Complete list of enemy units still alive (includes hidden units).</param>
    /// <param name="visibleEnemies">Subset of living enemies currently visible through fog-of-war.</param>
    /// <param name="currentTurn">Current turn number (1-indexed).</param>
    /// <param name="gameMap">GameMap reference for querying unit positions.</param>
    public static void UpdateContacts(
        RuntimeParticipant observer,
        List<RuntimeUnit> allLivingEnemies,
        List<RuntimeUnit> visibleEnemies,
        int currentTurn,
        GameMap gameMap)
    {
        if (observer == null || gameMap == null)
        {
            Debug.LogWarning("[AI][EnemyMemory] UpdateContacts called with null observer or gameMap");
            return;
        }

        // Ensure participant-specific storage exists
        if (!memory.TryGetValue(observer.Participant.ParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
        {
            participantMemory = new Dictionary<string, MemoryEntry>();
            memory[observer.Participant.ParticipantID] = participantMemory;
        }

        // Remove entries for units that no longer exist (destroyed)
        var unitsToForget = new List<string>();
        foreach (var kvp in participantMemory)
        {
            string trackedUnitID = kvp.Key;
            bool stillAlive = false;

            foreach (RuntimeUnit livingEnemy in allLivingEnemies)
            {
                if (livingEnemy != null && livingEnemy.RuntimeID == trackedUnitID)
                {
                    stillAlive = true;
                    break;
                }
            }

            if (!stillAlive)
            {
                unitsToForget.Add(trackedUnitID);
            }
        }

        foreach (string deadUnitID in unitsToForget)
        {
            participantMemory.Remove(deadUnitID);
        }

        // Update entries for visible enemies (fresh sightings)
        foreach (RuntimeUnit visibleEnemy in visibleEnemies)
        {
            if (visibleEnemy == null)
            {
                continue;
            }

            // Get current position from GameMap
            if (!gameMap.TryGetUnitMovementPivotCoordinate(visibleEnemy, out Vector2Int currentPosition))
            {
                Debug.LogWarning($"[AI][EnemyMemory] Could not get position for visible unit {visibleEnemy.RuntimeID}");
                continue;
            }

            if (participantMemory.TryGetValue(visibleEnemy.RuntimeID, out MemoryEntry existing))
            {
                // Update existing entry with fresh sighting
                // Only shift the heading if position actually changed
                if (existing.LastKnownPosition != currentPosition)
                {
                    existing.PreviousPosition = existing.LastKnownPosition;
                }

                existing.LastKnownPosition = currentPosition;
                existing.LastKnownFacing = visibleEnemy.Facing;
                existing.MovementBudget = visibleEnemy.Definition?.MovementBudget ?? 0;
                existing.LastSeenTurn = currentTurn;
                existing.TurnsElapsed = 0;
            }
            else
            {
                // Create new entry for first-time sighting
                participantMemory[visibleEnemy.RuntimeID] = new MemoryEntry
                {
                    Unit = visibleEnemy,
                    LastKnownPosition = currentPosition,
                    PreviousPosition = currentPosition, // No heading available yet
                    LastKnownFacing = visibleEnemy.Facing,
                    MovementBudget = visibleEnemy.Definition?.MovementBudget ?? 0,
                    LastSeenTurn = currentTurn,
                    TurnsElapsed = 0
                };
            }
        }

        // Age entries for hidden enemies and expire stale predictions
        var entriesToExpire = new List<string>();

        foreach (RuntimeUnit livingEnemy in allLivingEnemies)
        {
            if (livingEnemy == null)
            {
                continue;
            }

            // Skip if currently visible (already handled above)
            bool isVisible = false;
            foreach (RuntimeUnit visible in visibleEnemies)
            {
                if (visible != null && visible.RuntimeID == livingEnemy.RuntimeID)
                {
                    isVisible = true;
                    break;
                }
            }

            if (isVisible)
            {
                continue;
            }

            // Age the hidden enemy's entry
            if (participantMemory.TryGetValue(livingEnemy.RuntimeID, out MemoryEntry hiddenEntry))
            {
                hiddenEntry.TurnsElapsed++;

                // Mark for expiration if too old
                if (hiddenEntry.TurnsElapsed > PredictionExpirationTurns)
                {
                    entriesToExpire.Add(livingEnemy.RuntimeID);
                }
            }
        }

        // Remove expired entries
        foreach (string expiredUnitID in entriesToExpire)
        {
            participantMemory.Remove(expiredUnitID);
        }
    }

    /// <summary>
    /// Gets predicted positions for all hidden enemies with valid memory entries.
    /// 
    /// Predictions use simple dead-reckoning: project the last observed heading forward
    /// by the number of turns elapsed, capped by the unit's movement budget. Units with
    /// no observed heading (only seen once) are predicted to remain at their last known
    /// position.
    /// 
    /// Predictions are returned only for units with non-expired memory entries.
    /// </summary>
    /// <param name="observer">The AI participant requesting predictions.</param>
    /// <param name="currentTurn">Current turn number (for staleness calculation).</param>
    /// <returns>List of predicted positions for hidden enemies. Empty if no valid predictions exist.</returns>
    public static List<Vector2Int> GetPredictedPositions(RuntimeParticipant observer, int currentTurn)
    {
        var predictions = new List<Vector2Int>();

        if (observer == null)
        {
            return predictions;
        }

        if (!memory.TryGetValue(observer.Participant.ParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
        {
            return predictions;
        }

        foreach (var kvp in participantMemory)
        {
            MemoryEntry entry = kvp.Value;

            // Only predict for units with elapsed time (hidden enemies)
            if (entry.TurnsElapsed <= 0)
            {
                continue;
            }

            // Skip expired entries
            if (entry.TurnsElapsed > PredictionExpirationTurns)
            {
                continue;
            }

            Vector2Int prediction = PredictPosition(entry);
            predictions.Add(prediction);
        }

        return predictions;
    }

    /// <summary>
    /// Predicts the current position of a hidden enemy based on last known data.
    /// 
    /// Uses linear dead-reckoning with movement budget capping:
    /// 1. Calculate heading from previous position to last known position
    /// 2. Project heading forward by TurnsElapsed
    /// 3. Clamp displacement to (MovementBudget * TurnsElapsed) to ensure physical plausibility
    /// 
    /// If no heading is available (unit only seen once), returns last known position.
    /// </summary>
    /// <param name="entry">Memory entry containing sighting history.</param>
    /// <returns>Predicted current position.</returns>
    private static Vector2Int PredictPosition(MemoryEntry entry)
    {
        if (entry.TurnsElapsed <= 0)
        {
            return entry.LastKnownPosition;
        }

        // Calculate observed heading
        Vector2Int heading = entry.LastKnownPosition - entry.PreviousPosition;

        // If no heading (unit hasn't moved between sightings), assume stationary
        if (heading == Vector2Int.zero)
        {
            return entry.LastKnownPosition;
        }

        // Project heading forward by elapsed turns
        Vector2Int rawOffset = heading * entry.TurnsElapsed;

        // Cap displacement by movement budget
        int maxDisplacement = entry.MovementBudget * entry.TurnsElapsed;
        int rawDistance = Mathf.Max(Mathf.Abs(rawOffset.x), Mathf.Abs(rawOffset.y)); // Chebyshev distance

        if (rawDistance > maxDisplacement && rawDistance > 0)
        {
            float scale = maxDisplacement / (float)rawDistance;
            rawOffset = new Vector2Int(
                Mathf.RoundToInt(rawOffset.x * scale),
                Mathf.RoundToInt(rawOffset.y * scale));
        }

        return entry.LastKnownPosition + rawOffset;
    }

    /// <summary>
    /// Clears all memory entries for a specific participant.
    /// 
    /// Should be called when a participant is eliminated or at match end to prevent
    /// state leakage between games.
    /// </summary>
    /// <param name="observer">The participant whose memory should be cleared.</param>
    public static void ClearForParticipant(RuntimeParticipant observer)
    {
        if (observer == null)
        {
            return;
        }

        if (memory.TryGetValue(observer.Participant.ParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
        {
            participantMemory.Clear();
        }
    }

    /// <summary>
    /// Clears all memory entries for all participants.
    /// 
    /// Should be called at the start of a new match to ensure clean state.
    /// </summary>
    public static void ClearAll()
    {
        memory.Clear();
    }
}
