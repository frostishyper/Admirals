# AI Integration

This is the technical entry point for integrating an AI controller with
Admirals. The complete phase-by-phase examples are in
`Docs/AIControllerIntegration.md`, but this file contains everything required
to bind a controller, read observations, produce commands, and submit them.

For an ordered co-developer implementation and testing checklist, use
`Docs/AICoDeveloperHandoff.md`.

For the implemented baseline opponent's exact scoring and phase policy, use
`Docs/RandomRoboAI.md`. For the prefab/profile setup required to add another
selectable AI, use `Docs/ImplementingNewAI.md`.

Current source of truth:

```text
Assets/Scripts/Game/GameMaster/GameParticipantAPI.cs
Assets/Scripts/Game/GameMaster/GameKnowledge.cs
Assets/Scripts/Game/GameMaster/GameMaster.cs
```

## 1. Integration flow

```text
GameMaster finishes initialization
        |
        v
TryGetAIPlayerAccess -> one participant-bound ParticipantAccess
        |
        v
TryGetSnapshot -> filtered ParticipantGameSnapshot
        |
        v
AI maps observation to one ParticipantCommand (or waits)
        |
        v
ParticipantAccess.SubmitCommand
        |
        v
existing GameMaster validation and resolution
        |
        v
ParticipantCommandResult + SnapshotChanged
```

The AI never supplies participant identity in a command. The retained
`ParticipantAccess` supplies the acting participant, so ordinary AI commands
cannot be redirected to the opponent.

All calls currently execute synchronously on Unity's main thread.

## 2. Add the controller

Create the co-developer's controller under:

```text
Assets/Scripts/Game/AI/AdmiralsAIController.cs
```

Derive it from `ParticipantAIController`, place it on a controller prefab, and
assign that prefab to the AI participant's `ParticipantInfo.AIControllerPrefab`
field. `GameMaster` instantiates the selected participant's prefab and binds it
to that exact participant's `ParticipantAccess`. Do not place an AI controller
in the Game scene and do not make the controller locate `GameMaster` itself.

Minimal working adapter:

```csharp
using UnityEngine;

public sealed class AdmiralsAIController : ParticipantAIController
{
    private GameParticipantAPI.ParticipantAccess _Access;
    private bool _DecisionRequested;

    protected override bool OnInitialize(
        GameParticipantAPI.ParticipantAccess access,
        out string validationMessage)
    {
        _Access = access;
        _Access.SnapshotChanged += RequestDecision;
        _Access.MovementPresented += OnMovementPresented;
        _Access.SearchPresented += OnSearchPresented;
        RequestDecision();
        validationMessage = string.Empty;
        return true;
    }

    private void OnDestroy()
    {
        if (_Access == null)
            return;

        _Access.SnapshotChanged -= RequestDecision;
        _Access.MovementPresented -= OnMovementPresented;
        _Access.SearchPresented -= OnSearchPresented;
    }

    private void RequestDecision() => _DecisionRequested = true;

    private void Update()
    {
        if (!_DecisionRequested || _Access == null)
            return;

        _DecisionRequested = false;

        if (!_Access.TryGetSnapshot(
                out ParticipantGameSnapshot snapshot,
                out string message))
        {
            Debug.LogWarning(message, this);
            return;
        }

        GameParticipantAPI.ParticipantCommand command = Decide(snapshot);
        if (command == null)
            return;

        GameParticipantAPI.ParticipantCommandResult result =
            _Access.SubmitCommand(command);

        if (!result.Accepted)
        {
            Debug.LogWarning(result.ValidationMessage, this);
            RequestDecision();
        }
    }

    private GameParticipantAPI.ParticipantCommand Decide(
        ParticipantGameSnapshot snapshot)
    {
        switch (snapshot.CurrentPhase)
        {
            case GameplayPhase.Deployment:
                return DecideDeployment(snapshot);

            case GameplayPhase.Movement:
                return snapshot.Movement != null && snapshot.Movement.CanAct
                    ? DecideMovement(snapshot)
                    : null;

            case GameplayPhase.PreBattle:
                return snapshot.PreBattle != null && snapshot.PreBattle.CanAct
                    ? DecidePreBattle(snapshot)
                    : null;

            case GameplayPhase.Search:
                return snapshot.Search != null && snapshot.Search.CanAct
                    ? DecideSearch(snapshot)
                    : null;

            case GameplayPhase.Battle:
                return DecideBattle(snapshot);

            default:
                return null;
        }
    }

    private GameParticipantAPI.ParticipantCommand DecideDeployment(
        ParticipantGameSnapshot snapshot) => null;

    private GameParticipantAPI.ParticipantCommand DecideMovement(
        ParticipantGameSnapshot snapshot) => null;

    private GameParticipantAPI.ParticipantCommand DecidePreBattle(
        ParticipantGameSnapshot snapshot) => null;

    private GameParticipantAPI.ParticipantCommand DecideSearch(
        ParticipantGameSnapshot snapshot) => null;

    private GameParticipantAPI.ParticipantCommand DecideBattle(
        ParticipantGameSnapshot snapshot) => null;

    private void OnMovementPresented(
        GameParticipantAPI.MovementPresentation presentation)
    {
        // Optional playback/logging output. Do not use as board truth.
    }

    private void OnSearchPresented(GameSearch.CommitResult result)
    {
        // Optional playback/logging output. SnapshotChanged provides new truth.
    }
}
```

Do not call `SubmitCommand` directly inside `SnapshotChanged`. Successful
commands raise that event synchronously. Mark work pending and decide on a
later `Update`, coroutine, or completed model callback as shown above.

`TryGetAIPlayerAccess` remains available for diagnostics and legacy adapters,
but production controllers do not use it. The GameMaster host binds each
authored AI controller to an exact participant.

## 3. Observation API

Get current permitted state with:

```csharp
bool read = _Access.TryGetSnapshot(
    out ParticipantGameSnapshot snapshot,
    out string validationMessage);
```

Main snapshot shape:

```text
ParticipantGameSnapshot
|- CurrentPhase, TurnNumber, MatchStarted
|- HasFirstPlayerPriority, DeploymentLocked
|- Match
|  |- MatchComplete, Outcome
|  |- OwnScore, OpponentScore
|  `- CompletedTurnNumber, ResolutionReason
|- Movement, PreBattle, Search, Battle, End phase views
|- Tiles[]
|  `- Coordinate, Visibility, TerrainKnown, Terrain
|- OwnUnits[]
|  |- runtime/fleet/definition IDs and authored Definition
|  |- life state, health, domain, facing and current board footprint
|  |- Weapons[] / Systems[] / Abilities[]
|  |- Statuses[] / Modifiers[] / Tags[]
|  `- carried Unit state and current charges
|- VisibleEnemies[]
|  `- only currently absolute-visible enemy presentation/board facts
|- Mines[]
`- Contacts[]
   `- coordinate and domain; no hidden RuntimeUnit reference
```

Authored definition references are deliberately available for permitted Units,
weapons, systems, abilities, statuses, modifiers, and tags. Treat every
definition and returned array as read-only.

Hidden enemies do not appear in `VisibleEnemies`. Contacts expose only learned
coordinate/domain information. Fogged tiles do not expose unknown terrain or
occupancy.

## 4. Command API

The AI returns one `GameParticipantAPI.ParticipantCommand` produced from the
snapshot it evaluated. Available factories are:

| Phase | Factory | Arguments after snapshot |
|---|---|---|
| Deployment | `PlaceDeployment` | fleetEntryID, pivot, facing |
| Deployment | `ResetDeployment` | none |
| Deployment | `ConfirmDeployment` | none |
| Pre-Battle/Search/End | `UseAbility` | runtimeUnitID, abilitySlotIndex |
| Movement | `CommitMovement` | runtimeUnitID, destinationPivot, facing |
| Movement | `EndMovement` | none |
| Pre-Battle | `DeployMine` | runtimeUnitID, systemSlot, coordinate |
| Pre-Battle | `DeployCarriedUnit` | carrierID, systemSlot, carriedIndex, pivot, facing |
| Pre-Battle | `EndPreBattle` | none |
| Search | `CommitSearch` | runtimeUnitID, sensorSlot, searchFacing |
| Search | `EndSearch` | none |
| Battle | `BeginEngagement` | attackerID, targetCoordinate |
| Battle | `CancelEngagement` | none |
| Engagement | `LockBattleAttack` | actionKind, slotIndex |
| Engagement | `LockBattleDefense` | defense kind/slot plus optional support Unit/kind/slot |
| Engagement | `ContinueEngagement` | none |
| Battle | `EndBattle` | none |
| End | `FinishEndPhase` | none |

Example movement command:

```csharp
GameParticipantAPI.ParticipantCommand command =
    GameParticipantAPI.ParticipantCommand.CommitMovement(
        snapshot,
        unit.RuntimeUnitID,
        new Vector2Int(18, 9),
        UnitFacing.East);

GameParticipantAPI.ParticipantCommandResult result =
    _Access.SubmitCommand(command);
```

The factory copies `ExpectedPhase` and `ExpectedTurnNumber` from `snapshot`.
`SubmitCommand` rejects it with `RejectedAsStale == true` if the phase or turn
changed while the AI was deciding. Normal authoritative validation still runs
when the phase/turn matches.

Command result shape:

```text
ParticipantCommandResult
|- Accepted
|- RejectedAsStale
|- ValidationMessage
|- CurrentPhase / CurrentTurnNumber
|- Movement          GameMovement.CommitResult or null
|- Search            GameSearch.CommitResult or null
`- BattleResolution  GameBattle.ResolutionView or null
```

`ValidationMessage` is diagnostic text, not a stable machine-readable error
code. On rejection, fetch a fresh snapshot and replan instead of branching on
exact message strings.

## 5. Queries and validation before commanding

Queries do not mutate state. They allow a controller to evaluate candidates
without copying Admirals legality rules into the AI:

```text
Deployment
  TryGetDeploymentView
  TryGetInitialDeploymentFacing
  TryGetDeploymentZoneCoordinates
  TryGetDeploymentPlacementTiles
  ValidateDeploymentPlacement

Movement
  TryGetMovementView
  TryGetMovementArea
  ValidateMovement -> GameMovement.MovementPlan

Pre-Battle
  TryGetPreBattleView
  TryGetMineDeploymentArea
  ValidateMineDeployment
  TryGetUnitDeploymentArea
  ValidateUnitDeployment

Search
  TryGetSearchView
  TryGetSearchPattern -> GameSearch.SearchTile[]

Battle
  TryGetBattleView
  TryGetFireControlArea
  TryGetBattleTarget -> GameBattle.TargetView
```

Example Movement proposal generation:

```csharp
if (!_Access.TryGetMovementArea(
        unit.RuntimeUnitID, out Vector2Int[] area, out _))
    return null;

foreach (Vector2Int destination in area)
{
    if (!_Access.ValidateMovement(
            unit.RuntimeUnitID,
            destination,
            unit.Facing,
            out GameMovement.MovementPlan plan,
            out _))
        continue;

    float score = ScoreMovement(plan, snapshot);
    // Retain the highest-scoring destination/facing, then return one command.
}
```

Validation is advisory. Submission revalidates because state may change while
the AI is thinking. Movement validation uses participant-known information;
authoritative commit may return `Interrupted` without revealing a concealed
blocker.

## 6. External model/JSON adapter

An external model should emit an untrusted proposal DTO, for example:

```json
{
  "kind": "CommitSearch",
  "runtimeUnitID": "unit-7f1a",
  "systemSlotIndex": 0,
  "searchFacing": "North"
}
```

The Unity adapter validates/parses that DTO and maps it to a command factory:

```csharp
return GameParticipantAPI.ParticipantCommand.CommitSearch(
    snapshot,
    proposal.runtimeUnitID,
    proposal.systemSlotIndex,
    proposal.searchFacing);
```

The external model must not provide participant identity, live gameplay
objects, its own claimed phase/turn token, or UI/GameObject references. The
adapter supplies the current snapshot to the factory and submits the result
through its retained `_Access`.

## 7. Battle engagement commands

During an engagement, use `snapshot.Battle.Engagement`.

Attacker:

```csharp
GameBattle.EngagementView engagement = snapshot.Battle.Engagement;
GameBattle.ActionOption attack = ChooseAttack(engagement.AttackerOptions);

return GameParticipantAPI.ParticipantCommand.LockBattleAttack(
    snapshot, attack.Kind, attack.SlotIndex);
```

Defender with optional ally support:

```csharp
GameBattle.ActionOption defense =
    ChooseDefenseOrNull(engagement.DefenderOptions);
GameBattle.AllySupportGroup ally =
    ChooseAllyOrNull(engagement.AllySupport);
GameBattle.ActionOption support =
    ally == null ? null : ChooseSupportOrNull(ally.Options);

return GameParticipantAPI.ParticipantCommand.LockBattleDefense(
    snapshot,
    defense?.Kind ?? GameBattle.CombatActionKind.None,
    defense?.SlotIndex ?? -1,
    ally?.RuntimeUnitID ?? string.Empty,
    support?.Kind ?? GameBattle.CombatActionKind.None,
    support?.SlotIndex ?? -1);
```

Only the attacker receives attacker options before lock-in. Only the defender
receives defender/support options. Locked enemy decisions remain hidden until
resolution.

## 8. Events

```text
SnapshotChanged
  Authoritative participant-readable state changed. Fetch a fresh snapshot.

MovementPresented
  Filtered playback route. Opponents receive only visible route segments.

SearchPresented
  Search footprint and contacts acquired by this participant.
```

Presentation events are not board truth and are not replayed for late
subscribers. Always fetch an initial snapshot explicitly.

## 9. End phase and match loop

`GameMaster` interprets current-state GameMode condition groups as relevant
state changes commit, and turn/checkpoint groups after both participants finish
End. It publishes either result through `snapshot.Match`:

```csharp
if (snapshot.Match.MatchComplete)
{
    ParticipantMatchOutcome outcome = snapshot.Match.Outcome;
    string reason = snapshot.Match.ResolutionReason;
}
```

`Outcome` is `Ongoing`, `Victory`, `Defeat`, or `Draw` from the bound
participant's perspective. Scores are exposed as `OwnScore` and
`OpponentScore`, so no controller-side participant remapping is needed.

An immediate result may therefore appear before End. Stop producing new actions
as soon as `MatchComplete` becomes true. When the last Battle resolution caused
the result, either engagement participant may still submit
`ContinueEngagement` after the reading gate; that closes the presentation and
moves the game into End.

During End, read `snapshot.End`. When `CanAct` is true the controller may submit
an End-authored `UseAbility`, or finish its action window with:

```csharp
return GameParticipantAPI.ParticipantCommand.FinishEndPhase(snapshot);
```

After both End action windows finish, GameMaster evaluates completed-turn
conditions. If no terminal condition is satisfied, it automatically alternates
first-player priority, increments the turn, clears turn-only Search/contact
knowledge, and publishes a fresh Movement snapshot. The AI must wait for that
snapshot and command against its new phase/turn; it must not advance
`GameMaster` directly.

## 10. Integration boundaries

The AI may:

- read its `ParticipantGameSnapshot` and definitions exposed through it;
- use participant queries and validators;
- produce and submit `ParticipantCommand` data;
- consume structured command results and filtered events.

The AI must not:

- click or scrape UI;
- inspect sprites, animations, HUD hierarchy, or map GameObjects;
- read `GameMap`, `GameUnits`, `GameKnowledge`, or the opponent roster;
- directly mutate runtime or authored objects;
- create `AIPlaceUnit`, `AIMoveUnit`, `AIAttack`, or parallel rules;
- infer concealed facts from rejected commands.

Human UI and AI both terminate at the same typed GameMaster input methods.
`ParticipantCommand` is only a convenient non-UI input envelope; it owns no
gameplay rules.

## 11. Handoff checklist

- Match contains exactly one `ParticipantType.AI` for the current bootstrap.
- Controller waits for `GameMaster.InitializationComplete`.
- Controller acquires and retains one `ParticipantAccess`.
- Controller subscribes and explicitly requests its first snapshot.
- Strategy maps one permitted snapshot to one command or `null`.
- Commands are submitted only through the retained access.
- Failed/stale commands cause a fresh observation and replan.
- Definition references are treated as read-only.
- AI explicitly ends phases when that is its chosen/required action.
- AI reads `snapshot.Match` during End and stops acting if the match is complete.
- AI uses End-authored abilities or `FinishEndPhase` while `snapshot.End.CanAct`.
- AI observes the fresh Movement snapshot after a completed non-terminal End.
- AI never advances the turn or alternates first-player priority itself.

For complete examples for Deployment, Movement, Pre-Battle, Search, Battle
overworld selection, attacker lock-in, defender support, and resolution, see
`Docs/AIControllerIntegration.md`.
