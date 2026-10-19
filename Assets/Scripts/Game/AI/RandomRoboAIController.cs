using System;
using System.Collections.Generic;
using UnityEngine;


// Baseline legal-action AI used to exercise the complete participant API.
// It deliberately remains simple: random legal choices are biased toward
// known enemies, usable actions and attacks rather than being purely idle.
public sealed class RandomRoboAIController : ParticipantAIController
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


    protected override bool OnInitialize(
        GameParticipantAPI.ParticipantAccess ParticipantAccess,
        out string ValidationMessage)
    {
        _Access = ParticipantAccess;
        _Access.SnapshotChanged += RequestDecision;
        _Access.SearchPresented += RecordSearchCoverage;
        RequestDecision();

        Debug.Log(
            $"Random Robo bound to '{_Access.DisplayName}'.",
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
                $"Random Robo command {Command.Kind} was rejected: " +
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
            _WaitingOnResolvedEngagement = false;

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
                return Snapshot.End?.CanAct == true
                    ? GameParticipantAPI.ParticipantCommand
                        .FinishEndPhase(Snapshot)
                    : null;

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
            return GameParticipantAPI.ParticipantCommand
                .ConfirmDeployment(Snapshot);
        }

        if (!_Access.TryGetDeploymentZoneCoordinates(
                out Vector2Int[] Zone) ||
            Zone == null || Zone.Length == 0)
            return null;

        _Access.TryGetInitialDeploymentFacing(
            out UnitFacing InitialFacing);

        int PlacementStart = UnityEngine.Random.Range(
            0, Math.Max(1, View.Placements.Count));
        int CoordinateStart = UnityEngine.Random.Range(0, Zone.Length);
        int FacingStart = UnityEngine.Random.Range(0, 4);

        for (int p = 0; p < View.Placements.Count; p++)
        {
            GameDeployment.PlacementView Placement =
                View.Placements[(PlacementStart + p) % View.Placements.Count];

            if (Placement == null || Placement.IsPlaced)
                continue;

            for (int c = 0; c < Zone.Length; c++)
            {
                Vector2Int Coordinate =
                    Zone[(CoordinateStart + c) % Zone.Length];

                for (int f = 0; f < 4; f++)
                {
                    UnitFacing Facing = (UnitFacing)(
                        ((int)InitialFacing + FacingStart + f) % 4);

                    if (_Access.ValidateDeploymentPlacement(
                            Placement.ID, Coordinate, Facing, out _))
                    {
                        return GameParticipantAPI.ParticipantCommand
                            .PlaceDeployment(
                                Snapshot,
                                Placement.ID,
                                Coordinate,
                                Facing);
                    }
                }
            }
        }

        return null;
    }


    private GameParticipantAPI.ParticipantCommand DecideMovement(
        ParticipantGameSnapshot Snapshot)
    {
        GameMovement.ParticipantView View = Snapshot.Movement;

        if (View.MovesCommitted >= View.MovementLimit)
            return GameParticipantAPI.ParticipantCommand.EndMovement(Snapshot);

        List<Vector2Int> KnownTargets = GetKnownTargetCoordinates(Snapshot);
        List<Vector2Int> ExplorationTargets =
            KnownTargets.Count == 0
                ? GetUnexploredCoordinates(Snapshot)
                : null;
        List<MovementChoice> Choices = new List<MovementChoice>();

        for (int i = 0; i < View.Units.Length; i++)
        {
            GameMovement.UnitView UnitView = View.Units[i];
            if (UnitView == null || UnitView.HasMoved)
                continue;

            ParticipantUnitSnapshot Unit =
                FindOwnUnit(Snapshot, UnitView.RuntimeUnitID);

            if (Unit == null || !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active ||
                !_Access.TryGetMovementArea(
                    Unit.RuntimeUnitID,
                    out Vector2Int[] Area,
                    out _))
                continue;

            for (int c = 0; c < Area.Length; c++)
            {
                for (int f = 0; f < 4; f++)
                {
                    UnitFacing Facing = (UnitFacing)f;
                    if (!_Access.ValidateMovement(
                            Unit.RuntimeUnitID,
                            Area[c],
                            Facing,
                            out _,
                            out _))
                        continue;

                    float Score = UnityEngine.Random.Range(0f, 4f);
                    if (KnownTargets.Count > 0)
                    {
                        Score += 100f - ClosestManhattanDistance(
                            Area[c], KnownTargets);
                    }
                    else
                    {
                        Score += ScoreExplorationDestination(
                            Area[c], ExplorationTargets);
                    }

                    if (Area[c] != Unit.MovementPivotCoordinate)
                        Score += 2f;

                    Choices.Add(new MovementChoice(
                        Unit.RuntimeUnitID, Area[c], Facing, Score));
                }
            }
        }

        if (Choices.Count == 0)
            return GameParticipantAPI.ParticipantCommand.EndMovement(Snapshot);

        MovementChoice Choice = HighestScoringMovement(Choices);
        return GameParticipantAPI.ParticipantCommand.CommitMovement(
            Snapshot,
            Choice.RuntimeUnitID,
            Choice.Coordinate,
            Choice.Facing);
    }


    private GameParticipantAPI.ParticipantCommand DecidePreBattle(
        ParticipantGameSnapshot Snapshot)
    {
        if (Snapshot.PreBattle.ActionsCommitted > 0 ||
            UnityEngine.Random.value > _UsePreBattleActionChance)
        {
            return GameParticipantAPI.ParticipantCommand
                .EndPreBattle(Snapshot);
        }

        List<GameParticipantAPI.ParticipantCommand> Choices =
            new List<GameParticipantAPI.ParticipantCommand>();

        for (int u = 0; u < Snapshot.OwnUnits.Length; u++)
        {
            ParticipantUnitSnapshot Unit = Snapshot.OwnUnits[u];
            if (Unit == null || !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            for (int s = 0; s < Unit.Systems.Length; s++)
            {
                ParticipantSystemSnapshot System = Unit.Systems[s];
                if (System?.Definition == null ||
                    (System.Definition.OperationalPhases &
                        GameplayPhase.PreBattle) == 0 ||
                    (System.Definition.UsesCharges &&
                        System.CurrentCharges <= 0))
                    continue;

                if (_Access.TryGetMineDeploymentArea(
                        Unit.RuntimeUnitID,
                        System.SlotIndex,
                        out Vector2Int[] MineArea,
                        out _) &&
                    MineArea.Length > 0)
                {
                    Vector2Int Coordinate = MineArea[
                        UnityEngine.Random.Range(0, MineArea.Length)];

                    if (_Access.ValidateMineDeployment(
                            Unit.RuntimeUnitID,
                            System.SlotIndex,
                            Coordinate,
                            out _))
                    {
                        Choices.Add(GameParticipantAPI.ParticipantCommand
                            .DeployMine(
                                Snapshot,
                                Unit.RuntimeUnitID,
                                System.SlotIndex,
                                Coordinate));
                    }
                }

                for (int c = 0; c < System.CarriedUnits.Length; c++)
                {
                    ParticipantCarriedUnitSnapshot Carried =
                        System.CarriedUnits[c];

                    if (Carried == null ||
                        Carried.State != RuntimeCarriedUnitState.Carried)
                        continue;

                    UnitFacing Facing = (UnitFacing)
                        UnityEngine.Random.Range(0, 4);

                    if (!_Access.TryGetUnitDeploymentArea(
                            Unit.RuntimeUnitID,
                            System.SlotIndex,
                            Carried.CarriedUnitIndex,
                            Facing,
                            out Vector2Int[] DeploymentArea,
                            out _) ||
                        DeploymentArea.Length == 0)
                        continue;

                    Vector2Int Coordinate = DeploymentArea[
                        UnityEngine.Random.Range(0, DeploymentArea.Length)];

                    if (_Access.ValidateUnitDeployment(
                            Unit.RuntimeUnitID,
                            System.SlotIndex,
                            Carried.CarriedUnitIndex,
                            Coordinate,
                            Facing,
                            out _))
                    {
                        Choices.Add(GameParticipantAPI.ParticipantCommand
                            .DeployCarriedUnit(
                                Snapshot,
                                Unit.RuntimeUnitID,
                                System.SlotIndex,
                                Carried.CarriedUnitIndex,
                                Coordinate,
                                Facing));
                    }
                }
            }
        }

        return Choices.Count > 0
            ? Choices[UnityEngine.Random.Range(0, Choices.Count)]
            : GameParticipantAPI.ParticipantCommand.EndPreBattle(Snapshot);
    }


    private GameParticipantAPI.ParticipantCommand DecideSearch(
        ParticipantGameSnapshot Snapshot)
    {
        GameSearch.ParticipantView View = Snapshot.Search;

        if (View.SearchesCommitted >= View.SearchLimit && View.CanEndSearch)
            return GameParticipantAPI.ParticipantCommand.EndSearch(Snapshot);

        HashSet<Vector2Int> KnownTargets =
            new HashSet<Vector2Int>(GetKnownTargetCoordinates(Snapshot));
        Dictionary<Vector2Int, ParticipantTileVisibility> TileVisibility =
            BuildTileVisibility(Snapshot);
        List<SearchChoice> Choices = new List<SearchChoice>();

        for (int i = 0; i < View.Units.Length; i++)
        {
            GameSearch.UnitView UnitView = View.Units[i];
            if (UnitView == null || UnitView.HasSearched)
                continue;

            ParticipantUnitSnapshot Unit =
                FindOwnUnit(Snapshot, UnitView.RuntimeUnitID);

            if (Unit == null || !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            for (int s = 0; s < Unit.Systems.Length; s++)
            {
                ParticipantSystemSnapshot System = Unit.Systems[s];
                if (System?.Definition?.Search == null ||
                    !System.Definition.Search.Enabled ||
                    (System.Definition.OperationalPhases &
                        GameplayPhase.Search) == 0 ||
                    (System.Definition.UsesCharges &&
                        System.CurrentCharges <= 0))
                    continue;

                for (int f = 0; f < 4; f++)
                {
                    UnitFacing Facing = (UnitFacing)f;
                    if (!_Access.TryGetSearchPattern(
                            Unit.RuntimeUnitID,
                            System.SlotIndex,
                            Facing,
                            out GameSearch.SearchTile[] Tiles,
                            out _) ||
                        Tiles.Length == 0)
                        continue;

                    float Score = UnityEngine.Random.Range(0f, 5f);
                    for (int t = 0; t < Tiles.Length; t++)
                    {
                        if (KnownTargets.Contains(Tiles[t].Coordinate))
                            Score += 100f + Tiles[t].Strength;

                        _SearchCoverage.TryGetValue(
                            Tiles[t].Coordinate,
                            out int PreviousSearches);

                        // Prefer broad, previously unsearched coverage when
                        // there is no known target to focus on.
                        Score += 4f / (1f + PreviousSearches);

                        ParticipantTileVisibility Visibility =
                            GetTileVisibility(
                                TileVisibility,
                                Tiles[t].Coordinate);

                        if (Visibility == ParticipantTileVisibility.Fog)
                            Score += 2f;
                        else if (Visibility == ParticipantTileVisibility.Obscured)
                            Score += 0.5f;
                    }

                    Choices.Add(new SearchChoice(
                        Unit.RuntimeUnitID,
                        System.SlotIndex,
                        Facing,
                        Score));
                }
            }
        }

        if (Choices.Count == 0)
        {
            if (!View.CanEndSearch &&
                _LastSearchStallDiagnosticTurn != Snapshot.TurnNumber)
            {
                _LastSearchStallDiagnosticTurn = Snapshot.TurnNumber;
                Debug.Log(
                    $"Random Robo cannot complete Search on Turn {Snapshot.TurnNumber}: " +
                    "no unused Unit has a legal sensor pattern. The phase timer will resolve the remaining quota.",
                    this);
            }

            return View.CanEndSearch
                ? GameParticipantAPI.ParticipantCommand.EndSearch(Snapshot)
                : null;
        }

        SearchChoice Choice = HighestScoringSearch(Choices);
        return GameParticipantAPI.ParticipantCommand.CommitSearch(
            Snapshot,
            Choice.RuntimeUnitID,
            Choice.SystemSlotIndex,
            Choice.Facing);
    }


    private GameParticipantAPI.ParticipantCommand DecideBattle(
        ParticipantGameSnapshot Snapshot)
    {
        GameBattle.ParticipantView View = Snapshot.Battle;
        if (View == null)
            return null;

        GameBattle.EngagementView Engagement = View.Engagement;
        if (Engagement?.IsActive == true)
        {
            if (Engagement.Resolved)
            {
                if (!Engagement.IsAttacker)
                    return null;

                if (!_WaitingOnResolvedEngagement)
                {
                    _WaitingOnResolvedEngagement = true;
                    _ResolvedEngagementContinueTime =
                        Time.unscaledTime + _ResolutionReadingDelay;
                    _DecisionRequested = true;
                    _NextDecisionTime = _ResolvedEngagementContinueTime;
                    return null;
                }

                if (Time.unscaledTime < _ResolvedEngagementContinueTime)
                {
                    _DecisionRequested = true;
                    _NextDecisionTime = _ResolvedEngagementContinueTime;
                    return null;
                }

                _WaitingOnResolvedEngagement = false;
                return GameParticipantAPI.ParticipantCommand
                    .ContinueEngagement(Snapshot);
            }

            _WaitingOnResolvedEngagement = false;

            if (Engagement.IsAttacker && !Engagement.AttackerLocked)
            {
                GameBattle.ActionOption Attack =
                    ChooseAction(Engagement.AttackerOptions);

                return Attack != null
                    ? GameParticipantAPI.ParticipantCommand.LockBattleAttack(
                        Snapshot, Attack.Kind, Attack.SlotIndex)
                    : GameParticipantAPI.ParticipantCommand
                        .CancelEngagement(Snapshot);
            }

            if (!Engagement.IsAttacker && Engagement.AttackerLocked &&
                !Engagement.DefenderLocked)
            {
                GameBattle.ActionOption Defense =
                    UnityEngine.Random.value <= _UseDefenseChance
                        ? ChooseAction(Engagement.DefenderOptions)
                        : null;

                GameBattle.AllySupportGroup SupportGroup = null;
                GameBattle.ActionOption Support = null;

                if (UnityEngine.Random.value <= _UseAllySupportChance &&
                    Engagement.AllySupport.Length > 0)
                {
                    SupportGroup = Engagement.AllySupport[
                        UnityEngine.Random.Range(
                            0, Engagement.AllySupport.Length)];
                    Support = ChooseAction(SupportGroup.Options);
                }

                return GameParticipantAPI.ParticipantCommand
                    .LockBattleDefense(
                        Snapshot,
                        Defense?.Kind ?? GameBattle.CombatActionKind.None,
                        Defense?.SlotIndex ?? -1,
                        SupportGroup?.RuntimeUnitID ?? string.Empty,
                        Support?.Kind ?? GameBattle.CombatActionKind.None,
                        Support?.SlotIndex ?? -1);
            }

            return null;
        }

        _WaitingOnResolvedEngagement = false;

        if (!View.CanAct)
            return null;

        List<Vector2Int> Targets = GetKnownTargetCoordinates(Snapshot);
        List<BattleChoice> Choices = new List<BattleChoice>();
        int ReadyAttackerCount = 0;

        for (int u = 0; u < View.Units.Length; u++)
        {
            GameBattle.UnitView Attacker = View.Units[u];
            if (Attacker == null || Attacker.HasAttacked)
                continue;

            ParticipantUnitSnapshot OwnUnit =
                FindOwnUnit(Snapshot, Attacker.RuntimeUnitID);

            if (OwnUnit == null || !OwnUnit.IsOnMap ||
                OwnUnit.LifeState != RuntimeUnitLifeState.Active)
                continue;

            ReadyAttackerCount++;

            for (int t = 0; t < Targets.Count; t++)
            {
                if (!_Access.TryGetBattleTarget(
                        Attacker.RuntimeUnitID,
                        Targets[t],
                        out GameBattle.TargetView Target,
                        out _))
                    continue;

                float Score =
                    Target.LegalWeaponCount * 5f +
                    Target.LegalAbilityCount * 3f +
                    (Target.IsAbsoluteVisible ? 2f : 0f) +
                    UnityEngine.Random.Range(0f, 2f);

                Choices.Add(new BattleChoice(
                    Attacker.RuntimeUnitID,
                    Target.Coordinate,
                    Score));
            }
        }

        if (Choices.Count == 0)
        {
            LogBattleEndReason(
                Snapshot,
                Targets.Count,
                ReadyAttackerCount);
            return GameParticipantAPI.ParticipantCommand.EndBattle(Snapshot);
        }

        BattleChoice Choice = HighestScoringBattle(Choices);
        return GameParticipantAPI.ParticipantCommand.BeginEngagement(
            Snapshot, Choice.RuntimeUnitID, Choice.Coordinate);
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
            $"Random Robo ended Battle on Turn {Snapshot.TurnNumber}: {Reason}.",
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
