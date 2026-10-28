using System;
using System.Collections.Generic;
using UnityEngine;

// Strategic AI controller that uses tactical subsystems for decision-making.
public sealed class StrategicAdmiralAIController : ParticipantAIController
{
    private readonly struct MovementChoice
    {
        public readonly string RuntimeUnitID;
        public readonly Vector2Int Coordinate;
        public readonly UnitFacing Facing;
        public readonly float Score;

        public MovementChoice(string RuntimeUnitID, Vector2Int Coordinate,
            UnitFacing Facing, float Score)
        {
            this.RuntimeUnitID = RuntimeUnitID;
            this.Coordinate = Coordinate;
            this.Facing = Facing;
            this.Score = Score;
        }
    }

    private readonly struct SearchChoice
    {
        public readonly string RuntimeUnitID;
        public readonly int SystemSlotIndex;
        public readonly UnitFacing Facing;
        public readonly float Score;

        public SearchChoice(string RuntimeUnitID, int SystemSlotIndex,
            UnitFacing Facing, float Score)
        {
            this.RuntimeUnitID = RuntimeUnitID;
            this.SystemSlotIndex = SystemSlotIndex;
            this.Facing = Facing;
            this.Score = Score;
        }
    }

    private readonly struct BattleChoice
    {
        public readonly string RuntimeUnitID;
        public readonly Vector2Int Coordinate;
        public readonly float Score;

        public BattleChoice(string RuntimeUnitID, Vector2Int Coordinate,
            float Score)
        {
            this.RuntimeUnitID = RuntimeUnitID;
            this.Coordinate = Coordinate;
            this.Score = Score;
        }
    }

    [Header("Pacing")]
    [SerializeField, Min(0f)] private float _MinimumDecisionDelay = 0.35f;
    [SerializeField, Min(0f)] private float _MaximumDecisionDelay = 0.75f;
    [SerializeField, Min(0f)] private float _ResolutionReadingDelay = 6f;

    [Header("Simple Strategy")]
    [SerializeField, Range(0f, 1f)]
    private float _UsePreBattleActionChance = 0.65f;

    [SerializeField, Range(0f, 1f)]
    private float _UseDefenseChance = 0.85f;

    [SerializeField, Range(0f, 1f)]
    private float _UseAllySupportChance = 0.65f;

    private GameParticipantAPI.ParticipantAccess _Access;
    private bool _DecisionRequested;
    private float _NextDecisionTime;
    private bool _WaitingOnResolvedEngagement;
    private float _ResolvedEngagementContinueTime;
    private readonly Dictionary<Vector2Int, int> _ExplorationVisits =
        new Dictionary<Vector2Int, int>();
    private readonly Dictionary<string, Vector2Int> _LastUnitPositions =
        new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
    private readonly Dictionary<Vector2Int, int> _SearchCoverage =
        new Dictionary<Vector2Int, int>();
    private int _LastBattleEndDiagnosticTurn = -1;
    private int _LastSearchStallDiagnosticTurn = -1;
    private BattleContext _BattleContext;

    // Formation mode for movement cohesion
    private FormationMode _FormationMode;

       // Strategic subsystems
    private FleetCommander _FleetCommander;
    private FleetSnapshot _LastFleetSnapshot;
    private RuntimeParticipant _OwnParticipant;
    private RuntimeParticipant _EnemyParticipant;

    protected override bool OnInitialize(
        GameParticipantAPI.ParticipantAccess ParticipantAccess,
        out string ValidationMessage)
    {
        _Access = ParticipantAccess;
        _BattleContext = new BattleContext(null, _Access);
        _Access.SnapshotChanged += RequestDecision;
        _Access.SearchPresented += RecordSearchCoverage;

        // Initialize strategic subsystems (only non-static ones)
        _FleetCommander = null; // Will initialize on first snapshot

        RequestDecision();

        Debug.Log(
            $"Strategic Admiral bound to '{_Access.DisplayName}'.",
            this);

        ValidationMessage = string.Empty;
        return true;
    }

    private void OnDestroy()
    {
        if (_Access != null)
        {
            _Access.SnapshotChanged -= RequestDecision;
            _Access.SearchPresented -= RecordSearchCoverage;
        }
    }

    private void Update()
    {
        if (!_DecisionRequested || _Access == null ||
            Time.unscaledTime < _NextDecisionTime)
            return;

        _DecisionRequested = false;

        if (!_Access.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out string SnapshotMessage))
        {
            Debug.LogWarning(SnapshotMessage, this);
            RequestDecision();
            return;
        }

        if (Snapshot?.Match?.MatchComplete == true)
            return;

        GameParticipantAPI.ParticipantCommand Command =
            Decide(Snapshot);

        if (Command == null)
            return;

        GameParticipantAPI.ParticipantCommandResult Result =
            _Access.SubmitCommand(Command);

        if (!Result.Accepted)
        {
            Debug.LogWarning(
                $"Strategic Admiral command {Command.Kind} was rejected: " +
                Result.ValidationMessage,
                this);
            RequestDecision();
        }
    }

    private void RequestDecision()
    {
        _DecisionRequested = true;
        _NextDecisionTime = Time.unscaledTime + UnityEngine.Random.Range(
            Mathf.Min(_MinimumDecisionDelay, _MaximumDecisionDelay),
            Mathf.Max(_MinimumDecisionDelay, _MaximumDecisionDelay));
    }

    private GameParticipantAPI.ParticipantCommand Decide(
        ParticipantGameSnapshot Snapshot)
    {
        if (Snapshot == null)
            return null;

        RecordUnitPositions(Snapshot);

        if (Snapshot.CurrentPhase != GameplayPhase.Battle)
        {
            _WaitingOnResolvedEngagement = false;
            _BattleContext.EngagedTargetsThisPhase?.Clear();
        }

        switch (Snapshot.CurrentPhase)
        {
            case GameplayPhase.Deployment:
                return DecideDeployment(Snapshot);

            case GameplayPhase.Movement:
                return Snapshot.Movement?.CanAct == true
                    ? DecideMovement(Snapshot)
                    : null;

            case GameplayPhase.PreBattle:
                return Snapshot.PreBattle?.CanAct == true
                    ? DecidePreBattle(Snapshot)
                    : null;

            case GameplayPhase.Search:
                return Snapshot.Search?.CanAct == true
                    ? DecideSearch(Snapshot)
                    : null;

            case GameplayPhase.Battle:
                return DecideBattle(Snapshot);

            case GameplayPhase.End:
                if (Snapshot.End?.CanAct == true)
                {
                    GameParticipantAPI.ParticipantCommand command =
                        GameParticipantAPI.ParticipantCommand.FinishEndPhase(Snapshot);
                    ValidateCommandCoherence(command, Snapshot);
                    return command;
                }
                return null;

            default:
                return null;
        }
    }

    private GameParticipantAPI.ParticipantCommand DecideDeployment(
        ParticipantGameSnapshot Snapshot)
    {
        if (Snapshot.DeploymentLocked ||
            !_Access.TryGetDeploymentView(
                out GameDeployment.ParticipantView View) ||
            View == null)
            return null;

        if (View.AllPlaced)
        {
            GameParticipantAPI.ParticipantCommand confirmCommand =
                GameParticipantAPI.ParticipantCommand.ConfirmDeployment(Snapshot);
            ValidateCommandCoherence(confirmCommand, Snapshot);
            return confirmCommand;
        }

        if (!_Access.TryGetDeploymentZoneCoordinates(
                out Vector2Int[] Zone) ||
            Zone == null || Zone.Length == 0)
            return null;

        // Create AITurnContext for planners
        AITurnContext context = new AITurnContext
        {
            OwnParticipant = _OwnParticipant,
            EnemyParticipant = _EnemyParticipant,
            CurrentPhase = GameplayPhase.Deployment,
            TurnNumber = Snapshot.TurnNumber
        };

        _LastFleetSnapshot = _FleetCommander.Evaluate(Snapshot);
        
        GameParticipantAPI.ParticipantCommand command =
            DeploymentPlanner.PlanDeployment(_Access, Snapshot, context, _LastFleetSnapshot);

if (command != null)
        {
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }

        return null;
    }

private GameParticipantAPI.ParticipantCommand DecideMovement(
        ParticipantGameSnapshot Snapshot)
    {
        GameMovement.ParticipantView View = Snapshot.Movement;

        if (View.MovesCommitted >= View.MovementLimit)
        {
            GameParticipantAPI.ParticipantCommand command =
                GameParticipantAPI.ParticipantCommand.EndMovement(Snapshot);
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }

        // Update FleetSnapshot for strategic analysis
        _LastFleetSnapshot = _FleetCommander.Evaluate(Snapshot);

        // Use MovementPrioritizer to identify high-priority units
        List<ParticipantUnitSnapshot> prioritizedUnits =
            MovementPrioritizer.PrioritizeUnits(Snapshot, _Access, _LastFleetSnapshot);

        // Try to plan movement for each prioritized unit
        foreach (ParticipantUnitSnapshot unit in prioritizedUnits)
        {
            if (unit == null)
                continue;

            // Check if unit has already moved this turn
            bool hasMovedThisTurn = false;
            for (int i = 0; i < View.Units.Length; i++)
            {
                if (View.Units[i]?.RuntimeUnitID == unit.RuntimeUnitID &&
                    View.Units[i].HasMoved)
                {
                    hasMovedThisTurn = true;
                    break;
                }
            }

            if (hasMovedThisTurn)
                continue;

            // Use MovementPlanner to find best move for this unit
            AITurnContext context = new AITurnContext
            {
                OwnParticipant = _OwnParticipant,
                EnemyParticipant = _EnemyParticipant,
                CurrentPhase = GameplayPhase.Movement,
                TurnNumber = Snapshot.TurnNumber
            };

            GameParticipantAPI.ParticipantCommand command =
                MovementPlanner.PlanMovement(unit, _LastFleetSnapshot, context, _Access, Snapshot);



        if (command != null)
        {
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }
        }

        // If no strategic move found, end movement phase
        GameParticipantAPI.ParticipantCommand endCommand =
            GameParticipantAPI.ParticipantCommand.EndMovement(Snapshot);
        ValidateCommandCoherence(endCommand, Snapshot);
        return endCommand;
    }

    private GameParticipantAPI.ParticipantCommand DecidePreBattle(
        ParticipantGameSnapshot Snapshot)
    {
        if (Snapshot.PreBattle.ActionsCommitted > 0 ||
            UnityEngine.Random.value > _UsePreBattleActionChance)
        {
            GameParticipantAPI.ParticipantCommand command =
                GameParticipantAPI.ParticipantCommand.EndPreBattle(Snapshot);
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }

        // Update FleetSnapshot to get current stance
        _LastFleetSnapshot = _FleetCommander.Evaluate(Snapshot);
        FleetStance currentStance = _LastFleetSnapshot?.CurrentStance ?? FleetStance.Hunt;

        GameParticipantAPI.ParticipantCommand preBattleCommand =
            PreBattleDecider.DecidePreBattle(Snapshot, _Access, currentStance);

        GameParticipantAPI.ParticipantCommand finalCommand =
            preBattleCommand ?? GameParticipantAPI.ParticipantCommand.EndPreBattle(Snapshot);
        ValidateCommandCoherence(finalCommand, Snapshot);
        return finalCommand;
    }

    private GameParticipantAPI.ParticipantCommand DecideSearch(
        ParticipantGameSnapshot Snapshot)
    {
        GameSearch.ParticipantView View = Snapshot.Search;

        if (View.SearchesCommitted >= View.SearchLimit)
        {
            GameParticipantAPI.ParticipantCommand command =
                GameParticipantAPI.ParticipantCommand.EndSearch(Snapshot);
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }

        // Update FleetSnapshot for strategic analysis
        _LastFleetSnapshot = _FleetCommander.Evaluate(Snapshot);

        // Use SearchPlanner to find best search action
        GameParticipantAPI.ParticipantCommand searchCommand =
            SearchPlanner.PlanSearch(_Access, Snapshot, _LastFleetSnapshot);

        if (searchCommand != null)
        {
            ValidateCommandCoherence(searchCommand, Snapshot);
            return searchCommand;
        }

        // If no strategic search found, check if we can end search
        if (View.CanEndSearch)
        {
            GameParticipantAPI.ParticipantCommand command =
                GameParticipantAPI.ParticipantCommand.EndSearch(Snapshot);
            ValidateCommandCoherence(command, Snapshot);
            return command;
        }

        // Diagnostic logging if stuck
        if (!View.CanEndSearch &&
            _LastSearchStallDiagnosticTurn != Snapshot.TurnNumber)
        {
            _LastSearchStallDiagnosticTurn = Snapshot.TurnNumber;
            Debug.Log(
                $"Strategic Admiral cannot complete Search on Turn {Snapshot.TurnNumber}: " +
                "SearchPlanner found no valid search actions. The phase timer will resolve the remaining quota.",
                this);
        }

        return null;
    }

    private GameParticipantAPI.ParticipantCommand DecideBattle(
        ParticipantGameSnapshot Snapshot)
    {
        GameBattle.ParticipantView View = Snapshot.Battle;
        if (View == null)
            return null;

        _BattleContext.Snapshot = Snapshot;
        _BattleContext.Access = _Access;

        if (View.AttacksCommitted == 0)
        {
            _BattleContext.EngagedTargetsThisPhase?.Clear();
        }

        GameParticipantAPI.ParticipantCommand command =
            BattleDecider.DecideBattle(Snapshot, _Access, _BattleContext);

        ValidateCommandCoherence(command, Snapshot);
        return command;
    }

    private void ValidateCommandCoherence(
        GameParticipantAPI.ParticipantCommand Command,
        ParticipantGameSnapshot Snapshot)
    {
        if (Command == null || Snapshot == null)
            return;

        bool phaseMismatch = Command.ExpectedPhase != Snapshot.CurrentPhase;
        bool turnMismatch = Command.ExpectedTurnNumber != Snapshot.TurnNumber;

        if (phaseMismatch)
        {
            Debug.LogWarning(
                $"Command phase coherence violation: Command expects {Command.ExpectedPhase} " +
                $"but snapshot shows {Snapshot.CurrentPhase} (Turn {Snapshot.TurnNumber})",
                this);
        }

        if (turnMismatch)
        {
            Debug.LogWarning(
                $"Command turn coherence violation: Command expects Turn {Command.ExpectedTurnNumber} " +
                $"but snapshot shows Turn {Snapshot.TurnNumber} (Phase {Snapshot.CurrentPhase})",
                this);
        }
    }

    private static ParticipantUnitSnapshot FindOwnUnit(
        ParticipantGameSnapshot Snapshot,
        string RuntimeUnitID)
    {
        for (int i = 0; i < Snapshot.OwnUnits.Length; i++)
        {
            if (Snapshot.OwnUnits[i]?.RuntimeUnitID == RuntimeUnitID)
                return Snapshot.OwnUnits[i];
        }

        return null;
    }

    private static List<Vector2Int> GetKnownTargetCoordinates(
        ParticipantGameSnapshot Snapshot)
    {
        HashSet<Vector2Int> Coordinates = new HashSet<Vector2Int>();

        for (int i = 0; i < Snapshot.Contacts.Length; i++)
            Coordinates.Add(Snapshot.Contacts[i].Coordinate);

        for (int i = 0; i < Snapshot.VisibleEnemies.Length; i++)
        {
            Vector2Int[] Footprint =
                Snapshot.VisibleEnemies[i].OccupiedCoordinates;

            for (int c = 0; c < Footprint.Length; c++)
                Coordinates.Add(Footprint[c]);
        }

        return new List<Vector2Int>(Coordinates);
    }

    private static int ClosestManhattanDistance(
        Vector2Int Coordinate,
        List<Vector2Int> Targets)
    {
        int Best = int.MaxValue;
        for (int i = 0; i < Targets.Count; i++)
        {
            int Distance =
                Mathf.Abs(Coordinate.x - Targets[i].x) +
                Mathf.Abs(Coordinate.y - Targets[i].y);
            Best = Mathf.Min(Best, Distance);
        }

        return Best == int.MaxValue ? 0 : Best;
    }

    private void RecordUnitPositions(ParticipantGameSnapshot Snapshot)
    {
        for (int i = 0; i < Snapshot.OwnUnits.Length; i++)
        {
            ParticipantUnitSnapshot Unit = Snapshot.OwnUnits[i];
            if (Unit == null || !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            if (_LastUnitPositions.TryGetValue(
                    Unit.RuntimeUnitID,
                    out Vector2Int Previous) &&
                Previous == Unit.MovementPivotCoordinate)
                continue;

            _LastUnitPositions[Unit.RuntimeUnitID] =
                Unit.MovementPivotCoordinate;

            _ExplorationVisits.TryGetValue(
                Unit.MovementPivotCoordinate,
                out int Visits);
            _ExplorationVisits[Unit.MovementPivotCoordinate] = Visits + 1;
        }
    }

    private void RecordSearchCoverage(GameSearch.CommitResult Result)
    {
        if (Result?.Tiles == null)
            return;

        for (int i = 0; i < Result.Tiles.Length; i++)
        {
            Vector2Int Coordinate = Result.Tiles[i].Coordinate;
            _SearchCoverage.TryGetValue(Coordinate, out int Searches);
            _SearchCoverage[Coordinate] = Searches + 1;
        }
    }

    private static List<Vector2Int> GetUnexploredCoordinates(
        ParticipantGameSnapshot Snapshot)
    {
        List<Vector2Int> Coordinates = new List<Vector2Int>();
        for (int i = 0; i < Snapshot.Tiles.Length; i++)
        {
            if (Snapshot.Tiles[i].Visibility ==
                ParticipantTileVisibility.Fog)
            {
                Coordinates.Add(Snapshot.Tiles[i].Coordinate);
            }
        }
        return Coordinates;
    }

    private float ScoreExplorationDestination(
        Vector2Int Coordinate,
        List<Vector2Int> ExplorationTargets)
    {
        _ExplorationVisits.TryGetValue(Coordinate, out int Visits);

        float Score = -Visits * 12f;
        if (ExplorationTargets != null && ExplorationTargets.Count > 0)
        {
            Score += 80f - ClosestManhattanDistance(
                Coordinate,
                ExplorationTargets) * 2f;
        }

        return Score;
    }

    private static Dictionary<Vector2Int, ParticipantTileVisibility>
        BuildTileVisibility(ParticipantGameSnapshot Snapshot)
    {
        Dictionary<Vector2Int, ParticipantTileVisibility> Result =
            new Dictionary<Vector2Int, ParticipantTileVisibility>();

        for (int i = 0; i < Snapshot.Tiles.Length; i++)
        {
            Result[Snapshot.Tiles[i].Coordinate] =
                Snapshot.Tiles[i].Visibility;
        }

        return Result;
    }

    private static ParticipantTileVisibility GetTileVisibility(
        Dictionary<Vector2Int, ParticipantTileVisibility> TileVisibility,
        Vector2Int Coordinate)
    {
        return TileVisibility.TryGetValue(Coordinate, out var Visibility)
            ? Visibility
            : ParticipantTileVisibility.Fog;
    }

    private void LogBattleEndReason(
        ParticipantGameSnapshot Snapshot,
        int KnownTargetCount,
        int ReadyAttackerCount)
    {
        if (_LastBattleEndDiagnosticTurn == Snapshot.TurnNumber)
            return;

        _LastBattleEndDiagnosticTurn = Snapshot.TurnNumber;

        string Reason;
        if (ReadyAttackerCount == 0)
        {
            Reason = "no active Unit remained available to attack";
        }
        else if (KnownTargetCount == 0)
        {
            Reason =
                "it had no Search contact or absolute-visible enemy target";
        }
        else
        {
            Reason =
                $"none of its {ReadyAttackerCount} ready Unit(s) had a known target " +
                "inside legal Fire Control with a compatible weapon or attack ability";
        }

        Debug.Log(
            $"Strategic Admiral ended Battle on Turn {Snapshot.TurnNumber}: {Reason}.",
            this);
    }

    private static MovementChoice HighestScoringMovement(
        List<MovementChoice> Choices)
    {
        MovementChoice Best = Choices[0];
        for (int i = 1; i < Choices.Count; i++)
        {
            if (Choices[i].Score > Best.Score)
                Best = Choices[i];
        }
        return Best;
    }

    private static SearchChoice HighestScoringSearch(
        List<SearchChoice> Choices)
    {
        SearchChoice Best = Choices[0];
        for (int i = 1; i < Choices.Count; i++)
        {
            if (Choices[i].Score > Best.Score)
                Best = Choices[i];
        }
        return Best;
    }

    private static BattleChoice HighestScoringBattle(
        List<BattleChoice> Choices)
    {
        BattleChoice Best = Choices[0];
        for (int i = 1; i < Choices.Count; i++)
        {
            if (Choices[i].Score > Best.Score)
                Best = Choices[i];
        }
        return Best;
    }

    private static GameBattle.ActionOption ChooseAction(
        GameBattle.ActionOption[] Options)
    {
        return Options != null && Options.Length > 0
            ? Options[UnityEngine.Random.Range(0, Options.Length)]
            : null;
    }
}


