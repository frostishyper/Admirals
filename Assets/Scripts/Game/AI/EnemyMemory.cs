using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Admirals.AI
{
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
    /// 
    /// ARCHITECTURE NOTE: Refactored to work with ParticipantGameSnapshot data only.
    /// No direct access to GameMap or RuntimeUnit references.
    /// </summary>
    public static class EnemyMemory
    {
        /// <summary>
        /// Internal storage for a single enemy sighting record.
        /// Tracks position history, movement capability, and staleness.
        /// </summary>
        private class MemoryEntry
        {
            /// <summary>The enemy unit being tracked (snapshot reference).</summary>
            public ParticipantUnitSnapshot Unit { get; set; }

            /// <summary>RuntimeID of the tracked unit for correlation across snapshots.</summary>
            public string UnitRuntimeID { get; set; }

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
        /// First key: Observing participant's ID
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
        /// <param name="snapshot">Current game state snapshot visible to the observer.</param>
        public static void UpdateContacts(
            RuntimeParticipant observer,
            ParticipantGameSnapshot snapshot)
        {
            if (observer == null || snapshot == null)
            {
                Debug.LogWarning("[AI][Memory] UpdateContacts called with null observer or snapshot");
                return;
            }

            string observerParticipantID = observer.Participant.ParticipantID;

            // Ensure participant-specific storage exists
            if (!memory.TryGetValue(observerParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
            {
                participantMemory = new Dictionary<string, MemoryEntry>();
                memory[observerParticipantID] = participantMemory;
            }

            // Extract visible enemies from snapshot
            // VisibleEnemies contains units visible via fog-of-war
            // Contacts contains units detected via search (scanner contacts)
            // We track both types
            var visibleEnemies = new List<ParticipantUnitSnapshot>();
            
            // Add visible enemies (from VisibleEnemies array)
            if (snapshot.VisibleEnemies != null)
            {
                foreach (var visibleEnemy in snapshot.VisibleEnemies)
                {
                    if (visibleEnemy == null) continue;
                    
                    // Convert VisibleEnemyUnitSnapshot to tracking format
                    // We need to track by a consistent identifier
                    // VisibleEnemyUnitSnapshot doesn't expose RuntimeID directly for security
                    // We'll use InstanceName + DefinitionID as a composite key
                    string trackingID = GetTrackingID(visibleEnemy);
                    
                    UpdateVisibleEnemy(participantMemory, trackingID, visibleEnemy, snapshot.TurnNumber);
                }
            }

            // Track units from Contacts array as well
            if (snapshot.Contacts != null)
            {
                foreach (var contact in snapshot.Contacts)
                {
                    if (contact == null) continue;
                    
                    string trackingID = GetTrackingIDFromContact(contact);
                    
                    UpdateContactEnemy(participantMemory, trackingID, contact, snapshot.TurnNumber);
                }
            }

            // Age entries that are no longer visible
            // Determine which entries need aging by checking against current visible set
            var currentlyVisibleIDs = new HashSet<string>();
            
            if (snapshot.VisibleEnemies != null)
            {
                foreach (var visibleEnemy in snapshot.VisibleEnemies)
                {
                    if (visibleEnemy != null)
                    {
                        currentlyVisibleIDs.Add(GetTrackingID(visibleEnemy));
                    }
                }
            }
            
            if (snapshot.Contacts != null)
            {
                foreach (var contact in snapshot.Contacts)
                {
                    if (contact != null)
                    {
                        currentlyVisibleIDs.Add(GetTrackingIDFromContact(contact));
                    }
                }
            }

            // Age hidden entries and mark expired ones for removal
            var entriesToExpire = new List<string>();

            foreach (var kvp in participantMemory)
            {
                string trackedID = kvp.Key;
                MemoryEntry entry = kvp.Value;

                if (!currentlyVisibleIDs.Contains(trackedID))
                {
                    // Unit is hidden - age the entry
                    entry.TurnsElapsed++;

                    // Mark for expiration if too old
                    if (entry.TurnsElapsed > PredictionExpirationTurns)
                    {
                        entriesToExpire.Add(trackedID);
                    }
                }
            }

            // Remove expired entries
            foreach (string expiredID in entriesToExpire)
            {
                participantMemory.Remove(expiredID);
            }
        }

        /// <summary>
        /// Generates a tracking ID for a VisibleEnemyUnitSnapshot.
        /// Uses InstanceName + DefinitionID as composite key.
        /// </summary>
        private static string GetTrackingID(VisibleEnemyUnitSnapshot enemy)
        {
            return $"{enemy.InstanceName}_{enemy.DefinitionID}";
        }

        /// <summary>
        /// Generates a tracking ID for a ParticipantContactSnapshot.
        /// </summary>
        private static string GetTrackingIDFromContact(ParticipantContactSnapshot contact)
        {
            // Contacts may have different data structure - adapt as needed
            return $"{contact.Coordinate.x}_{contact.Coordinate.y}_{contact.Domain}";
        }

        /// <summary>
        /// Updates or creates a memory entry for a visible enemy.
        /// </summary>
        private static void UpdateVisibleEnemy(
            Dictionary<string, MemoryEntry> participantMemory,
            string trackingID,
            VisibleEnemyUnitSnapshot visibleEnemy,
            int currentTurn)
        {
            // Get current position directly from snapshot
            Vector2Int currentPosition = visibleEnemy.MovementPivotCoordinate;

            if (participantMemory.TryGetValue(trackingID, out MemoryEntry existing))
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
                existing.Unit = null; // VisibleEnemyUnitSnapshot doesn't fit ParticipantUnitSnapshot
                existing.UnitRuntimeID = trackingID;
            }
            else
            {
                // Create new entry for first-time sighting
                participantMemory[trackingID] = new MemoryEntry
                {
                    Unit = null, // VisibleEnemyUnitSnapshot doesn't fit ParticipantUnitSnapshot
                    UnitRuntimeID = trackingID,
                    LastKnownPosition = currentPosition,
                    PreviousPosition = currentPosition, // No heading available yet
                    LastKnownFacing = visibleEnemy.Facing,
                    MovementBudget = visibleEnemy.Definition?.MovementBudget ?? 0,
                    LastSeenTurn = currentTurn,
                    TurnsElapsed = 0
                };
            }
        }

        /// <summary>
        /// Updates or creates a memory entry for a contact.
        /// </summary>
        private static void UpdateContactEnemy(
            Dictionary<string, MemoryEntry> participantMemory,
            string trackingID,
            ParticipantContactSnapshot contact,
            int currentTurn)
        {
            Vector2Int contactPosition = contact.Coordinate;

            if (participantMemory.TryGetValue(trackingID, out MemoryEntry existing))
            {
                // Update existing entry
                if (existing.LastKnownPosition != contactPosition)
                {
                    existing.PreviousPosition = existing.LastKnownPosition;
                }

                existing.LastKnownPosition = contactPosition;
                existing.LastSeenTurn = currentTurn;
                existing.TurnsElapsed = 0;
            }
            else
            {
                // Create new entry - contacts don't have facing or movement budget
                participantMemory[trackingID] = new MemoryEntry
                {
                    Unit = null,
                    UnitRuntimeID = trackingID,
                    LastKnownPosition = contactPosition,
                    PreviousPosition = contactPosition,
                    LastKnownFacing = UnitFacing.Up, // Default
                    MovementBudget = 0, // Unknown
                    LastSeenTurn = currentTurn,
                    TurnsElapsed = 0
                };
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
        /// <param name="snapshot">Current game snapshot for map bounds calculation.</param>
        /// <returns>List of predicted positions for hidden enemies. Empty if no valid predictions exist.</returns>
        public static List<Vector2Int> GetPredictedPositions(
            RuntimeParticipant observer,
            ParticipantGameSnapshot snapshot)
        {
            var predictions = new List<Vector2Int>();

            if (observer == null || snapshot == null)
            {
                return predictions;
            }

            string observerParticipantID = observer.Participant.ParticipantID;

            if (!memory.TryGetValue(observerParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
            {
                return predictions;
            }

            // Calculate map bounds from snapshot tiles for position clamping
            int mapWidth = CalculateMapWidth(snapshot.Tiles);
            int mapHeight = CalculateMapHeight(snapshot.Tiles);

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

                Vector2Int prediction = PredictPosition(entry, mapWidth, mapHeight);
                predictions.Add(prediction);
            }

            return predictions;
        }

        /// <summary>
        /// Legacy overload for backward compatibility.
        /// This overload will be removed once all callers are updated to pass ParticipantGameSnapshot.
        /// </summary>
        /// <param name="observer">The AI participant requesting predictions.</param>
        /// <param name="currentTurn">Current turn number (not used in new implementation).</param>
        /// <returns>List of predicted positions for hidden enemies. Empty if no valid predictions exist.</returns>
        [System.Obsolete("Use GetPredictedPositions(RuntimeParticipant, ParticipantGameSnapshot) instead")]
        public static List<Vector2Int> GetPredictedPositions(
            RuntimeParticipant observer,
            int currentTurn)
        {
            // Return predictions without map bounds clamping (legacy behavior)
            var predictions = new List<Vector2Int>();

            if (observer == null)
            {
                return predictions;
            }

            string observerParticipantID = observer.Participant.ParticipantID;

            if (!memory.TryGetValue(observerParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
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

                // Use legacy prediction without map bounds (pass 0 for width/height to skip clamping)
                Vector2Int prediction = PredictPosition(entry, 0, 0);
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
        /// 4. Clamp to map bounds
        /// 
        /// If no heading is available (unit only seen once), returns last known position.
        /// </summary>
        /// <param name="entry">Memory entry containing sighting history.</param>
        /// <param name="mapWidth">Map width for bounds clamping.</param>
        /// <param name="mapHeight">Map height for bounds clamping.</param>
        /// <returns>Predicted current position.</returns>
        private static Vector2Int PredictPosition(MemoryEntry entry, int mapWidth, int mapHeight)
        {
            if (entry.TurnsElapsed <= 0)
            {
                return entry.LastKnownPosition;
            }

            // Calculate observed heading using Vector2Int arithmetic
            Vector2Int heading = entry.LastKnownPosition - entry.PreviousPosition;

            // If no heading (unit hasn't moved between sightings), assume stationary
            if (heading == Vector2Int.zero)
            {
                return entry.LastKnownPosition;
            }

            // Project heading forward by elapsed turns
            Vector2Int rawOffset = heading * entry.TurnsElapsed;

            // Cap displacement by movement budget using Manhattan distance
            if (entry.MovementBudget > 0)
            {
                int maxDisplacement = entry.MovementBudget * entry.TurnsElapsed;
                int rawDistance = Mathf.Abs(rawOffset.x) + Mathf.Abs(rawOffset.y); // Manhattan distance

                if (rawDistance > maxDisplacement && rawDistance > 0)
                {
                    float scale = maxDisplacement / (float)rawDistance;
                    rawOffset = new Vector2Int(
                        Mathf.RoundToInt(rawOffset.x * scale),
                        Mathf.RoundToInt(rawOffset.y * scale));
                }
            }

            Vector2Int predicted = entry.LastKnownPosition + rawOffset;

            // Clamp to map bounds
            if (mapWidth > 0 && mapHeight > 0)
            {
                predicted.x = Mathf.Clamp(predicted.x, 0, mapWidth - 1);
                predicted.y = Mathf.Clamp(predicted.y, 0, mapHeight - 1);
            }

            return predicted;
        }

        /// <summary>
        /// Calculates map width from snapshot tiles.
        /// </summary>
        private static int CalculateMapWidth(ParticipantTileSnapshot[] tiles)
        {
            if (tiles == null || tiles.Length == 0) return 0;
            return tiles.Max(t => t.Coordinate.x) + 1;
        }

        /// <summary>
        /// Calculates map height from snapshot tiles.
        /// </summary>
        private static int CalculateMapHeight(ParticipantTileSnapshot[] tiles)
        {
            if (tiles == null || tiles.Length == 0) return 0;
            return tiles.Max(t => t.Coordinate.y) + 1;
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

            string observerParticipantID = observer.Participant.ParticipantID;

            if (memory.TryGetValue(observerParticipantID, out Dictionary<string, MemoryEntry> participantMemory))
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
}
