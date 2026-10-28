# AI Co-Developer Handoff

This document is the shortest practical route for connecting an AI controller
to Admirals. It assumes the gameplay phases already exist and the co-developer
owns AI strategy/model behavior, not Admirals gameplay rules.

Related references:

- `Docs/AIIntegration.md` — technical API entry point.
- `Docs/AIControllerIntegration.md` — detailed examples for every phase.
- `Assets/Scripts/Game/GameMaster/GameParticipantAPI.cs` — exact current API.

## 1. Choose the AI runtime

Two integration shapes are supported.

### Unity-local C# AI

The strategy runs inside Unity and directly receives
`ParticipantGameSnapshot`. It returns a `ParticipantCommand`.

Use this when the AI is heuristic, utility-based, behavior-tree based, or a
local C# model.

### External AI

Unity converts the permitted snapshot into a serializable observation DTO,
sends it through a transport, receives a proposal DTO, and maps that proposal
to a `ParticipantCommand`.

Possible transports include HTTP, WebSocket, named pipes, or a child process.
Admirals currently provides the gameplay observation/command boundary but does
not select or implement an external transport.

In either arrangement, Unity remains authoritative.

## 2. Create the Unity adapter

Create:

```text
Assets/Scripts/Game/AI/AdmiralsAIController.cs
```

The adapter owns Unity lifecycle and participant API communication. Strategy
or model code should remain separate. Derive from `ParticipantAIController`;
the selected `ParticipantInfo` controller prefab is instantiated and bound by
`GameMaster`.

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

        // Events are not replayed for late subscribers.
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

    private void RequestDecision()
    {
        _DecisionRequested = true;
    }

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

        GameParticipantAPI.ParticipantCommand command =
            Decide(snapshot);

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
        // Replace this with the strategy/model adapter.
        return null;
    }

    private void OnMovementPresented(
        GameParticipantAPI.MovementPresentation presentation)
    {
        // Optional playback/logging information, not board truth.
    }

    private void OnSearchPresented(GameSearch.CommitResult result)
    {
        // Optional playback/logging information, not board truth.
    }
}
```

Do not submit another command directly from `SnapshotChanged`. Successful
commands invoke the event synchronously. Queue the next decision for a later
`Update`, coroutine, or model-completion callback.

## 3. Unity setup

Each AI `ParticipantInfo` must reference a prefab containing exactly one
`ParticipantAIController` implementation. `GameMaster` creates and binds that
prefab automatically; do not add it to the Game scene.

```text
Game
├─ GameMaster
└─ AIController
   └─ AdmiralsAIController
      └─ Game Master → Game/GameMaster
```

The AI adapter requires no HUD, camera, tilemap, UnitView, animation, or
GameMaster reference.

## 4. Read the observation

The AI reads only:

```csharp
_Access.TryGetSnapshot(
    out ParticipantGameSnapshot snapshot,
    out string validationMessage);
```

Important top-level fields:

```text
CurrentPhase
TurnNumber
MatchStarted
HasFirstPlayerPriority
DeploymentLocked
Match
  MatchComplete / Outcome
  OwnScore / OpponentScore
  CompletedTurnNumber / ResolutionReason
Movement / PreBattle / Search / Battle
Tiles[]
OwnUnits[]
VisibleEnemies[]
Mines[]
Contacts[]
```

`OwnUnits` contains the AI's complete current runtime state. Authored
definitions are available through each permitted Unit, weapon, system,
ability, status, modifier, and tag snapshot.

`VisibleEnemies` contains only currently absolute-visible enemy information.
`Contacts` contain only coordinate and domain. Hidden enemies are not included.

Treat snapshots and authored definitions as read-only.

## 5. Produce one command

Strategy maps one snapshot to either one command or `null`.

```csharp
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

        case GameplayPhase.End:
            return snapshot.End != null && snapshot.End.CanAct &&
                !snapshot.Match.MatchComplete
                ? GameParticipantAPI.ParticipantCommand.FinishEndPhase(snapshot)
                : null;

        default:
            return null;
    }
}
```

Available command factories:

```text
Deployment
  ParticipantCommand.PlaceDeployment
  ParticipantCommand.ResetDeployment
  ParticipantCommand.ConfirmDeployment

Movement
  ParticipantCommand.CommitMovement
  ParticipantCommand.EndMovement

Pre-Battle
  ParticipantCommand.UseAbility
  ParticipantCommand.DeployMine
  ParticipantCommand.DeployCarriedUnit
  ParticipantCommand.EndPreBattle

Search
  ParticipantCommand.UseAbility
  ParticipantCommand.CommitSearch
  ParticipantCommand.EndSearch

Battle overworld
  ParticipantCommand.BeginEngagement
  ParticipantCommand.CancelEngagement
  ParticipantCommand.EndBattle

Battle engagement
  ParticipantCommand.LockBattleAttack
  ParticipantCommand.LockBattleDefense
  ParticipantCommand.ContinueEngagement

End
  ParticipantCommand.UseAbility
  ParticipantCommand.FinishEndPhase
```

Example:

```csharp
return GameParticipantAPI.ParticipantCommand.CommitMovement(
    snapshot,
    selectedUnit.RuntimeUnitID,
    selectedDestination,
    selectedFacing);
```

The command factory copies the expected phase and turn from the observation.
The command does not contain participant identity.

## 6. Query legality before choosing

Do not copy Admirals legality rules into the AI. Use participant queries to
generate and validate candidates, then let strategy score them.

```text
Deployment
  TryGetDeploymentZoneCoordinates
  ValidateDeploymentPlacement

Movement
  TryGetMovementArea
  ValidateMovement

Pre-Battle
  TryGetMineDeploymentArea
  ValidateMineDeployment
  TryGetUnitDeploymentArea
  ValidateUnitDeployment

Search
  TryGetSearchPattern

Battle
  TryGetFireControlArea
  TryGetBattleTarget
```

Example Movement selection:

```csharp
if (!_Access.TryGetMovementArea(
        unit.RuntimeUnitID,
        out Vector2Int[] area,
        out _))
{
    return GameParticipantAPI.ParticipantCommand.EndMovement(snapshot);
}

Vector2Int bestDestination = default;
UnitFacing bestFacing = unit.Facing;
float bestScore = float.NegativeInfinity;
bool found = false;

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
    if (score <= bestScore)
        continue;

    bestScore = score;
    bestDestination = destination;
    found = true;
}

return found
    ? GameParticipantAPI.ParticipantCommand.CommitMovement(
        snapshot, unit.RuntimeUnitID, bestDestination, bestFacing)
    : GameParticipantAPI.ParticipantCommand.EndMovement(snapshot);
```

Validation is advisory. Submission always validates again against current
authoritative state.

## 7. Submit and handle the result

```csharp
GameParticipantAPI.ParticipantCommandResult result =
    _Access.SubmitCommand(command);
```

Result fields:

```text
Accepted
RejectedAsStale
ValidationMessage
CurrentPhase
CurrentTurnNumber
Movement
Search
BattleResolution
```

On rejection:

```csharp
if (!result.Accepted)
{
    RequestDecision();
    return;
}
```

Do not branch strategy on exact `ValidationMessage` text. It is diagnostic, not
a stable error code. Fetch a new snapshot and replan.

`RejectedAsStale` means the command's observation phase/turn no longer matches
the game.

## 8. External model DTO boundary

Do not send Unity runtime objects to an external model. Define serializable
observation and proposal DTOs.

Example proposal:

```json
{
  "kind": "CommitSearch",
  "runtimeUnitID": "unit-7f1a",
  "systemSlotIndex": 0,
  "searchFacing": "North"
}
```

Map it inside Unity:

```csharp
return GameParticipantAPI.ParticipantCommand.CommitSearch(
    snapshot,
    proposal.runtimeUnitID,
    proposal.systemSlotIndex,
    proposal.searchFacing);
```

The external proposal must not specify:

- participant identity;
- its own phase/turn token;
- `RuntimeUnit` or other live gameplay objects;
- GameObjects, HUD controls, sprites, or prefab references.

The Unity adapter supplies the current snapshot to the factory.

## 9. Recommended implementation order

Implement and verify one phase at a time:

1. Deployment: place every Fleet Entry and confirm.
2. Movement: choose an unmoved Unit, evaluate routes, commit or end.
3. Pre-Battle: use ability/deploy mine/deploy carried Unit or end.
4. Search: choose Unit, sensor, facing, then search or end when allowed.
5. Battle overworld: choose attacker and valid target or end.
6. Engagement attacker: choose one offered attack option.
7. Engagement defender: choose defense and optional allied support.
8. Resolution: continue when the authoritative delay permits.

Start with deterministic/simple decisions. Replace only the scoring/selection
layer with the real AI once the entire observation-command loop works.

## 10. Phase validation checklist

### Deployment

- AI sees only its own tentative placements.
- Every placement passes `ValidateDeploymentPlacement`.
- `ConfirmDeployment` succeeds only after a complete valid plan.

### Movement

- AI acts only when `snapshot.Movement.CanAct`.
- A Unit never moves twice in one Movement phase.
- Interrupted movement returns a structured result without revealing a hidden
  blocker.

### Pre-Battle

- Mine and carried-Unit candidates come from API areas/validators.
- Charges and carried state update in the next snapshot.
- AI can intentionally end without taking an action.

### Search

- AI acts only when `snapshot.Search.CanAct`.
- One Unit searches at most once.
- Contacts returned belong only to the AI participant.

### Battle

- Only contacts or absolute-visible enemies can be targeted.
- The attacker receives only legal attack options.
- The defender receives its defense and optional ally-support options.
- Locked opponent choices remain hidden until resolution.

## 11. Match loop and completion

Current-state GameMode groups are evaluated after relevant authoritative state
changes; turn/checkpoint groups are evaluated after both participants finish
End. Read
`snapshot.Match` after every update for the participant-relative `Victory`,
`Defeat`, `Draw`, or `Ongoing` result and the current scores. A final resolved
Battle engagement may still be continued after its reading gate to close the
presentation, but no new gameplay choice is legal after `MatchComplete`.

During an active End window, use an End-authored ability or submit
`ParticipantCommand.FinishEndPhase(snapshot)`. When both participants finish a
non-terminal End, GameMaster automatically enters the next turn's Movement
phase and alternates first-player priority. Wait for the updated snapshot and
command against its new phase/turn; do not advance `GameMaster` directly.

## 12. Definition of done

The integration is working when:

- the controller binds exactly one AI participant;
- it receives only participant-permitted information;
- it completes every implemented phase without clicking UI;
- every decision becomes one `ParticipantCommand`;
- every command goes through the retained `ParticipantAccess`;
- rejected commands cause a fresh observation and replan;
- no strategy code reads authoritative managers or presentation objects;
- the same GameMaster validators resolve human and AI inputs.
