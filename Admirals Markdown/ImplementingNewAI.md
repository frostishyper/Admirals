# Implementing and Selecting a New AI

This is the practical recipe for adding another Admirals AI profile. Read
`Docs/AIIntegration.md` for the security/authority contract and
`Docs/AIControllerIntegration.md` for complete phase API examples.

## How AI selection works

```text
Singleplayer AI selector
        |
        v
selected ParticipantInfo
        |
        v
ParticipantSlot.SourceInfo
        |
        v
final MatchConfig copy
        |
        v
GameMaster reads ParticipantInfo.AIControllerPrefab
        |
        v
instantiate prefab + bind exact ParticipantAccess
        |
        v
ParticipantAIController implementation
```

The participant asset is the AI profile. It owns lobby identity data such as
name, portrait, and rank, plus the controller prefab that supplies behavior.
The lobby does not select a C# type directly, and the Game scene does not
contain permanent AI controller objects.

Different participant assets may reference different controller prefabs.
Multiple profiles may also deliberately share one prefab. Currently Random
Robo and Lt. Tensor both use the Random Robo baseline; Lt. Tensor can be changed
to the co-developer's smarter prefab without modifying `GameMaster` or the
lobby controller.

## 1. Create the controller class

Create a script under:

```text
Assets/Scripts/Game/AI/
```

Minimal adapter:

```csharp
using UnityEngine;

public sealed class StrategicAdmiralAIController : ParticipantAIController
{
    private GameParticipantAPI.ParticipantAccess _Access;
    private bool _DecisionRequested;

    protected override bool OnInitialize(
        GameParticipantAPI.ParticipantAccess participantAccess,
        out string validationMessage)
    {
        _Access = participantAccess;
        _Access.SnapshotChanged += RequestDecision;

        // SnapshotChanged is not replayed for late subscribers.
        RequestDecision();

        validationMessage = string.Empty;
        return true;
    }

    private void OnDestroy()
    {
        if (_Access != null)
            _Access.SnapshotChanged -= RequestDecision;
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

        if (snapshot.Match.MatchComplete)
            return;

        GameParticipantAPI.ParticipantCommand command = Decide(snapshot);
        if (command == null)
            return; // Waiting for the opponent or another state change.

        GameParticipantAPI.ParticipantCommandResult result =
            _Access.SubmitCommand(command);

        if (!result.Accepted)
            RequestDecision(); // Fetch fresh state and replan later.
    }

    private GameParticipantAPI.ParticipantCommand Decide(
        ParticipantGameSnapshot snapshot)
    {
        // Return at most one command from this observation.
        return null;
    }
}
```

Do not submit a command directly inside `SnapshotChanged`; successful commands
raise that event synchronously. Queue work for `Update`, a coroutine, or the
completion callback of an external model request.

## 2. Use the participant API

The controller may use only its retained `_Access` to:

- request `ParticipantGameSnapshot`;
- ask legality/range/preview queries;
- submit `ParticipantCommand`;
- receive filtered presentation events.

It must not find or read `GameMaster`, `GameMap`, `GameUnits`, `GameKnowledge`,
HUD objects, UnitView objects, or the opposing RuntimeParticipant. It must not
simulate clicks or implement a second set of legality rules.

Typical command creation:

```csharp
return GameParticipantAPI.ParticipantCommand.CommitMovement(
    snapshot,
    chosenUnit.RuntimeUnitID,
    chosenCoordinate,
    chosenFacing);
```

The factory copies the observed phase/turn token. Submission revalidates all
rules and rejects stale decisions safely.

## 3. Create the controller prefab

Create a plain prefab under `Assets/Prefabs/AI/`:

```text
AI_StrategicAdmiral_Controller
└─ StrategicAdmiralAIController
   └─ authored strategy/pacing fields
```

The prefab needs exactly one component derived from
`ParticipantAIController`. It needs no `GameMaster`, HUD, camera, map, or Unit
presentation references.

Keeping tuning values on the prefab allows multiple AI profiles to reuse the
same controller class with different aggression, pacing, or risk settings.

## 4. Create or update the participant profile

Create a `ParticipantInfo` asset through:

```text
Assets > Create > Admirals > Participant Info
```

Configure:

```text
Participant_NewAI
├─ Participant Type → AI
├─ Display Name → player-facing AI name
├─ Default Rank → desired rank asset
├─ Portrait Left / Right → desired portraits
└─ AI Controller Prefab → AI_StrategicAdmiral_Controller
```

`MatchConfig.ValidateGameplayReady` rejects an AI profile with a missing
controller prefab or a prefab lacking `ParticipantAIController`. This prevents
a match from silently launching with a dead opponent.

## 5. Add the profile to the Singleplayer selector

On the saved Frontend scene:

```text
Canvas_Frontend
└─ Screen_Singleplayer
   ├─ LobbyController
   │  └─ Available AI Infos
   │     ├─ Participant_RandomRobo
   │     ├─ Participant_AdmiralTensor
   │     └─ Participant_NewAI
   └─ .../Selector_AI
      └─ LobbyScreenController.AI Selector reference
```

The selector displays `ParticipantInfo.DisplayName`. Changing the choice
rebuilds Player 2's lobby slot from that profile, cancels Player 1's ready
state, and keeps the AI seat automatically ready. The selected `SourceInfo`
survives the lobby-to-match copy, which is how `GameMaster` resolves the exact
controller prefab.

Pass & Play does not populate `Available AI Infos`, so this selector/controller
path does not affect it.

## 6. Implement every phase termination path

A production controller must either act or explicitly finish its current
window:

```text
Deployment  place all entries, then ConfirmDeployment
Movement    CommitMovement or EndMovement
Pre-Battle  action/ability or EndPreBattle
Search      CommitSearch until quota, then EndSearch
Battle      engagement flow or EndBattle
End         optional ability or FinishEndPhase
```

During an engagement:

```text
AI attacker  BeginEngagement → LockBattleAttack → wait for defense
AI defender  wait for attack reveal → LockBattleDefense
Resolved     attacker controls ContinueEngagement after the reading window
```

Always stop when `snapshot.Match.MatchComplete` becomes true.

## 7. External-model integration

Keep model networking and model-specific DTOs behind the controller adapter.
The external system may receive a serialized form of the permitted snapshot
and return an untrusted proposal, but Unity must:

1. parse the proposal;
2. map it to a current `ParticipantCommand` factory;
3. supply the current observation token itself;
4. submit through the retained participant access;
5. handle rejection by obtaining fresh state.

Never let external output specify participant identity or authoritative object
references.

## 8. Validation checklist

- The participant appears in the Singleplayer selector with the correct name.
- Starting the match creates `AI_<DisplayName>` under `GameMaster`.
- The Console reports controller binding once and no repeated rejection loop.
- AI Deployment completes without exposing the opponent's tentative plan.
- Human HUD remains bound to the local participant during AI priority.
- Movement, Pre-Battle, Search, Battle, defense, and End all advance without
  manually controlling Player B.
- The AI cannot target concealed enemies that are neither contacts nor in
  absolute vision.
- Combat resolution remains readable before Continue.
- The AI stops after Victory, Defeat, or Draw.
- Return to Lobby and Pass & Play still work.

For a concrete complete baseline, compare against
`Docs/RandomRoboAI.md` and `RandomRoboAIController.cs`.
