# Systems and Combat

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

> **Superseded early brainstorming:** older notes described dedicated **CMS** and **Special** system slots, and a flat movement range. Current model: slots are Sensor / Defensive / Passive / Deployment; **FireControl (CMS)** is a *capability*; Deployment-slot systems carry `UnitDeployment` / `MineDeployment` capabilities. Trust the sections below and the repo.

---

## Systems

`Definition_System` is reusable authored system data.

Settled concepts:

- identity;
- Manufacturer;
- SystemType;
- Tier;
- Slot;
- eligible UnitTypes;
- operational Domains;
- operational phases;
- Pattern;
- charges/replenishment;
- capabilities.

Current capabilities include:

- Search;
- FireControl;
- Passive;
- Defense;
- UnitDeployment;
- MineDeployment.

### Pre-Battle

Pre-Battle is a participant-priority phase with a 60-second clock for each
participant. Units do not move during this phase. The active participant may
use only abilities and system capabilities authored for `PreBattle`, then ends
their action window (or times out) before priority passes to the opponent.

Mine deployment is submitted through the same participant-bound GameMaster
boundary as other gameplay actions. The authoritative transaction validates
the deploying Unit/System, authored OriginPoint pattern, charge, target tile,
and current Map occupancy before placing the Mine and spending one charge.

### End

End is a participant-priority phase with its own authored time limit (60
seconds by default). On entry, `GameMaster` resolves authoritative upkeep once:
status intervals, status/modifier duration ticks, `HealthPerEndPhase`, Unit
destruction caused by those effects, and Weapon/System replenishment progress.
Passive End modifiers respect target relationship, Domain, and authored
OriginPoint pattern range.

After upkeep, each participant receives an End action window and may inspect
their Units and use self-targeted abilities authored for `GameplayPhase.End`.
Ending the window or timing out passes priority. Completed-turn GameMode rules
are evaluated only after both participants finish End. If the Match remains
ongoing, GameMaster alternates first-player priority, increments the turn,
clears turn-only Search/contact knowledge, and begins Movement using the
GameMode-authored time limit. If a victory, defeat, or draw condition succeeds,
the Match remains concluded in End and no new phase accepts input.

### Current slot model

Current semantic slot model:

- Sensor;
- Defensive;
- Passive;
- Deployment.

Do not resurrect an older dedicated CMS/Special slot model from early brainstorming. FireControl/CMS behavior is a capability.

### Replenishment and charges

Weapon/System definition owns replenish rate + interval.

Unit slot owns max charges and runtime charge state.

---

## Search and Fire Control

Search and FireControl are distinct.

Search cares about operational domains, pattern, strength, and contact revelation.

FireControl enables engagement/attack range and targeting support. Fire control does not itself mean the target was detected.

Systems may be multi-role/domain-capable when authored that way.

---

## Passive Systems and Universal Modifiers

Passive systems may affect self, buff allies, debuff enemies, be phase-specific, and use domain/pattern areas.

Effects from multiple distinct valid sources may stack unless a specific rule says otherwise.

Reusable modifier concepts include:

- MovementRange;
- AttackRange;
- Replenishment;
- RollOutcome;
- StatusProtection;
- StatusInfliction;
- HealthPerEndPhase;
- Damage;
- Stealth;
- Search.

`GameplayModifier` itself carries ModifierType, Value, and ApplicablePhases. Do not casually move duration into it if duration is owned by application/status semantics.

### Modifier semantics (design intent)

Modifiers are one **universal** system usable by passives, statuses, abilities, and anything else. **Buffs apply only to allies; debuffs apply only to enemies.** Effects from multiple valid sources stack unless a specific rule says otherwise.

| Modifier | Meaning |
|---|---|
| MovementRange (bonus/penalty) | Integer. Each ±1 extends/shrinks the recipient's pattern by 1 at its last tile (enlarges/shrinks the whole pattern). |
| AttackRange (bonus/penalty) | Same behavior as MovementRange, applied to attack ranges. |
| Replenishment (bonus) | Integer. ± to the amount replenished each time the recipient replenishes. |
| RollOutcome (bonus/penalty) | Integer ± to roll results. |
| StatusProtection | % subtracted from enemy status-infliction chance on the recipient. |
| StatusInfliction | % added to the recipient's chance to inflict. |
| HealthPerEndPhase | Integer per turn, applied in End phase, allies only, **never negative**. |
| Damage (bonus/mitigation) | Integer ±. |
| Stealth (bonus/penalty) | Integer ±. |
| Search (bonus/penalty) | Integer ±. |

Passive systems may affect self only, buff allies (and self) in an area, or debuff enemies in an area; they are limited by their operational Domain(s) and may be phase-specific.

---

## Statuses

`Definition_Status` includes identity, duration mode/turns, interval, damage/healing per interval, and modifiers.

Duration modes: None, Permanent, Turns.

Same-status rule:

- identical status does not stack as duplicate independent instances;
- timed reapplication extends/adds duration turns.

---

## Gameplay Actions

`GameplayAction` is the reusable cross-system action payload.

Current action concepts include Damage, Health, AddTag, RemoveTag, ApplyStatus, RemoveStatus, ApplyModifier.

Unique features should reuse this shared effect infrastructure where applicable instead of reimplementing damage/status/tag logic.

---

## D20 Architecture

There is one reusable D20 infrastructure.

Rules:

- range 1–20;
- typed consumer payloads;
- validation covers 1–20 exactly once;
- final modified lookup clamps to 1–20;
- established attack/defense D20 types keep their `ValidateOutcome` naming.

Flat outcomes remain supported.

---

## Abilities

Abilities may apply tags/modifiers, deploy things, act in set phases, or provide offensive/defensive combat behavior.

They should reuse existing actions/modifiers/patterns/outcomes where possible.

Do not force all Unit classes to share identical ability or presentation capabilities.

---

## Weapon Model

A weapon definition includes identity, Manufacturer, multiple WeaponTypes, Tier, target domains, eligible UnitTypes, replenishment, flat/D20 outcome, and possible status afflictions.

Maximum charges are **not** on the weapon definition. Maximum charges belong to the Unit weapon slot.

### Weapon legality

Relevant legality includes correct role/phase, sufficient charges, legitimate target knowledge, target domain, fire-control/range support, and authored compatibility.

A hidden authoritative enemy is not targetable merely because it exists internally.

### Weapon details (design intent)

- A weapon may be used **only by the attacker in the Battle phase**.
- **Afflictions** (status + chance) can be configured to roll on hit, on miss, on both, or only on critical (in addition to the previous).
- **Outcomes** are flat/set or D20-roll-based (ranged weapons use set effects). Outcomes carry `Hit`/`Miss` results so other systems — notably inflicted effects — can hook in.
- **Targeting legality requires ALL of:** target Domain matches the weapon's target domains; an eligible FireControl capability (right Domain and range) exists; enough charges; the target is a labeled **contact** or inside **Absolute Vision**.
- Content direction: torpedoes (surface + subsurface), AAMs (aircraft), several ASM classes (heavier ships get harder-hitting ones; lighter ships/aircraft get others), depth charges (subsurface only). Adding Units means adding their systems, weapons, and outcomes.

---

## Attack Properties and Defense Interaction

Current attack properties:

- Critical;
- GuaranteedHit;
- BypassDefense.

### Critical
Flexible flag other systems/effects may react to.

### GuaranteedHit
Bypasses defense negation but can still be affected by mitigation/roll penalties and can be stripped by a defense designed to remove it.

### BypassDefense
Bypasses defense roll penalties, mitigation, and negation, but can still be stripped by an appropriate defense.

Do not collapse GuaranteedHit and BypassDefense.

---

## Defensive Reactions

At most two defenses participate in an engagement:

1. one allied defensive reaction;
2. the actual defender's defensive reaction.

No unlimited nearby defense stacking.

A defensive system is compatible with an incoming weapon when the weapon shares **any** compatible weapon type required by the defense.

Defense outcomes may alter rolls/damage, negate, strip attack properties, and invoke reusable gameplay actions.

---

## Visibility / Knowledge / Contacts

### Absolute vision
A tile under absolute vision is fully revealed. Legitimately visible enemy Units/mines can show actual presentation.

### Search
Search/contact reveals contact Domain, not automatically full Unit class/type identity.

Search is sequential by first-player priority and gives each participant a
90-second window. A participant's Search quota is half their current active,
on-map fleet rounded up, capped by the number of active Units with a usable
Search sensor. Each Unit may commit at most one Search per phase.

A sensor's OriginPoint pattern rotates around the Unit's OriginPoint. Every
pattern tile uses its authored layer strength plus current Search modifiers.
Sensors can detect only Units whose current Domain intersects the sensor's
authored Domains. For a multi-tile target, one Search uses the strongest
overlapping tile for the detection and stealth checks; success creates a
Domain-only contact on every overlapped footprint tile. Search power over 100
reduces current Stealth by the overflow before the concealment check. Enemies
already inside Absolute Vision do not receive redundant contact markers.

Committed pattern tiles become temporarily Obscured, play the authored Search
animation for two seconds, and then display successful Domain contact markers.
This temporary knowledge lasts through the current turn and clears when the End
phase concludes. Search can be ended manually only after the quota is spent;
the timer can always force the participant handoff.

### Searched tiles
Temporary searched tiles may have fog lifted for the current turn but remain muted; found contacts can show Domain indicators.

### Fog rollback
Temporary searched visibility rolls back next turn outside absolute vision.

### Targeting
Target must be legitimately known (contact or absolute vision) plus ordinary weapon legality.

---

## Combat Information Secrecy

Battle uses sequential first-player priority with a 90-second overworld window
per participant. The clock pauses while an engagement is open. Each active,
on-map Unit may initiate one engagement per Battle phase; a defender may be
engaged repeatedly by different Units. A participant may end Battle even when
unused attacks remain.

Fire Control is shown one Domain at a time. Every available onboard FireControl
system for that Domain contributes its rotated OriginPoint pattern to one union;
overlaps do not add strength. A known target is legal only when a compatible,
charged weapon reaches it through that union or an offensive Battle ability
reaches it through its own authored pattern.

The attacker locks one weapon or offensive ability first. That attack identity
is then revealed to the defender so a relevant reaction can be selected. The
defender may lock one own defense (or No Defense) and optionally one eligible
allied support defense. Allied support is legal when its authored pattern reaches
at least one tile in the defender footprint. Defensive choices remain hidden
from the attacker until resolution.

Resolution applies allied defense first, then the defender's own defense, and
only then looks up the final attack outcome after roll modifiers. Charges commit
when both sides are locked. Damage/destruction is immediate. The resolution
trace is revealed in order and Continue remains disabled for six seconds after
the complete trace is shown.

Do not expose hidden locked combat choices through participant-facing output.

---

## Game Modes and Victory

`Definition_GameMode` supports OR groups of AND win conditions. Scoring is separate.

Only Annihilation needs to be authored initially unless scope expands.

Do not hardcode Annihilation into generic GameMaster/turn logic when the definition system owns the condition.

GameMode data only describes conditions. `GameMaster` interprets their
evaluation moment:

- A group without `TurnCondition` or `ScoreAtTurnLimitCondition` is a current
  state rule and is checked immediately after authoritative state changes that
  can satisfy it. Once satisfied, the result is authoritative and further
  gameplay input stops. A just-resolved Battle engagement remains visible until
  its resolution is continued, then the game enters End.
- A group containing `TurnCondition` or `ScoreAtTurnLimitCondition` is a
  completed-turn rule and is checked after both participants finish End.
  Domination-style score/checkpoint modes belong here.

Definitions do not choose or execute evaluation timing. They provide the
condition composition; `GameMaster` interprets and enforces it.

Each group is evaluated from each participant's perspective: `Self` means that
participant and `Opponent` means the other participant. Every condition inside
one group must pass; satisfying any group of that type satisfies that Victory
or Defeat route.

A participant wins by satisfying one of its Victory groups or when the opponent
satisfies one of its Defeat groups. If the same End checkpoint produces a win
for both participants, the authoritative result is a draw. This preserves
simultaneous resolution instead of privileging whichever side happens to be
evaluated first.

The current score source is `EnemyUnitDestroyedScoring`: each participant's
score is the number of enemy fleet members no longer remaining multiplied by
the authored points per Unit. `RemainingFleetSize` includes carried Units, so a
carried Unit lost with a destroyed carrier also contributes to loss/scoring
state. Score-at-turn-limit conditions evaluate at the initial limit and then at
each authored tie-extension checkpoint until one side has the required score
relationship.
