# Presentation and HUD

> Part of the Admirals agent docs. `AGENTS.md` (repo root) holds the rules that override anything here.
> Repository code/assets are the source of truth for exact current APIs and serialized fields; this doc records intent, ownership boundaries, and invariants.

---

## Presentation Is Separate from Gameplay Definitions

Settled mapping:

```text
Definition_Unit
        ↓
UnitPresentationCatalog
        ↓
UnitView prefab
```

`Definition_Unit` has no presentation fields.

It is acceptable to co-locate a Unit's prefab, Animator Controller, and animation clips beside that Unit's GameData folder for authoring convenience. Conceptual separation still holds.

---

## UnitView / Animator Architecture

### Unit-specific Animator Controllers

Different Units do not necessarily share the same animation capabilities. Do not force a universal Move/Attack/Dive/Submerge state schema across the whole roster.

The Animator Controller may encode Unit-specific presentation. `UnitView` stays thin/generic where possible.

`UnitView` may bind `Definition_UnitTag` assets to Animator bool parameters.
The tag is a typed asset reference, and the parameter is selected from the
actual Animator Controller's Bool parameters through the custom Inspector
drawer rather than authored as unconstrained text. Those bools must be driven
from participant snapshots after authoritative tag changes, never directly
from a HUD button.

### Facing parameter

Current directional idle convention:

```text
Facing float
0 = North
1 = East
2 = South
```

West reuses East with `SpriteRenderer.flipX = true`.

Gameplay footprint/pivot is not mirrored by visual flip.

### Idle Blend Tree

Idle direction uses a 1D Blend Tree rather than hard-coded Animator state paths or Any State directional transitions.

This avoids brittle strings and global transitions interrupting future Move/Attack states.

### Animation timing

Current idle sprite sheets use seven-frame loops at about 100 ms/frame:

```text
10 FPS
0.7 s per seven-frame cycle
```

Preserve artist-authored timing unless an asset says otherwise.

### Movement/turning

Committed Movement is played from the authoritative traveled route. Translation
steps ease between board positions while retaining the route's facing, including
sideways movement. Pivot-only turn steps switch directional idle facing over a
short eased pause; they do not rotate the sprite transform and require no
dedicated turn art.

Playback remains presentation-only. Authoritative Map/Unit state commits
immediately, and participant input is locked while the local route playback
queue is active.

---

## Current Presentation Progress

Current implementation:

- `UnitPresentationCatalog` exists.
- Rover destroyer and Gryphon fighter have presentation entries/prefabs.
- directional idle works.
- North/East/South clips exist.
- West uses East + flipX.
- presentation tested on runtime map.
- Deployment card drag instantiates the mapped animated UnitView as a
  non-authoritative tentative ghost under the runtime Map Units root.
- transient ghosts snap to the candidate MovementPivot tile; a legal drop
  preserves the view as the participant's tentative Unit presentation.
- configurable alpha-only colors distinguish legal, illegal, and placed ghost
  state without selecting final UI art or palette.
- sprite bleed beyond footprint is acceptable.
- centered-pivot Unit art is positioned at the rotated footprint's geometric
  center while gameplay remains anchored to MovementPivot coordinates.
- after Deployment, `ParticipantUnitPresenter` owns world Unit views and
  synchronizes them exclusively from the currently controlled participant's
  filtered snapshots;
- friendly views track committed pivot, facing, and runtime presentation tags;
- enemy views exist only while the enemy appears in `VisibleEnemies`, and are
  removed when current Absolute Vision no longer permits them;
- committed Movement uses stepwise route playback. Interrupted moves play only
  the authoritative traveled portion and stop at the final legal step;
- Movement playback is participant-filtered: the acting participant receives
  the traveled route, while opponents receive only contiguous route segments
  during which that Unit was in their Absolute Vision. Hidden coordinates are
  never included in opponent presentation output;
- Movement interaction uses a participant-bound `MovementMapInteraction`:
  selecting an owned Unit exposes its authored maneuver area on
  `Tile_Markers`, tentative position/facing changes remain presentation-only,
  and only Confirm submits the candidate through the shared authoritative
  Movement API. The selected-Unit HUD is hidden when no Unit is selected.
- The selected-Unit Movement bottom HUD uses four focused tabs instead of one
  compressed multi-column strip:
  - Status: Unit preview; Unit name with Unit-type badges above the segmented
    health area; and the smaller gray instance name above the active-effects
    section;
  - Systems: a horizontally scrolling tray of system cards;
  - Weapons: a horizontally scrolling tray of weapon cards with authored
    weapon-type badges and presentation-owned Domain badges;
  - Abilities: a horizontally scrolling tray whose Use buttons submit through
    the participant gameplay API and are enabled only for Movement-usable
    abilities.
  Unit/weapon type badges come from their typed classification definitions.
  Domain badge sprites remain presentation configuration on
  `MovementUnitDetailsUI`.

Do not reopen this pass unless an actual defect blocks Deployment.

---

## Gameplay HUD Design Rules

### Wireframes are design authority, not literal pixel specs

Wireframes define composition, information hierarchy, general placement, phase-specific structure, and visual intent.

They do **not** mandate literal pixel dimensions. Do not cram a 1080p runtime UI into a 720p wireframe's pixel budget just because the mockup was 1280×720.

Use current CanvasScaler/project settings and preserve proportions while allowing content to breathe.

### Persistent shell

Top/bottom HUD bars persist across normal gameplay phases, but internal content changes by phase.

Combat Resolution is a major exception and may use a substantially different/full-screen HUD.

### Deployment top bar

Includes player name, phase label, placed/total count, Reset, Confirm.

Do not add redundant instructional headings that undermine the clean wireframe.

### Rotate controls

Rotate controls belong lower-right in the map/play area, not inside the tray.

### Bottom tray

Only Deployment's bottom region is inherently a scrolling tray. Do not assume later phase bottom bars are ScrollRects.

---

## HUD Art Ownership

The user owns/chooses UI art.

Do not assign sprites because filenames look plausible, invent palettes, or substitute generic art.

Agent responsibility: hierarchy, layout, components, behavior, wiring.

User chooses actual visual assets unless explicitly delegated.

---

## UI Layout Philosophy

Prefer content-driven Unity UI using Layout Groups, Content Size Fitter, Layout Element where semantically useful, and anchors/pivots for structural placement.

Avoid arbitrary child positioning inside layout-driven cards, unnecessary spacer objects, fixed widths that cannot accommodate real names, or layout scripts for problems built-in Unity UI already solves.

---

## Deployment Unit Card

Settled card concept:

```text
HUD_Deployment_UnitCard
├─ PreviewArea
│  └─ UnitPreview
└─ Footer
   ├─ Text_Name
   └─ Text_Count
```

Requirements:

- root uses layout-driven sizing;
- works for aircraft, ships, long carrier-like sprites;
- actual names such as `"Rover" Class Destroyer` fit;
- count is aligned at right;
- Unit name can consume flexible remaining width;
- preview preserves aspect;
- card may grow instead of crushing content.

Do not add a custom layout script merely to solve card sizing unless built-in layout is demonstrably insufficient.

Current prefab path:

```text
Assets/Prefabs/HUD/Deployment/HUD_Deployment_UnitCard.prefab
```

---

## Deployment Tray / ScrollRect

Settled hierarchy:

```text
DeploymentTray
└─ Viewport
   └─ Content
      ├─ Card
      ├─ Card
      └─ ...
```

Important wiring:

- `ScrollRect.Content` → actual `Viewport/Content` RectTransform.
- `ScrollRect.Viewport` → Viewport.
- `DeploymentTrayUI.Content` → actual Content, not DeploymentTray root.
- Cards instantiate under Content.
- Horizontal scrolling only.
- Content uses horizontal layout + content sizing.

A previous `Image + Mask` Viewport setup caused generated cards to disappear. Those components were removed in the working setup. Do not casually reintroduce a default visible Image solely to mask the viewport.

---

## HUD Prefab Organization

Keep prefab folders shallow:

```text
Assets/Prefabs/HUD/
├─ HUD_Game.prefab
└─ Deployment/
   ├─ HUD_Deployment_Top.prefab
   ├─ HUD_Deployment_Overlay.prefab
   ├─ HUD_Deployment_Bottom.prefab
   └─ HUD_Deployment_UnitCard.prefab
```

Do not create one-item `Top/Bottom/Overlay` subfolders.

Pass & Play owns the authored
`Assets/Prefabs/HUD/PassAndPlay/HUD_PassAndPlay_Handoff.prefab`. It is
instantiated above `Canvas_Game` only when the finalized Match contains two
local participants. Its opaque privacy gate blocks input and conceals the old
participant's filtered view until the receiving player continues. Singleplayer
does not instantiate it.

Deployment, Movement, and Pre-Battle own independent Top, Overlay, and Bottom
phase prefabs. Phase controllers switch those roots; they do not repurpose
another phase's prefab by changing its labels at runtime. Shared presentation
components may still render the same participant-filtered data into multiple
phase prefabs.

Search follows the same rule and owns independent Top, Overlay, and Bottom
prefabs under `Assets/Prefabs/HUD/Search`. Its bottom cards remain
content-driven and horizontally scrollable; Search strength labels use an
authored prefab. Phase-specific HUD structure must not be synthesized as
inaccessible runtime-only UI.

Battle likewise owns independent Top, Overlay, and Bottom prefabs under
`Assets/Prefabs/HUD/Battle`, plus `HUD_Battle_Engagement.prefab` for the
full-screen engagement. Runtime code may populate authored card/line templates,
but must not synthesize the phase screen hierarchy. Battle's overworld bottom
bar reuses the selected-Unit presenter in Battle mode, showing Status and
Fire Control only. The engagement action and resolution regions are vertically
scrollable and use content-driven layout.

End owns independent Top and Bottom prefabs under
`Assets/Prefabs/HUD/End`. It intentionally has no play-area control overlay:
the map remains selectable for Unit inspection, while the bottom Unit details
present Status, Systems, Weapons, and Abilities. End abilities are submitted
through the same participant-bound input API. When GameMaster concludes the
Match, the existing End top bar presents the bound participant's Victory,
Defeat, or Draw result plus final score and completed turn, and disables the
End command instead of constructing a separate runtime-only result hierarchy.

Outside Deployment, the shared bottom-bar shell is hidden whenever no owned
Unit is selected. Selecting an owned Unit reveals the phase's own Bottom root;
deselecting it hides the complete shell again.

Absolute-visible enemy Units retain their normal Unit sprite and receive the
authored animated red indicator across every occupied footprint tile. These
indicators are participant-filtered and rendered on a dedicated presentation
Tilemap at sorting order 45: above Fog/Terrain, below Unit sprites (50), and
below tactical Tile Markers/VFX (60/70). Contacts do not receive this marker,
because they are not absolute-visible Units.

---

## Current Game Scene/HUD Shape

Conceptual scene:

```text
Game
├─ Main Camera
├─ GameMaster
├─ Canvas_Game
│  └─ HUD
│     ├─ TopBar
│     ├─ PlayAreaUI
│     │  └─ DeploymentRotateControls
│     └─ BottomBar
│        └─ DeploymentTray
└─ EventSystem
```

Runtime map is instantiated separately by GameMaster.

Do not author a permanent map into the Game scene.

---

## Gameplay Camera / HUD Interaction

Intended interaction model:

- gameplay camera renders map behind HUD;
- HUD elements block pointer input where appropriate;
- map remains interactable in actual play area;
- `PlayAreaUI` must not become one giant raycast blocker;
- camera access/clamping respects usable gameplay area so edge tiles remain reachable.

Established camera behavior:

- orthographic;
- WASD pan;
- smooth wheel zoom;
- pan speed scales with zoom;
- min/max ortho size;
- map bounds + padding;
- starts centered;
- may later focus local deployment zone;
- no middle-mouse drag requirement.

At every allowed zoom level, edge tiles must be reachable/clickable. Avoid clamp logic that locks an oversized view to exact center and makes edges inaccessible.

Inspect current repo before changing camera rect/viewport architecture because this area evolved during HUD work.

---

## Raycast/Input Safety

HUD elements intended to intercept clicks may stay raycast targets.

Pure structural play-area objects should not block map input because they happen to have an Image.

Deliberately decide `Raycast Target` on UI Images. Never make the whole map unclickable with a full-screen invisible UI Image.

---

## Deployment Card Runtime Binding

Runtime binding supplies Unit preview, Unit name, and count.

One top-level Fleet Entry = one card, so current count is generally `x1`.

Do not create RuntimeUnits to populate the tray.

Presentation lookup uses `UnitPresentationCatalog`, not fields on `Definition_Unit`.

---

## Map Visual Layers

Established visual layer concept:

```text
Grid
├─ Terrain_Art
├─ Below_Units
├─ Units
├─ Tile_Markers
├─ Fog
└─ VFX
```

Preserve separation by purpose.

Fog presentation is participant-bound and dynamic. The existing `Fog` Tilemap
uses the fifteen authored quadrant-mask Fog tiles to rebuild boundaries whenever
the permitted participant snapshot changes. `Obscured` does not require a
separate authored tile: the existing `Terrain_Art` cell color is multiplied by
a configurable presentation tint. The tint remains beneath both Obscured and
Fog tiles so transparent Fog edges never reveal full-bright terrain; only
Absolute tiles restore the original terrain color.

The `Units` visual root uses a `SortingGroup` above participant Fog so permitted
Unit presentation remains readable without exposing hidden authoritative Units.
Generated maps use the complete order stack: Terrain 0, Below Units 10, Fog 40,
Units 50, Tile Markers 60, and VFX 70.

---

## UI Naming

Prefer semantic names such as `TopBar`, `PlayAreaUI`, `BottomBar`, `DeploymentTray`, `Viewport`, `Content`, `PreviewArea`, `UnitPreview`, `Footer`, `Text_Name`, `Text_Count`, `DeploymentRotateControls`.

Do not leave generic `Panel1`, `New Text`, etc. in final authored hierarchy.

---

## Visual References (wireframes / concept art)

Design images live in `Docs/Design/` (outside `Assets/`, so Unity ignores them; track PNGs with Git LFS). **Describe each image in text here** — do not rely on the agent seeing the picture. Wireframes define composition and hierarchy, not literal pixel sizes (see *Wireframes are design authority*).

_No references added yet. Template:_

```markdown
### <Screen name> — `Docs/Design/<file>.png`
Layout: …  Phase(s): …  Key elements: …  Notes/intent: …
```
