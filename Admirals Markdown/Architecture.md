# Architecture

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

The authority diagram and ownership summary are in `AGENTS.md`. This file covers orchestration, handoff, shared types, and organization.

---

## Runtime Participants and Sides

A runtime participant identifies the participant slot and assigned fleet and carries deployment-lock state.

Keep distinct:

- participant identity / side;
- control source (human/AI);
- runtime Unit roster (`GameUnits`);
- tentative deployment plan (`GameDeployment`).

Do not collapse these into a UI/controller object.

An authored AI `ParticipantInfo` selects a `ParticipantAIController` prefab.
After authoritative match initialization, `GameMaster` instantiates that
prefab and supplies only the exact participant-bound `ParticipantAccess`.
Controller prefabs are strategy adapters, not gameplay authorities, and are
not permanently authored into the Game scene.

Pass & Play uses the same finalized Match and shared Game scene. A Match with
two `LocalPlayer` slots enables the local privacy-handoff presenter; a normal
LocalPlayer + AI Match does not instantiate or run that presenter. Handoffs
change only the participant-bound human presentation and pause phase clocks
while the screen is concealed. They do not create a second gameplay-rules path.

---

## Participant API and Knowledge Boundary

`GameMovement` owns Movement-phase limits, per-Unit committed-move state,
participant end state, first-player-priority handoff, the active participant's
phase clock, known-information route planning, and stepwise movement resolution.
Only the active participant may submit Movement actions, but both participants
continue receiving refreshed participant-filtered snapshots after committed
state changes. Phase time limits are authored by `Definition_GameMode`.
`GameMap` remains the owner of physical occupancy, while
`GameKnowledge` remains the owner of participant-specific explored terrain.

`GameParticipantAPI` is the participant-facing integration boundary for
human controllers, AI controllers, and future networking adapters. Controllers
receive a participant-bound access object rather than selecting an arbitrary
participant on each call.

Non-UI controllers may express one decision as a plain
`GameParticipantAPI.ParticipantCommand` and submit it through the bound
`ParticipantAccess.SubmitCommand`. The command never carries participant
identity; the access object supplies the actor. Command dispatch reuses the
same typed GameMaster input methods as human control and therefore owns no
parallel legality or resolution rules. `ParticipantCommandResult` reports
acceptance, stale phase/turn rejection, diagnostics, and any structured phase
result produced by the command.

`GameKnowledge` is a substantial authoritative subordinate of `GameMaster`.
It owns participant-specific visibility and learned-information state. It reads
physical truth from `GameMap` and runtime Unit truth from `GameUnits`, but
does not replace either owner.

Participant snapshots are copied projections. They must not expose live
`RuntimeUnit`, `RuntimeMapTile`, opponent roster, or other hidden
authoritative references.

`GameMaster` owns match lifecycle interpretation. It interprets GameMode groups
without turn/checkpoint conditions as immediate state rules, and groups with
`TurnCondition` or `ScoreAtTurnLimitCondition` as completed-turn rules checked
after both participants finish End. It stores one authoritative
match-complete/winner/draw result;
`GameKnowledge` exposes only the participant-relative outcome and scores in
`ParticipantGameSnapshot.Match`. Phase systems and presentation do not decide
victory independently.

After presenting a terminal result, the authored End HUD exposes a Return to
Lobby command. Presentation clears `MatchSession.ActiveMatch` and loads the
`Frontend` scene; it does not alter or reinterpret the authoritative result.

## Match / Frontend / Game Handoff

Established flow:

```text
Lobby
  â†“ ready
10-second ready countdown
  â†“
MatchConfig
  â†“
MatchSession
  â†“
Game scene
  â†“
GameMaster initializes gameplay
```

Rules:

- No extra Start button after the ready flow.
- Game scene is entered through a finalized Match.
- `MatchConfig` resolves map prefab, game mode definition, participant fleets.
- `MatchSession` persists across scene transition.
- Game scene does not permanently contain the authored gameplay map.
- `GameMaster` instantiates the selected map prefab at runtime.
- `MapInfo` references map prefab.
- `GameModeInfo` references `Definition_GameMode`.
- `MatchLaunchBridge` performs the handoff.

Do not bypass this with production static test state.

---

## Current GameMaster Initialization Model

Conceptual order:

1. retrieve finalized active Match;
2. validate gameplay readiness;
3. initialize participants;
4. instantiate/initialize map;
5. initialize Unit state;
6. initialize Deployment state;
7. prepare Deployment;
8. mark initialization complete.

Prepare Deployment:

- phase = Deployment;
- both participant deployment states unlocked;
- both plans reset;
- no RuntimeUnits created;
- match not started.

Preserve dependency order unless a concrete need requires change.

---

## Shared Semantic Types

Preserve established semantic distinctions and explicit enum numbering where already defined.

Important current concepts:

- Domain flags: None, Air, Surface, Subsurface, All.
- GameplayPhase flags: None, Deployment, Movement, PreBattle, Search, Battle, End, All.
- SystemSlotType: None, Sensor, Defensive, Passive, Deployment.
- PatternMode: OriginPoint, FootprintMovement.
- CombatRole flags: None, Attacker, Defender, Both.
- GameplayTarget flags: None, Self, Ally, Enemy, Contact, Tile.
- OutcomeResult: None, Hit, Miss.
- AttackProperty: None, Critical, GuaranteedHit, BypassDefense.
- OutcomeTrigger flags: None, Hit, Miss, Critical.
- ModifierType: None, MovementRange, AttackRange, Replenishment, RollOutcome, StatusProtection, StatusInfliction, HealthPerEndPhase, Damage, Stealth, Search.
- DurationMode: None, Permanent, Turns.
- GameplayActionType: None, Damage, Health, AddTag, RemoveTag, ApplyStatus, RemoveStatus, ApplyModifier.
- AbilityOutcomeType: None, Actions, Attack, Defense.

Where current code uses explicit numeric values, preserve them. Reordering enums can break serialized assets.

---

## Authored Data Organization

Conceptual gameplay script structure:

```text
Assets/Scripts/Game/
â”œâ”€ AI/
â”œâ”€ Editor/
â”œâ”€ GameMaster/
â”œâ”€ HUD/
â”œâ”€ Map/
â”œâ”€ Systems/
â”œâ”€ Units/
â”œâ”€ Weapons/
â”œâ”€ Classifications/
â””â”€ Definitions/
```

Shared definitions stay grouped rather than one folder per tiny type.

Avoid one-item nested folders.

---

## GameData Organization

Manufacturer/MIC-first organization for manufacturer-owned content:

```text
Assets/GameData/
â”œâ”€ Abilities/
â”œâ”€ Classifications/
â”œâ”€ Effects/
â”œâ”€ Fleets/
â”œâ”€ GameModes/
â”œâ”€ Maps/
â”œâ”€ MIC_BlackShoreDefense/
â”‚  â”œâ”€ Manufacturer_...
â”‚  â”œâ”€ Units/
â”‚  â”œâ”€ Weapons/
â”‚  â”œâ”€ Systems/
â”‚  â””â”€ Patterns/
â””â”€ MIC_<OtherManufacturer>/
   â”œâ”€ Manufacturer_...
   â”œâ”€ Units/
   â”œâ”€ Weapons/
   â”œâ”€ Systems/
   â””â”€ Patterns/
```

Exact repo names are authoritative.

Do not infer manufacturer from old examples. Gryphon is explicitly **not** Black Shore Defense; inspect its authored Manufacturer.

Names of weapons/systems may have changed. Trust current assets.

---

## Authoring Philosophy

Prefer reusable ScriptableObjects for independently reusable authored definitions and embedded `[Serializable]` data when a record only makes sense inside an owning definition.

Avoid a ScriptableObject for every tiny nested record.

Use shared systems for common effects. Unique mechanics are allowed to have substantial code when truly unique, but should still reuse generic actions/modifiers/patterns where possible.

---

## Planned Tooling

- Re-import / upgrade a **SerializeReference** editor extension so polymorphic embedded data can be authored inline (prefer internal serializable data over extra ScriptableObjects where it doesn't need independent reuse).
- A better custom **ScriptableObject selector with filtering**, so pickers don't list every SO of a matching type regardless of fit (e.g. filter by Domain, slot/capability, or eligible UnitType).
- A **pattern maker** that supports both modes: `FootprintMovement` (entire footprint must fit) and `OriginPoint` (emanates from a single point). See `Docs/MapAndUnits.md`.
- Goal: **less authoring fragmentation**. Adding a Unit implies adding its systems, weapons, and outcomes â€” keep the data model friendly to bulk authoring.

---

## Code Map (fill in as the project grows)

### Core systems
| Module / System | Description | Status / Ver | Scripts |
|---|---|---|---|
| _TBD_ | | | |

### Scripts
| Script | Purpose | Status / Ver | Inputs | Outputs |
|---|---|---|---|---|
| _TBD_ | | | | |

### Non-script files
| File | Purpose | Status / Ver | Inputs | Outputs |
|---|---|---|---|---|
| _TBD_ | | | | |


