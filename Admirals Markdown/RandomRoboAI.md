# Random Robo Decision Policy

This document describes the implemented behavior of
`RandomRoboAIController`. It is a baseline playable opponent and an API
integration test, not the intended ceiling for Admirals AI quality.

Current source:

```text
Assets/Scripts/Game/AI/RandomRoboAIController.cs
Assets/Prefabs/AI/AI_RandomRobo_Controller.prefab
Assets/Data/Participants/Participant_RandomRobo.asset
```

## Runtime boundary

Random Robo derives from `ParticipantAIController`. `GameMaster` gives it one
`GameParticipantAPI.ParticipantAccess` already bound to its participant.

It only reads `ParticipantGameSnapshot`, public authored definitions exposed by
that snapshot, and participant queries such as movement validation, Search
patterns, and Battle targets. Every mutation is a `ParticipantCommand`
submitted through its bound access.

It never reads `GameMap`, `GameUnits`, hidden enemy RuntimeUnits, HUD objects,
sprites, or the human participant's private snapshot.

## Decision loop

1. `GameMaster` instantiates the controller prefab selected by
   `ParticipantInfo`.
2. `OnInitialize` retains the supplied participant access and subscribes to
   `SnapshotChanged` and `SearchPresented`.
3. A notification schedules a decision after a randomized delay authored on
   the prefab, currently 0.35-0.75 seconds.
4. The controller fetches a fresh snapshot.
5. It produces at most one command and submits it.
6. A rejected or stale command causes a fresh delayed decision.
7. It stops producing commands as soon as `snapshot.Match.MatchComplete` is
   true.

Commands are deliberately not submitted directly inside `SnapshotChanged`,
because successful commands synchronously publish another snapshot change.

## Persistent match memory

Random Robo keeps three small participant-legal memories:

- last observed position of each of its own Units;
- visit count for coordinates occupied by its own Units;
- coverage count for coordinates included in its committed Search patterns.

These memories improve exploration without granting hidden information. They
are discarded with the controller at the end of the match.

## Deployment

For one unplaced Fleet Entry, Random Robo:

1. reads its permitted deployment zone;
2. randomizes the starting Entry, coordinate, and facing order;
3. asks `ValidateDeploymentPlacement` for each proposal;
4. submits the first legal placement;
5. confirms once the entire plan is placed.

Opponent tentative deployment is never visible to it.

## Movement

The controller enumerates Units that have not moved, obtains each Unit's legal
movement area, and validates coordinate/facing combinations through the API.

When a contact or absolute-visible enemy exists, movement scores favor the
smallest Manhattan distance to a known enemy coordinate.

When no target is known, movement scores favor:

- coordinates closer to currently fogged tiles;
- coordinates the AI has visited fewer times;
- actually changing position rather than remaining still;
- a small random tie-break value.

This makes the fleet sweep outward instead of wandering entirely at random.
It still has no knowledge of where a concealed enemy actually is.

## Pre-Battle

The prefab authors a probability of attempting a Pre-Battle action, currently
65 percent. Random Robo enumerates legal candidates exposed through:

- mine deployment area and validation;
- carried-Unit deployment area and validation.

It randomly chooses one legal candidate and then ends Pre-Battle. If it elects
not to act or finds no candidate, it ends immediately. The baseline currently
does not evaluate general Pre-Battle abilities.

## Search

For every unused active Unit, Search system, and facing, Random Robo requests
the real Search pattern from the API.

Each pattern receives:

- a large bonus for covering a known contact/enemy coordinate;
- a bonus for tiles searched fewer times during this match;
- an additional bonus for fogged tiles;
- a smaller bonus for obscured tiles;
- a random tie-break value.

It submits the highest-scoring Search until its authored Search quota is met,
then ends Search. If no legal unused sensor exists before the quota is met, it
logs one diagnostic for that turn and lets the authoritative phase timer handle
the otherwise impossible remainder.

Search success and stealth resolution remain entirely authoritative.

## Battle overworld

Random Robo builds possible target coordinates from only:

- its current Search contacts;
- footprints of absolute-visible enemies.

For every ready attacker/known coordinate pair it calls `TryGetBattleTarget`.
That query enforces Fire Control, target Domain, charge state, and the presence
of a compatible weapon or attack ability.

Legal targets score by:

- available weapon count;
- available attack-ability count;
- a small bonus for absolute-visible targets;
- a random tie-break value.

If no engagement is legal, Random Robo ends Battle and logs one participant-
safe reason for the turn:

- no ready attacker;
- no contact or absolute-visible target; or
- known targets existed, but none were in legal Fire Control with a compatible
  action.

The diagnostic never reports concealed authoritative enemy facts.

## Engagement

As attacker, Random Robo randomly chooses one legal attack option. If an opened
engagement unexpectedly has no legal attack option, it cancels it.

As defender, it independently decides whether to use:

- one defender action, currently with an 85 percent chance when available;
- one allied support action, currently with a 65 percent chance when available.

All choices come from the filtered `EngagementView`; the opponent's locked
choice is not inspected before reveal.

When Random Robo is the attacker, it waits six seconds after resolution before
submitting Continue. When it is the defender, the human attacker retains
control of Continue.

## End

The baseline submits `FinishEndPhase` when it has End priority. Automatic
upkeep, replenishment, effect ticks, victory evaluation, and turn advancement
remain owned by the game. Random Robo currently does not evaluate optional
End-phase abilities.

## Known limitations

- Exploration uses Manhattan distance and visit history, not coordinated fleet
  search sectors or opponent prediction.
- It does not remember expired enemy contacts as suspected positions.
- It does not reason about weapon outcome tables, health, stealth probability,
  mine value, ability synergy, or victory-condition priorities.
- Pre-Battle and End ability selection is intentionally incomplete.
- Several Units can pursue the same coordinate.
- Random tie-breaks mean identical games can produce different decisions.

These are strategy limitations, not API limitations. A smarter controller can
replace the scoring and planning while retaining the same participant access
and command boundary.
