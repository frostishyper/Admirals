# Map and Units

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

---

## Map Authoring and Runtime Board

### Map is a prefab

A playable map is an authored prefab containing `MapAuthoring`.

GameMaster instantiates it at runtime and initializes/attaches runtime `GameMap`.

Current test maps have demonstrated boards such as `64x34`, but map size is authored and must not be hardcoded.

### Terrain/domain permission

Current terrain concepts include:

- Deep Water: Air / Surface / Subsurface;
- Shallow Water: Air / Surface;
- Land: Air;
- Barrier: none.

The map determines whether the Unit's current Domain is physically allowed.

### One Unit per tile

At most **one Unit occupies a tile**, regardless of Domain.

Do not allow air/surface/subsurface Unit stacking because their domains differ.

### Runtime Unit identity in occupancy

Board occupancy uses runtime Unit identity, not globally unique display names.

The settled model uses `RuntimeUnit` references as occupancy keys. Both participants may legitimately have an instance such as `Rover Class 1`.

Do not revert to string instance-name occupancy keys.

### Unit position ownership

`RuntimeUnit` does not own board position. `GameMap` owns coordinates/occupancy.

When a Unit is destroyed, remove board occupancy but retain its runtime record for scoring/history/rules.

### Domain changes

A runtime Unit may change domain only when its current occupied footprint is legal for the requested domain.

### Mines

Mines are separate board entities, not full RuntimeUnits.

Settled behavior:

- max one mine per tile;
- enemy surface/subsurface movement onto a mine can trigger it;
- friendly surface/subsurface units should not occupy their own mined tile;
- air units pass above without triggering;
- mine knowledge follows map/visibility rules.
- mines retain their source Deployment System so their authored flat or D20
  outcome can resolve when triggered;
- a triggering mine is removed immediately and its damage resolves during the
  movement step before any later route step is attempted;
- a Unit reduced to zero health by a mine is removed from Map occupancy while
  its destroyed RuntimeUnit record remains in GameUnits.

### Participant knowledge is not physical tile truth

Fog/visibility/contact knowledge should not be baked into one universal RuntimeTile truth when it differs by participant.

### Invalid authoritative movement

Impossible requested moves/placements are rejected. Corrupt internal state should log clearly rather than silently inventing a recovery.

---

## Unit Definition Model

`Definition_Unit` is authored Unit-class data, not a runtime individual.

Multiple runtime instances may share one definition.

Settled authored fields/concepts:

- Name;
- ID;
- Manufacturer;
- `UnitTypes[]`;
- Tier;
- BaseDomain;
- Health;
- Stealth;
- AbsoluteVision;
- FootprintSize;
- OriginPoint;
- MovementPivot;
- MovementPattern;
- weapon slots;
- system slots;
- abilities.

Do **not** put presentation fields in `Definition_Unit`.

Do **not** reintroduce a flat integer `MovementRange`. Movement is pattern-driven.

### Classifications

Manufacturer, UnitType, Tier, WeaponType, SystemType, etc. are authored classification definitions.

The user prefers `Definition_*` naming for authored definitions.

### IDs

IDs are user-authored strings with semantic naming, e.g. `weapon_aam_bsd_tidebreaker`.

Do not replace them with opaque auto-generated gameplay IDs unless explicitly requested.

---

## Footprints, Origin, Pivot, Facing, Movement

### Cardinal facing

Units use four cardinal directions. Facing affects footprint orientation, origin orientation, movement-pivot relationship, and presentation.

### OriginPoint â‰  MovementPivot

`OriginPoint` is where origin-based patterns emanate from.

`MovementPivot` is the tile about which the Unit footprint is positioned/rotated during movement/deployment and is often toward the rear of a ship.

Do not merge them.

### Footprint

A Unit footprint is contiguous authored tile geometry. The full rotated footprint must be legal.

Sprite bleed outside the exact footprint is acceptable and presentation-only.

### Movement pattern

Movement uses reusable `Definition_Pattern` data.

`PatternMode` distinguishes `OriginPoint` and `FootprintMovement`.

For footprint movement:

- authored local orientation is East canonical;
- movement starts from actual pivot/facing;
- do not recenter;
- full final footprint must fit legally;
- pivot follows snapped tile during placement/movement.

The authored movement pattern plus the Unit's starting footprint forms the
complete maneuver area. A committed destination is legal only when every tile
of the final rotated footprint fits inside that area. Runtime routing advances
through cardinal translations and pivot rotations; sideways translation does
not automatically change facing.

Movement route planning uses participant-known terrain and visible occupancy.
Unknown tiles are not queried through participant output. During authoritative
resolution, each route step is checked against physical `GameMap` truth; a
hidden obstruction interrupts the move at the last legal step without exposing
the obstruction's identity.

Participant output may expose the selected owned Unit's authored maneuver-area
coordinates because they contain no hidden board state. That area is only a
presentation/manipulation envelope: final candidate legality and committed
resolution still come from `ValidateMovement` and `TryCommitMovement`.

---

## Patterns

Patterns are reusable authored geometry for movement, search, fire-control/attack range, passive/defensive areas, deployment, etc.

Search strength belongs to the relevant search capability, not the generic pattern asset.

Do not create one pattern class per consumer when generic patterns already express the geometry.

---

## Runtime Units

A `RuntimeUnit` is the mutable match instance of a `Definition_Unit`.

Settled runtime concerns include:

- SourceFleetEntryID;
- side-local instance name;
- Definition;
- Owner;
- life/death state;
- current health/domain/facing;
- independent weapon/system states;
- abilities;
- statuses/modifiers/tags;
- carried manifests.

### Instance naming

Instance names are side-local and may repeat across participants.

### Carried Units

A carried Unit remains a carried record until deployed. Once deployed, it cannot return to being carried.

If a carrier dies while Units remain embarked, those Units are lost with it and still count for fleet/scoring state.

Embarked Units are not top-level initial Deployment cards.

---

## Fleet Model

Conceptually:

```text
Definition_Fleet
â”œâ”€ Name
â”œâ”€ ID
â””â”€ Entries[]
   â”œâ”€ Entry ID
   â””â”€ Definition_Unit
```

One top-level Fleet Entry corresponds to one eventual top-level RuntimeUnit instance.

Initial Deployment uses top-level Fleet Entries only.

---

## Current Content / Unit Targets

The game must support at minimum archetypes including:

- mine-laying/skirmishing frigate;
- fleet-protecting destroyer;
- offensive cruiser;
- fast attack submarine;
- carrier;
- naval strike fighter.

Do not hardcode gameplay around only currently integrated Rover and Gryphon content.

---

## Unit Tags and Additional Unit Rules

- A Unit may have **multiple UnitTypes**; where it may go is decided by the map's interaction with its UnitTypes and current Domain.
- **Tags** can modify a Unit at runtime â€” e.g. change presentation (sprite/animation state) or change Domain. Known example: `Submerged` (changes Domain and visual). More tags will be added; tag effects should flow through existing `GameplayAction` (`AddTag` / `RemoveTag`) and modifier infrastructure.

`Definition_UnitTag` is the finite authored source for a tag's identity and
gameplay consequences. `GameplayAction` and `RuntimeUnit` hold references to
that definition rather than free-form tag strings. `GameUnitEffects` resolves
the complete resulting tag set, validates any derived Domain against the
Unit's current occupied footprint through `GameMap`, then commits tags and
Domain together. The authored Submerged tag overrides the current Domain to
`Subsurface`; removing it restores the Unit's authored `BaseDomain` when no
other Domain override remains.
- **Stealth** and **AbsoluteVision** are Unit stats. Absolute Vision includes every tile of the Unit's complete current footprint, then expands outward in square/Chebyshev rings from that footprint, including diagonals. Friendly Units remain fully known to their owner. *(Open: whether modifiers/systems may alter AbsoluteVision.)*
- **Stats are data:** every stat/definition field should be queryable by HUD and AI even if a given consumer ignores it. The HUD decides what to show.
- A destroyed Unit leaves the map but its runtime record persists (scoring/history/rules).


