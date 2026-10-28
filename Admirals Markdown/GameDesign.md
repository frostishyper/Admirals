# Game Design

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

Original design intent. Where this conflicts with settled architecture in `AGENTS.md` / other `Docs/` files, the settled architecture wins — list the conflict under *Unresolved* below rather than guessing.

---

## Concept

*Admirals* evolves the blind-fire suspense of *Battleship* into a grid-based tactical RPG for PC. It combines the spatial maneuvering of **Fire Emblem / Advance Wars / Polytopia / Unicorn Overlord** with tabletop-style **D20** combat resolution. Setting: modern/future naval combat (missiles and electronic systems).

**Three pillars:** Reconnaissance · Positioning · Asymmetric Combat Resolution. Information is as critical as firepower.

### Shared grid and fog of war
Both fleets occupy one large grid, heavily shrouded in Fog of War. Targets must be actively spotted before they can be attacked; vision is the most valuable resource. Units use absolute passive vision (a fixed radius) plus active intelligence-gathering during Search. Moving blind risks ambushes.

### Tactical maneuvering and range
Every unit has distinct movement and attack ranges (pattern-driven). Players manage spacing and overlapping fields of fire/vision. Scouts: wide movement/search, limited range. Heavy ships: precise positioning to strike from distance without retaliation.

### Victory and attrition
Goal: total annihilation of the opposing fleet. Pacing ramps from a tense, stealthy early game to an explosive late game as the map opens up.

---

## Vision and Fog of War States

Admirals uses **three distinct visibility states**. These are the visual representation of the underlying participant-specific visibility/knowledge data.

### Absolute Vision

Absolute Vision is the fully revealed state.

- No fog art or fog overlay is present.
- No terrain obscuration is applied.
- Terrain/tile art is shown normally.
- Enemy Unit sprites are shown fully when they occupy tiles currently under that participant's Absolute Vision.
- Other legitimately visible entities on those tiles are likewise presented without fog/obscuration.

This is the clearest and strongest visibility state.

### Obscured

Obscured means the terrain is revealed, but the tile is not under full Absolute Vision.

Visual presentation:

- the actual fog art/sprite is absent;
- terrain/tile art remains visible;
- terrain is darkened, greyed out, or otherwise visually muted;
- dynamic information is only shown to the extent that the participant's underlying knowledge allows.

This state appears most commonly as a result of the **Search phase**.

When Search lifts fog from tiles that are not already under Absolute Vision:

- those tiles become visually Obscured rather than fully clear;
- the terrain becomes visible in its muted/greyed presentation;
- if Search acquired a contact, the appropriate contact tile marker appears;
- the contact marker reflects the Domain of the acquired contact.

Obscured presentation must reflect the participant's actual knowledge state. It must not reveal hidden Unit identity, position, or other information that the player has not legitimately acquired.

### Fog

Fog is the completely unrevealed state.

- The tile is fully covered by the fog art/overlay.
- Terrain/tile art beneath it is not revealed to the participant.
- Enemy Unit presentation beneath it is not revealed.
- No information is visually exposed except information explicitly permitted by some separate gameplay rule.

### Visibility is data first, presentation second

These three states are visual representations of participant-specific visibility data.

The rendering layer must not decide what the participant knows by itself.

Conceptually:

```text
participant visibility / knowledge data
                ↓
     Absolute Vision / Obscured / Fog
                ↓
        terrain + marker + Unit presentation
```

Presentation must never reveal more than the underlying participant-visible game state permits.

---

## Turn and Phase Structure

Admirals progresses **phase by phase**, not by giving one player an entire full turn before the other player acts.

### Simultaneous phases

Two phases are simultaneous for both sides:

- **Deployment**
- **End**

Deployment happens at the same time for both participants.

End also resolves as a simultaneous phase rather than one side receiving priority over the other.

### First-player selection

At the start of the match, the side that acts first is chosen randomly with a **50/50 chance** between the two participants.

That first-player designation applies to the ordered phases of that full turn.

### First player alternates each full turn

After a complete turn finishes, first-player priority switches to the other participant.

It continues alternating back and forth every full turn until the game ends.

Conceptually:

```text
Turn 1 → Participant A goes first
Turn 2 → Participant B goes first
Turn 3 → Participant A goes first
Turn 4 → Participant B goes first
...
```

The first-player designation does **not** randomly reroll every phase or every turn.

After the initial 50/50 selection, it alternates deterministically.

### Ordered phases resolve both participants before advancing

For an ordered gameplay phase such as Movement:

1. the participant with first-player priority performs their Movement actions/commands;
2. the other participant then performs their Movement actions/commands;
3. only after both sides have completed that phase does the game advance to the next phase.

The same phase-ordering principle applies to the other non-simultaneous gameplay phases.

Conceptually:

```text
Movement
├─ First Player acts
└─ Second Player acts
        ↓
Pre-Battle
├─ First Player acts
└─ Second Player acts
        ↓
Search
├─ First Player acts
└─ Second Player acts
        ↓
Battle
├─ First Player acts
└─ Second Player acts
        ↓
End
└─ Simultaneous
        ↓
next full turn
→ first-player priority switches
```

Therefore, "who goes first" means **who receives first action priority inside each ordered phase of that turn**, not who receives an entire uninterrupted full turn.

---

### Detection, Contacts, and Targetability

Searching a tile does **not** guarantee that an enemy Unit occupying that tile will be detected or produce a contact marker.

Search is an attempted detection process. Whether a Unit is actually detected depends on the interaction between factors such as:

- the searching system's **Search strength** on the relevant tile;
- the target Unit's **Stealth**;
- the Search pattern and the strength/value authored for that specific tile;
- any other applicable modifiers or effects.

The exact formula or resolution rule for how **Search strength and Stealth interact is not yet decided**. Do not invent or hardcode this interaction until the design is finalized.

Conceptually:

```text
Tile is searched
      ↓
Detection check / resolution
(Search strength vs target Stealth, exact rule TBD)
      ↓
┌────────────────────┬────────────────────┐
│ Detection succeeds │ Detection fails    │
│                    │                    │
│ Contact acquired   │ No contact marker  │
│ Contact marker may │ Enemy remains      │
│ be shown           │ unknown            │
└────────────────────┴────────────────────┘

A contact marker therefore represents successfully acquired knowledge, not merely the fact that Search affected a tile.

The marker shown for a successfully acquired contact reflects the detected contact's Domain without automatically revealing the enemy Unit's full identity.

Contacts and Absolute Vision as Target Knowledge
An enemy may become legitimately known for targeting in either of two ways:
1. it has been successfully acquired as a contact through Search or another valid detection rule; or
2. it is currently inside the participant's Absolute Vision, where the actual enemy Unit is fully revealed.
Either form of legitimate knowledge can make that enemy a candidate target once the appropriate combat phase occurs.

Knowledge alone does not make an attack legal.
For an enemy Unit to actually be targetable, the attacking Unit must also satisfy the ordinary engagement requirements, including:
- the target is a valid acquired contact or is currently revealed by Absolute Vision;
- an appropriate FireControl-capable System can support the engagement;
- that FireControl capability supports the target's Domain;
- the target is within that FireControl system's valid range/pattern;
- the attacking Unit has a Weapon capable of engaging the target's Domain;
- all other applicable weapon, phase, charge, status, and gameplay restrictions are satisfied.

Conceptually:

Legitimate target knowledge
(Contact OR Absolute Vision)
            +
Valid FireControl support
(domain + range/pattern)
            +
Compatible usable Weapon
            ↓
Potentially legal Battle target
            ↓
ordinary authoritative legality checks

A Unit being internally present on the authoritative board is never sufficient by itself to make it targetable.
Targetability must follow what that participant has legitimately detected or fully revealed, together with the attacking Unit's actual FireControl and Weapon capabilities.

---

## Unresolved / Needs Developer Decision

1. **Scheme-driven combat.** The original design described Offensive Schemes (e.g. Armor-Piercing Barrage, Area Suppression, Precision Strike) vs Defensive Schemes (e.g. Evasive Maneuvers, Brace for Impact, Deploy Countermeasures) with Pokémon-style typing interaction. The settled architecture models combat via weapons, defensive reactions, abilities, attack properties, and D20 outcomes. It is **undecided** whether Schemes still exist and how they relate (possibly the player-facing layer over weapons/defenses). Do not implement Schemes without asking.
2. **Tier scale/meaning** is not defined.
3. **"Old conventions."** Earlier notes referenced FireControl/radar/sonar conventions and an authoring workflow "from last time" that are not written down. Ask rather than assume.
4. **Turn/movement animation smoothing** for Units without dedicated turn art — developer wants help designing this (presentation-only; must not change gameplay facing rules).
5. **Absolute Vision modifiability** (see `Docs/MapAndUnits.md`).
6. Exact participant-filtered output API and shared input API names (see `AGENTS.md` → *What Is Not Yet Finalized*).

---

## Content Templates

### Unit entry
Map sprite & portrait · Name · UnitType(s) · Role · Manufacturer · Tier · BaseDomain · Health · Stealth · AbsoluteVision · FootprintSize · OriginPoint · MovementPivot · MovementPattern · weapon slots · system slots (Sensor / Defensive / Passive / Deployment) · abilities (each system: Name, Manufacturer, Pattern/Range, Type(s), Tier, max charges).

### System entry
Name · Manufacturer · SystemType · Tier · Slot · eligible UnitTypes · Domains · phases · Pattern · capabilities · replenish rate/interval · effect (D20 roll-range outcomes or flat).

### Weapon entry
Name · Manufacturer · WeaponType(s) · Tier · target Domains · eligible UnitTypes · replenish rate/interval · outcome (flat or D20) · possible afflictions (+ chance and trigger).

---

## Lore: Military-Industrial Complexes (MICs)

Manufacturer-owned content is organized by company (see GameData organization in `Docs/Architecture.md`). Each company entry: **Company Name**, **Description & Lore**, **Products** (System/Weapon/Unit name, type, tier, description). _No entries recorded here yet — the authored `Manufacturer_*` assets are authoritative._
