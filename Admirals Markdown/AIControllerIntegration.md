# AI Controller Integration Guide

> Practical integration contract for an Admirals AI controller. Architectural
> policy remains in `AIIntegration.md`; current C# source remains authoritative
> for exact signatures.
>
> For the recommended implementation order and phase validation checklist, see
> `Docs/AICoDeveloperHandoff.md`.

## 1. Integration boundary

An AI controller receives exactly one
`GameParticipantAPI.ParticipantAccess` and keeps it for the match. The access
object is already bound to one `RuntimeParticipant`; gameplay calls therefore
do not accept a caller-selected participant.

The AI may:

- read participant-filtered snapshots and phase views;
- read authored definition objects referenced by its permitted snapshots;
- ask the API for legal areas, previews, and validation;
- submit the same plain gameplay inputs used by the human controller.

The AI must not:

- read `GameMap`, `GameUnits`, `GameKnowledge`, or the opponent roster;
- inspect HUD, scene objects, sprites, or animation state;
- mutate runtime or authored objects returned by the game;
- infer a failed action's concealed cause beyond its validation message;
- create a second AI-specific rules path.

All API calls currently run synchronously on Unity's main thread.

## 2. Receiving the bound AI side

Create a prefab component derived from `ParticipantAIController`. Assign that
prefab to `ParticipantInfo.AIControllerPrefab`; `GameMaster` instantiates it
and supplies access already bound to the exact AI participant:

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
        if (_Access.TryGetSnapshot(out ParticipantGameSnapshot snapshot,
                out string message))
        {
            GameParticipantAPI.ParticipantCommand command = Decide(snapshot);
            if (command != null)
            {
                GameParticipantAPI.ParticipantCommandResult result =
                    _Access.SubmitCommand(command);
                if (!result.Accepted)
                    RequestDecision();
            }
        }
        else
            Debug.LogWarning(message, this);
    }

    private GameParticipantAPI.ParticipantCommand Decide(
        ParticipantGameSnapshot snapshot)
    {
        // Map permitted observation -> one proposed command, or null to wait.
        return null;
    }
    private void OnMovementPresented(
        GameParticipantAPI.MovementPresentation presentation) { }
    private void OnSearchPresented(GameSearch.CommitResult result) { }
}
```

Do not submit another action directly from `SnapshotChanged`. Mutating calls
raise notifications synchronously. Mark the controller dirty and decide from a
later `Update`, coroutine, or job-completion callback as above.

`TryGetAIPlayerAccess` remains available for diagnostics and legacy adapters.
Production controllers use the exact access supplied by the GameMaster host
and never select their own participant.

Useful access identity/static properties are:

```text
ParticipantAccess.DisplayName
ParticipantAccess.ParticipantType
ParticipantAccess.Fleet
```

## 3. Main observation snapshot

Call:

```csharp
bool ok = _Access.TryGetSnapshot(out ParticipantGameSnapshot snapshot,
    out string validationMessage);
```

Conceptual shape (illustrative values, not serialized JSON):

```text
ParticipantGameSnapshot
├─ CurrentPhase: Search
├─ TurnNumber: 2
├─ MatchStarted: true
├─ HasFirstPlayerPriority: false
├─ DeploymentLocked: true
├─ Movement / PreBattle / Search / Battle: participant phase views
├─ Tiles[]
│  └─ Coordinate, Visibility, TerrainKnown, Terrain
├─ OwnUnits[]
│  ├─ RuntimeUnitID, FleetEntryID, InstanceName
│  ├─ Definition + DefinitionID + DefinitionName
│  ├─ LifeState, CurrentHealth, MaxHealth, CurrentDomain, Facing
│  ├─ IsOnMap, MovementPivotCoordinate, OccupiedCoordinates[]
│  ├─ AbsoluteVision, EmbarkedPresentationLevel
│  ├─ Weapons[]: slot, Definition, charges, replenishment progress
│  ├─ Systems[]: slot/type, Definition, charges, carried Units
│  ├─ Abilities[]: slot and Definition
│  ├─ Statuses[] and Modifiers[]
│  └─ Tags[]
├─ VisibleEnemies[]
│  └─ revealed identity/class, Definition, domain, facing, footprint, tags
├─ Mines[]
│  └─ known mine ID, coordinate, whether owned
└─ Contacts[]
   └─ coordinate and domain only
```

### Information rules

- `OwnUnits` contains complete copied runtime state for the AI's Units.
- `VisibleEnemies` contains only currently absolute-visible enemies and omits
  enemy runtime IDs, health, weapons, systems, statuses, and other hidden state.
- `Contacts` are participant-specific Search knowledge. A contact is not an
  authoritative enemy Unit reference.
- Fogged tiles do not expose unknown terrain or occupancy.
- Definition references are static authored data and are intentionally readable
  by HUD and AI. Treat every returned definition and its arrays as read-only.
- Snapshot arrays/runtime values are projections. Fetch a new snapshot after a
  notification or successful action; do not treat an old snapshot as live.

Use slot indices from a snapshot or phase option when submitting an action.
Names are display text; IDs are semantic identity; array positions should not
be guessed when a `SlotIndex` is provided.

## 4. Participant command contract

The AI's decision output is a plain
`GameParticipantAPI.ParticipantCommand`. Submit it through the same bound
access object that supplied the observation:

```csharp
GameParticipantAPI.ParticipantCommand command =
    GameParticipantAPI.ParticipantCommand.CommitMovement(
        snapshot, unitID, destinationPivot, destinationFacing);

GameParticipantAPI.ParticipantCommandResult result =
    _Access.SubmitCommand(command);
```

The command contains `ExpectedPhase` and `ExpectedTurnNumber`, copied from the
snapshot used to make the decision. It deliberately contains no participant
or side field. `_Access` supplies the actor, preventing an AI from commanding
the opponent through ordinary input.

### Commands exposed

| Command factory | Decision data |
|---|---|
| `PlaceDeployment` | Fleet Entry ID, pivot, facing |
| `ResetDeployment` | none |
| `ConfirmDeployment` | none |
| `UseAbility` | runtime Unit ID, ability slot |
| `CommitMovement` | runtime Unit ID, destination pivot/facing |
| `EndMovement` | none |
| `DeployMine` | runtime Unit ID, system slot, coordinate |
| `DeployCarriedUnit` | carrier ID, system slot, carried index, pivot/facing |
| `EndPreBattle` | none |
| `CommitSearch` | runtime Unit ID, sensor slot, search facing |
| `EndSearch` | none |
| `BeginEngagement` | attacker ID, target coordinate |
| `CancelEngagement` | none |
| `LockBattleAttack` | action kind and slot |
| `LockBattleDefense` | own defense plus optional ally/support action |
| `ContinueEngagement` | none |
| `EndBattle` | none |
| `FinishEndPhase` | none |

Queries and validators remain methods on `ParticipantAccess`; they help the AI
construct a good command but do not mutate state. `SubmitCommand` dispatches to
the existing typed participant input methods, so it does not duplicate any
legality or resolution rules.

### Command result

```text
ParticipantCommandResult
├─ Command                 original submitted data
├─ Accepted                authoritative mutation succeeded
├─ RejectedAsStale         observed phase/turn no longer current
├─ ValidationMessage       diagnostic, not a stable machine code
├─ CurrentPhase
├─ CurrentTurnNumber
├─ Movement                CommitResult for CommitMovement, otherwise null
├─ Search                  CommitResult for CommitSearch, otherwise null
└─ BattleResolution        produced when defense lock resolves combat
```

Typical handling:

```csharp
GameParticipantAPI.ParticipantCommandResult result =
    _Access.SubmitCommand(command);

if (!result.Accepted)
{
    // The board may have changed while the model was deciding. Do not parse
    // ValidationMessage for strategy; request a fresh observation and replan.
    RequestDecision();
    return;
}

// SnapshotChanged will also request the next permitted decision.
```

If an external model emits JSON, treat it as an untrusted proposal DTO and map
it to one of the factories above. For example, model output may look like:

```json
{
  "kind": "CommitMovement",
  "runtimeUnitID": "unit-7f1a",
  "destinationMovementPivot": { "x": 21, "y": 14 },
  "destinationFacing": "East"
}
```

The adapter supplies the current snapshot to `ParticipantCommand.CommitMovement`.
The model must not provide participant identity, authoritative objects, or a
claimed phase/turn token.

## 5. Common decision loop

```csharp
private GameParticipantAPI.ParticipantCommand Decide(
    ParticipantGameSnapshot s)
{
    switch (s.CurrentPhase)
    {
        case GameplayPhase.Deployment:
            return DecideDeployment(s);
        case GameplayPhase.Movement:
            if (s.Movement != null && s.Movement.CanAct)
                return DecideMovement(s);
            return null;
        case GameplayPhase.PreBattle:
            if (s.PreBattle != null && s.PreBattle.CanAct)
                return DecidePreBattle(s);
            return null;
        case GameplayPhase.Search:
            if (s.Search != null && s.Search.CanAct)
                return DecideSearch(s);
            return null;
        case GameplayPhase.Battle:
            return DecideBattle(s);
        case GameplayPhase.End:
            if (s.Match.MatchComplete)
                return null;
            return s.End != null && s.End.CanAct
                ? GameParticipantAPI.ParticipantCommand.FinishEndPhase(s)
                : null;
        default:
            return null;
    }
}
```

`CanAct` is the priority/turn gate for sequential phases. A snapshot can change
while the AI is thinking, so every commit is authoritatively revalidated. A
`false` return is normal stale-input handling: discard the proposal, fetch a
fresh snapshot, and decide again.

## 6. Deployment

Readable operations:

```text
TryGetDeploymentView
TryGetInitialDeploymentFacing
TryGetDeploymentZoneCoordinates
TryGetDeploymentPlacementTiles
ValidateDeploymentPlacement
```

Mutating operations:

```text
TrySubmitDeploymentPlacement
TrySelectDeploymentPlacement / TryClearDeploymentSelection
TryRotateDeploymentPlacementClockwise / CounterClockwise
TryResetDeployment
TryConfirmDeployment
```

The strategic AI normally needs only submit/reset/confirm; selection and rotate
helpers exist primarily for interactive controllers.

Example:

```csharp
if (!_Access.TryGetDeploymentView(out GameDeployment.ParticipantView view) ||
    view.IsLocked)
    return;

_Access.TryGetDeploymentZoneCoordinates(out Vector2Int[] zone);
_Access.TryGetInitialDeploymentFacing(out UnitFacing facing);

foreach (GameDeployment.PlacementView entry in view.Placements)
{
    if (entry.IsPlaced)
        continue;

    foreach (Vector2Int pivot in zone)
    {
        if (!_Access.ValidateDeploymentPlacement(entry.ID, pivot, facing,
                out _))
            continue;

        GameParticipantAPI.ParticipantCommandResult placed =
            _Access.SubmitCommand(
                GameParticipantAPI.ParticipantCommand.PlaceDeployment(
                    snapshot, entry.ID, pivot, facing));
        if (placed.Accepted)
            break;

        Debug.LogWarning(placed.ValidationMessage);
    }
}

// Confirm performs final whole-plan validation and is irreversible.
_Access.SubmitCommand(
    GameParticipantAPI.ParticipantCommand.ConfirmDeployment(snapshot));
```

Opponent tentative Deployment never appears in this access object's output.
Runtime Units do not exist until confirmation commits the plan.

## 7. Movement

Phase state:

```text
snapshot.Movement
├─ IsMovementPhaseActive
├─ MovementLimit / MovesCommitted
├─ MovementEnded / CanAct / SecondsRemaining
└─ Units[]: RuntimeUnitID + HasMoved
```

Queries and inputs:

```text
TryGetMovementArea(unitID)
ValidateMovement(unitID, destinationPivot, destinationFacing)
TryCommitMovement(unitID, destinationPivot, destinationFacing)
TryEndMovement()
```

Example candidate evaluation:

```csharp
GameMovement.UnitView ready = snapshot.Movement.Units
    .FirstOrDefault(x => !x.HasMoved);
if (ready == null)
{
    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.EndMovement(snapshot));
    return;
}

if (_Access.TryGetMovementArea(ready.RuntimeUnitID,
        out Vector2Int[] area, out _))
{
    foreach (Vector2Int destination in area)
    foreach (UnitFacing facing in Enum.GetValues(typeof(UnitFacing)))
    {
        if (!_Access.ValidateMovement(ready.RuntimeUnitID, destination, facing,
                out GameMovement.MovementPlan plan, out _))
            continue;

        // Score plan.Route / destination here. For the chosen proposal:
        GameParticipantAPI.ParticipantCommand command =
            GameParticipantAPI.ParticipantCommand.CommitMovement(
                snapshot, ready.RuntimeUnitID, destination, facing);
        GameParticipantAPI.ParticipantCommandResult result =
            _Access.SubmitCommand(command);
        return; // This example chooses the first valid candidate.
    }
}
```

Validation plans from participant-known information. Commit checks concealed
board truth and may return `CommitOutcome.Interrupted` at the last legal step
without revealing the concealed blocker.

`MovementPresented` is playback output, not gameplay truth. The owner receives
the full route. An opponent receives only visible contiguous route segments;
an unseen enemy has an empty runtime ID.

## 8. Pre-Battle

Phase state:

```text
snapshot.PreBattle
├─ IsPreBattlePhaseActive
├─ PreBattleEnded / CanAct
├─ ActionsCommitted
└─ SecondsRemaining
```

Available inputs:

```text
TryUseAbility(unitID, abilitySlotIndex)
TryGetMineDeploymentArea / ValidateMineDeployment / TryDeployMine
TryGetUnitDeploymentArea / ValidateUnitDeployment / TryDeployCarriedUnit
TryEndPreBattle
```

Find candidates through `OwnUnits[].Abilities`, `OwnUnits[].Systems`, system
definitions, their current charges, and carried Unit state. Definition phase,
domain, pattern, outcome, and target data are readable from each snapshot's
`Definition` property; final legality still belongs to the API.

Example mine deployment:

```csharp
ParticipantUnitSnapshot unit = snapshot.OwnUnits.First(x => x.IsOnMap);
ParticipantSystemSnapshot mineLayer = unit.Systems.FirstOrDefault(x =>
    x.CurrentCharges > 0 && x.Definition != null &&
    x.SlotType == SystemSlotType.Deployment);

if (mineLayer != null && _Access.TryGetMineDeploymentArea(
        unit.RuntimeUnitID, mineLayer.SlotIndex, out Vector2Int[] area, out _))
{
    Vector2Int chosen = ChooseMineCoordinate(area, snapshot);
    _Access.SubmitCommand(GameParticipantAPI.ParticipantCommand.DeployMine(
        snapshot, unit.RuntimeUnitID, mineLayer.SlotIndex, chosen));
}
else
{
    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.EndPreBattle(snapshot));
}
```

Use the specific validators before committing expensive actions. Ending the
phase is always an explicit strategic option while `CanAct` is true.

## 9. Search

Phase state:

```text
snapshot.Search
├─ IsSearchPhaseActive
├─ SearchEnded / CanAct / CanEndSearch
├─ SearchLimit / SearchesCommitted / SecondsRemaining
└─ Units[]: RuntimeUnitID + HasSearched
```

Operations:

```text
TryGetSearchPattern(unitID, systemSlot, facing)
TryCommitSearch(unitID, systemSlot, facing)
TryUseAbility(unitID, abilitySlot)
TryEndSearch
```

Example:

```csharp
GameSearch.UnitView searcher = snapshot.Search.Units
    .FirstOrDefault(x => !x.HasSearched);
ParticipantUnitSnapshot unit = snapshot.OwnUnits
    .FirstOrDefault(x => x.RuntimeUnitID == searcher?.RuntimeUnitID);

ParticipantSystemSnapshot sensor = unit?.Systems.FirstOrDefault(x =>
    x.CurrentCharges > 0 && x.Definition?.Search != null &&
    x.Definition.Search.Enabled);

if (unit != null && sensor != null &&
    _Access.TryGetSearchPattern(unit.RuntimeUnitID, sensor.SlotIndex,
        unit.Facing, out GameSearch.SearchTile[] tiles, out _))
{
    // Each tile contains Coordinate + Strength. Score it, rotate if useful,
    // then submit exactly one chosen facing.
    GameParticipantAPI.ParticipantCommandResult result =
        _Access.SubmitCommand(
            GameParticipantAPI.ParticipantCommand.CommitSearch(snapshot,
                unit.RuntimeUnitID, sensor.SlotIndex, unit.Facing));
}
else if (snapshot.Search.CanEndSearch)
{
    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.EndSearch(snapshot));
}
```

`CommitResult.Tiles` contains the played search footprint and strength.
`CommitResult.Contacts` contains only contacts successfully acquired by this
participant. `SearchPresented` carries the same committed presentation result.

## 10. Battle overworld

Phase state:

```text
snapshot.Battle
├─ IsBattlePhaseActive / BattleEnded / CanAct
├─ EngagementActive / SecondsRemaining / AttacksCommitted
├─ Units[]: RuntimeUnitID + HasAttacked
└─ Engagement (null while no engagement is active)
```

Overworld operations:

```text
TryGetFireControlArea(attackerID, domain)
TryGetBattleTarget(attackerID, targetCoordinate)
TryBeginEngagement(attackerID, targetCoordinate)
TryEndBattle
```

Example:

```csharp
GameBattle.UnitView attacker = snapshot.Battle.Units
    .FirstOrDefault(x => !x.HasAttacked);

foreach (ParticipantContactSnapshot contact in snapshot.Contacts)
{
    if (attacker == null)
        break;
    if (!_Access.TryGetBattleTarget(attacker.RuntimeUnitID,
            contact.Coordinate, out GameBattle.TargetView target, out _))
        continue;
    if (target.LegalWeaponCount + target.LegalAbilityCount == 0)
        continue;

    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.BeginEngagement(snapshot,
            attacker.RuntimeUnitID, target.Coordinate));
    return;
}

_Access.SubmitCommand(GameParticipantAPI.ParticipantCommand.EndBattle(
    snapshot)); // Legal even when choosing not to attack.
```

Querying a coordinate is not permission to target hidden authority. The target
query succeeds only for a legitimate contact or absolute-visible enemy and a
legal attacker/range/action combination.

## 11. Battle engagement

Once active, both sides read their own filtered
`snapshot.Battle.Engagement`:

```text
EngagementView
├─ IsAttacker, attacker/defender public engagement identity and health
├─ BeganFromContact
├─ AttackerLocked / DefenderLocked / Resolved
├─ SelectedAttackName
├─ AttackerOptions[]       (attacker only until reveal rules permit)
├─ DefenderOptions[]       (defender only)
├─ AllySupport[]           (defender only)
└─ Resolution              (only after resolution)
```

An `ActionOption` is already legality-filtered and supplies:

```text
Kind, SlotIndex, Name, UsesD20, CurrentCharges, MaxCharges
```

Attacker:

```csharp
GameBattle.EngagementView e = snapshot.Battle.Engagement;
if (e.IsAttacker && !e.AttackerLocked)
{
    GameBattle.ActionOption attack = ChooseAttack(e.AttackerOptions);
    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.LockBattleAttack(snapshot,
            attack.Kind, attack.SlotIndex));
}
```

Defender (one own defense plus optional one ally support):

```csharp
if (!e.IsAttacker && e.AttackerLocked && !e.DefenderLocked)
{
    GameBattle.ActionOption defense = ChooseDefenseOrNull(e.DefenderOptions);
    GameBattle.AllySupportGroup ally = ChooseAllyOrNull(e.AllySupport);
    GameBattle.ActionOption support =
        ally == null ? null : ChooseSupportOrNull(ally.Options);

    GameParticipantAPI.ParticipantCommandResult result =
        _Access.SubmitCommand(
            GameParticipantAPI.ParticipantCommand.LockBattleDefense(snapshot,
                defense?.Kind ?? GameBattle.CombatActionKind.None,
                defense?.SlotIndex ?? -1,
                ally?.RuntimeUnitID ?? string.Empty,
                support?.Kind ?? GameBattle.CombatActionKind.None,
                support?.SlotIndex ?? -1));

    GameBattle.ResolutionView resolution = result.BattleResolution;
}
```

After resolution, inspect `ResolutionView` (`AttackerRoll`, `AllyRoll`,
`DefenderRoll`, `Damage`, `DefenderDestroyed`, `ResultName`, and ordered
`Events`). Continue only when the authoritative six-second reading gate allows:

```csharp
if (e.Resolved)
    _Access.SubmitCommand(
        GameParticipantAPI.ParticipantCommand.ContinueEngagement(snapshot));
```

A temporary rejection during this gate is expected. Do not implement a second
local timer as authority; retry after a later snapshot/update.

## 12. End phase and match lifecycle

`GameMaster` evaluates current-state GameMode groups after relevant
authoritative state changes and turn/checkpoint groups after both participants
finish End. End exposes an action window for End-authored self abilities and
`FinishEndPhase`. Both timings expose the filtered result at
`snapshot.Match`:

```csharp
case GameplayPhase.End:
    if (snapshot.Match.MatchComplete)
    {
        HandleFinishedMatch(snapshot.Match.Outcome,
            snapshot.Match.OwnScore,
            snapshot.Match.OpponentScore,
            snapshot.Match.ResolutionReason);
        return null;
    }
    if (snapshot.End != null && snapshot.End.CanAct)
        return GameParticipantAPI.ParticipantCommand.FinishEndPhase(snapshot);
    return null;
```

`CompletedTurnNumber` identifies the latest fully completed turn. The outcome
is already relative to the bound participant and is one of `Ongoing`,
`Victory`, `Defeat`, or `Draw`.

Check `snapshot.Match.MatchComplete` before selecting any new action, not only
inside the End case. If a Battle resolution completed the match, allow the
resolved engagement's `ContinueEngagement` command after its reading gate so
the presentation can close; do not choose another combat action.

The current repository still does not advance a non-terminal End into the next
Movement turn. Treat such End snapshots as waiting; do not invent a transition
or inspect `GameMaster` internals.

## 13. Return and validation semantics

- Query/validate methods never mutate authoritative gameplay state.
- `Try...` mutation methods validate again and return `false` with a human
  readable message when stale, illegal, out of priority, out of charges, or in
  the wrong phase.
- Validation messages are diagnostics, not stable machine-readable error codes.
  Branch on snapshot/view state and booleans, not exact message text.
- Successful mutations notify both participant access objects so each consumer
  can fetch its newly filtered snapshot.
- Never assume a proposal remains legal between validation and commit.

## 14. Recommended AI adapter split

Keep the co-developer's code above the gameplay API:

```text
AdmiralsAIController (Unity lifecycle + bound ParticipantAccess)
├─ ObservationMapper (optional DTO/tensor conversion)
├─ Strategy / model
└─ ActionExecutor
   └─ calls ParticipantAccess validate/query/submit methods
```

The mapper may transform permitted snapshots for a model, but must not augment
them from authoritative managers or presentation. The executor should keep no
alternative board truth and should recover from rejected stale proposals by
requesting a fresh snapshot.

## 15. Integration checklist

- AI participant is authored as `ParticipantType.AI`.
- Controller waits for `GameMaster.InitializationComplete`.
- `TryGetAIPlayerAccess` succeeds exactly once.
- Initial snapshot is fetched explicitly after subscribing.
- Strategy maps one permitted snapshot to one `ParticipantCommand` or waits.
- Commands are submitted only through the same bound `ParticipantAccess`.
- Decisions are gated by phase and `CanAct`/engagement role.
- Definition references are read but never modified.
- Every commit handles `false` without crashing or leaking concealed causes.
- Controller can explicitly end Movement, Pre-Battle, Search (when allowed),
  Battle, and its End action window.
- No direct `GameMap`, `GameUnits`, `GameKnowledge`, HUD, or scene scraping.
- End reads the participant-relative result, uses End actions when desired,
  submits `FinishEndPhase`, then waits for turn progression.

