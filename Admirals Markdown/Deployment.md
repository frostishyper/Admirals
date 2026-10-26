# Deployment

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

Current active work area (Deployment Pass 3). **Do not rebuild the working card/tray unless a real defect requires it.**

---

## Deployment Input/Output Contract

### Fundamental placement input

A tentative deployment placement is fundamentally described by:

```text
Participant
FleetEntryID
MovementPivotCoordinate
Facing
```

It is **not** described by pointer position, screen position, world mouse position, UI card object, `UnitView`, sprite, `RectTransform`, or `PointerEventData`.

Human input converts screen/world data to a board coordinate before submitting gameplay data. AI can submit board-coordinate data directly.

### Validate vs submit

The authoritative Deployment API should conceptually support:

- validate proposed placement without mutation;
- submit proposed placement with authoritative revalidation and mutation.

A drag ghost needs to ask “would this be legal?” continuously without changing authoritative state.

Do not lock exact method names before inspecting current repo code.

### One placement concept can cover place/move/rotate

The authoritative state can simply be:

```text
Fleet Entry X
Pivot = coordinate
Facing = cardinal direction
```

Submitting new values for an already tentative Fleet Entry naturally moves/rotates it. Human UI may still expose drag and rotate controls.

### Deployment output during setup

A participant should be able to read:

- current phase;
- own participant identity;
- own top-level deployable Fleet Entries;
- own tentative placements;
- own lock/confirmation state;
- legal public/static map information needed for deployment;
- own deployment zone;
- validation results.

A participant must not receive opponent tentative coordinates/facing/placement state.

“Not rendered” is not sufficient. Hidden opponent deployment state should not be exposed through the participant-facing API.

---

## Map Conditions and Deployment Zones

Map conditions include deployment-zone concepts.

Deployment placement needs both:

1. **physical validation** — tile existence, terrain/domain compatibility, footprint legality, occupancy;
2. **deployment-rule validation** — correct participant, phase, lock state, Fleet Entry, own deployment zone, tentative-plan conflicts.

Keep responsibilities separated:

```text
GameMap       → physical board legality
GameDeployment→ tentative participant plan
GameMaster    → orchestrates deployment rules + physical validation
```

Do not teach GameMap about UI cards or confirmation. Do not reimplement terrain physics inside GameDeployment.

**Unresolved/current-repo-dependent:** exact representation/API for side-specific deployment-zone ownership. Inspect repo; do not invent it.

---

## Initial Deployment Rules

### No RuntimeUnit before confirmation

During tentative Deployment:

```text
Runtime Units created = 0
```

Do not instantiate fake RuntimeUnits to reuse committed-board APIs.

### Tray contents

One card per top-level deployable Fleet Entry. Embarked Units remain aboard and do not get initial cards.

### Tentative placement

- MovementPivot follows snapped board tile;
- footprint rotates by facing;
- full footprint must be legal;
- placement remains movable until Confirm;
- placed card leaves tray;
- moving a placed Unit does not return its card;
- Reset clears tentative plan and restores availability.

### Selection/rotation

A tentative Unit can be selected, moved, and rotated. Rotate controls affect selected placement.

### Initial facing

Initial facing points toward the enemy side. Derive orientation from side/map layout rather than hardcoding globally if maps vary.

### Confirm

Confirm is final for that participant.

Require all top-level deployable entries to be validly placed.

Successful Confirm should:

1. validate full plan again;
2. create RuntimeUnits via runtime Unit state;
3. commit them to GameMap;
4. lock that participant.

Avoid partial commit.

When both confirm:

- Deployment ends;
- choose first player;
- set match started;
- enter Movement.

### Simultaneous secrecy

Opponent tentative Units are hidden during Deployment. AI receives the same allowed knowledge only.

---

## Deployment Visibility / Fog Presentation

During Deployment:

- own deployment zone fully revealed / absolute-vision-style presentation;
- rest of terrain remains visible but muted/greyed;
- do not cover most of setup map in heavy opaque fog;
- enemy tentative Units hidden.

Normal visibility begins when Movement phase 1 starts.

---

## Current GameDeployment Model

Current `GameDeployment` already references the same top-level Fleet Entry states owned by `GameUnits`.

A Placement already owns MovementPivotCoordinate, Facing, IsPlaced, HasFacing, FleetEntry, ID, and Unit.

A ParticipantPlan already supports lookup, selection, set placement, set facing, clear placement, reset, placed count, and all-placed check.

Do not duplicate this with a UI-owned second deployment model.

---

## Tentative Deployment Presentation

When drag interaction is implemented:

- dragging card selects Fleet Entry/Placement;
- create animated ghost through Unit presentation mapping;
- convert pointer to board tile outside authoritative API;
- snap MovementPivot to candidate tile;
- rotate footprint by candidate facing;
- ask authoritative validation;
- show legal/illegal footprint feedback;
- legal release submits tentative placement;
- illegal release rejects/cancels and card remains available.

The ghost is not a RuntimeUnit and does not create committed GameMap occupancy.

Tentative Unit visuals belong with map Unit presentation; footprint legality markers belong with Tile_Markers. Inspect current map roots rather than hardcoding transform paths.

---

## Three-Pass Gameplay UI Plan

### Pass 1 — HUD shell + gameplay camera/access
Effectively complete.

### Pass 2 — Unit visual presentation
Sufficiently complete for current Deployment work.

### Pass 3 — Deployment interaction
Current active pass:

- tray population;
- drag/drop;
- pivot snapping;
- footprint/facing validation;
- tentative placement;
- move/rotate;
- Reset;
- Confirm;
- RuntimeUnit creation/GameMap commit only on Confirm.

---

## Current Deployment UI Progress

Current implementation:

- Deployment HUD shell exists.
- horizontal Deployment tray works.
- runtime card generation works.
- one card per top-level Fleet Entry.
- generated cards show correct Unit preview/name/count.
- GameMaster exposes participant-scoped tentative-plan lookup plus authoritative
  validate/submit placement operations using Participant, FleetEntryID,
  MovementPivotCoordinate, and Facing.
- runtime footprint coordinates are built from FootprintSize, MovementPivot, and
  cardinal Facing using East-canonical authored geometry.
- GameMap can physically validate a Definition_Unit candidate without creating
  a fake RuntimeUnit or changing committed occupancy.
- same-participant tentative footprint overlap is rejected by GameMaster.
- cards can be dragged from the tray onto the map.
- drag creates an animated UnitView ghost under the runtime Map Units root.
- the ghost snaps its MovementPivot to the hovered tile and displays legal or
  illegal state through configurable opacity.
- legal release submits tentative placement, preserves the ghost, hides the
  placed card, and updates the placed count.
- illegal release discards the ghost and keeps the card available.
- initial facing is derived from the authored Deployment-zone centers.
- current test runtime still has zero RuntimeUnits and zero committed GameMap
  occupancy while awaiting Deployment confirmation.

Current limitation: the human UI currently presents Participant A. Selecting,
moving, and rotating placed tentative Units, Reset, Confirm, participant B/AI
control adapters, and footprint Tile_Marker visuals are not implemented yet.

Do not rebuild the tray from scratch.

---

## Current Active Milestone at Handoff

Project is in **Deployment Pass 3**.

Completed enough to build on:

- frontend Match handoff;
- runtime map instantiation;
- GameMaster initialization;
- GameMap runtime board;
- GameUnits participant rosters;
- GameDeployment tentative plans;
- gameplay HUD shell;
- gameplay camera;
- Unit presentation mapping;
- Rover/Gryphon animated Unit prefabs;
- Deployment card prefab;
- runtime Deployment tray population.

The tray successfully displays generated Fleet Entry cards at runtime.

Next intended sequence:

```text
1. Select and move already placed tentative Units.
2. Add rotate-left/right behavior for the selected tentative placement.
3. Add footprint Tile_Marker legality feedback using user-selected art.
4. Reset.
5. Confirm participant Deployment.
6. Create RuntimeUnits + commit to GameMap transactionally.
7. Add the participant B/AI control adapter through the same GameMaster API.
8. Complete both sides and transition to Movement.
```

Do not go back and redesign the working card/tray unless a real defect requires it.
