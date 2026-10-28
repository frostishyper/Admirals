# General Architecture & Implementation Philosophy

This section defines the default engineering philosophy for Admirals. It is intentionally broader than any one gameplay subsystem.

The project strongly prefers **consolidated, cohesive architecture without becoming monolithic**.

The goal is not "the fewest files possible," and it is not "one class per responsibility" taken to an extreme. The goal is to keep related behavior close enough that the project remains understandable, editable, and resistant to cascading changes while still separating responsibilities that are genuinely distinct.

---

## 1. Consolidated, Not Monolithic

Default toward keeping strongly related behavior together.

A class or file may be substantial when the behavior belongs to one coherent owner.

Do **not** split code merely because:

- a file is getting long;
- a method could technically live somewhere else;
- a concept can be wrapped in another class;
- a helper would make the call site look shorter;
- "single responsibility" can be interpreted narrowly enough to justify another file;
- a design pattern permits another layer of delegation.

A larger cohesive class is usually preferable to a graph of tiny objects whose only purpose is forwarding calls to one another.

At the same time, consolidation must not become a god object.

A substantial class should still have:

- a clear ownership boundary;
- internally separated concerns;
- readable private methods;
- a limited public surface;
- stable inputs/outputs;
- no unrelated presentation/gameplay/data responsibilities mixed together.

The intended shape is:

```text
substantial owner
├─ cohesive internal behavior
├─ a few meaningful subordinate systems when warranted
└─ minimal unnecessary indirection
```

not:

```text
manager
├─ manager
│  └─ helper
│     └─ service
│        └─ wrapper
│           └─ one method
```

---

## 2. Split by Substantial Responsibility, Not by Line Count

Create a new script/class when the separation has a real architectural benefit.

Good reasons to split include:

### Independent authoritative ownership

The new concern owns meaningful state or rules that should not belong to its parent.

Examples already established in Admirals:

```text
GameMaster
├─ GameMap
├─ GameUnits
└─ GameDeployment
```

These are worthwhile subordinates because each owns a substantial and distinct form of gameplay truth.

`GameMap` owns physical board truth.

`GameUnits` owns runtime Unit state.

`GameDeployment` owns tentative Deployment state.

These are not helpers extracted merely to shorten `GameMaster`.

### Important semantic type safety

Separate types are warranted when allowing two concepts to be accidentally interchangeable would be dangerous or confusing.

Examples include distinctions such as:

```text
Definition_UnitType
Definition_WeaponType
Definition_SystemType
Definition_Tier
Definition_Manufacturer
```

The distinction has gameplay meaning, so type separation is useful.

Prefer real C# type safety when semantic distinction matters rather than relying only on:

- Inspector labels;
- comments;
- string conventions;
- filtered dropdowns.

### Independent reuse

A concept that is genuinely reusable in many unrelated consumers may deserve an independent definition.

Examples:

```text
Definition_Pattern
Definition_Status
GameplayAction
GameplayModifier
D20Roll
```

These exist because several systems need the same underlying concept.

### Independent lifecycle

A Unity component may warrant its own script when it genuinely has its own lifecycle, runtime state, or scene/prefab responsibility.

Do not create a MonoBehaviour simply because some logic happens to interact with Unity.

### Distinct architectural boundary

Presentation versus gameplay is substantial enough to justify separation.

For example:

```text
Definition_Unit
        ↓
UnitPresentationCatalog
        ↓
UnitView
```

This separation prevents authored gameplay definitions from becoming coupled to sprites, Animators, prefabs, and other presentation concerns.

### Meaningful integration boundary

Human/AI input, authoritative gameplay resolution, physical map state, and presentation are distinct because they serve different consumers and trust boundaries.

Those boundaries are worth maintaining even if merging them would temporarily reduce file count.

---

## 3. Reasons That Usually Do NOT Justify Another Script

Do not create a new file merely for:

- one private calculation;
- one property wrapper;
- one Inspector field group;
- one enum that naturally belongs with related shared types;
- one data record that only exists inside a single owner;
- one method that only its parent ever calls;
- one tiny "manager" for a single action;
- one layout calculation that Unity's built-in layout system already handles;
- one wrapper around an existing API;
- one object whose only purpose is passing data from A to B unchanged.

Before introducing another class, ask:

> Does this new type own something substantial, enforce meaningful type safety, define a true architectural boundary, or provide independent reuse?

If the answer is no, prefer keeping the behavior inside the existing coherent owner.

---

## 4. Prefer Embedded Serializable Data Where Independent Identity Is Unnecessary

The project deliberately avoids ScriptableObject proliferation.

Use a ScriptableObject when the concept needs things such as:

- independent authored identity;
- reuse across many unrelated assets;
- direct asset references;
- independent selection in the editor;
- independent lifecycle/versioning as authored content.

Use embedded `[Serializable]` data when the concept only makes sense as part of one owning definition.

The desired authored-data depth is relatively shallow.

Conceptually:

```text
Unit
├─ Weapon
├─ System
├─ Ability
└─ other substantial reusable definitions
```

Do not turn every nested outcome, capability, setting, condition, or field into a separate ScriptableObject merely because Unity permits it.

The authoring workflow should not become a maze of hundreds of tiny assets.

---

## 5. Favor Composition Without Asset or Class Proliferation

Reusable behavior should be composed from shared systems where practical.

Examples already established:

```text
GameplayAction
GameplayModifier
Definition_Status
Definition_Pattern
D20Roll
System capabilities
Attack properties
```

A unique mechanic may have substantial custom code.

That is acceptable.

But it should still reuse existing common infrastructure where appropriate instead of creating parallel implementations of:

- damage;
- healing;
- statuses;
- modifiers;
- tags;
- D20 handling;
- pattern geometry;
- targeting;
- replenishment.

The philosophy is:

> Build common primitives once. Let unique systems compose them. Add unique code when the mechanic is actually unique.

Do not force every future mechanic into a universal abstraction if doing so makes the abstraction more complicated than the mechanic.

---

## 6. Updateability and Change Locality Are Major Design Goals

Architecture should make common future changes local.

Changing one gameplay concept should not require touching six unrelated scripts unless the behavior genuinely crosses six ownership boundaries.

Prefer designs where:

```text
one conceptual change
        ↓
one primary owner changes
        ↓
dependent consumers continue using a stable API
```

Avoid designs where:

```text
one conceptual change
        ↓
wrapper changes
        ↓
manager changes
        ↓
adapter changes
        ↓
consumer changes
        ↓
serialized prefab changes
        ↓
several unrelated systems break
```

A consolidated class can still be highly maintainable if:

- its internal concerns are clear;
- private implementation can change freely;
- consumers depend on stable public operations;
- state ownership remains obvious;
- shared logic is centralized instead of copied.

The project values **change locality** more than theoretical maximum delegation.

---

## 7. Stable Boundaries, Flexible Internals

Public and serialized surfaces should be more stable than private implementation.

When updating an existing system:

- preserve established public APIs when practical;
- preserve serialized field names unless migration is necessary;
- preserve enum numeric values where serialized assets may depend on them;
- preserve asset types and prefab component contracts;
- preserve unrelated behavior.

Private internals may be reorganized when it materially improves the requested feature, but do not make callers pay for internal cleanup without a reason.

A good implementation allows the owner to evolve while its consumers remain mostly unaffected.

---

## 8. Avoid Cascading Refactors

Do not turn a focused feature request into a repository-wide architecture migration.

If implementing feature X exposes an unrelated imperfection Y:

- leave Y alone unless it blocks X;
- note it if important;
- do not silently refactor it.

Changes should be targeted.

Avoid:

- speculative renaming;
- "while we're here" cleanup;
- folder reshuffling;
- generalized frameworks for hypothetical future features;
- rewriting working systems to match a preferred textbook pattern.

This project has repeatedly favored **working, coherent, extensible architecture** over architecture optimized for hypothetical future purity.

---

## 9. GameMaster Should Have Substantial Subordinates, Not Tiny Mechanisms

`GameMaster` is expected to be important.

It should not contain every implementation detail itself, but it also should not become an empty coordinator that delegates every three lines to another manager.

A subordinate under GameMaster should normally represent something substantial, such as:

```text
GameMap
GameUnits
GameDeployment
```

Possible future subordinates should meet a similar standard.

Do not create classes such as:

```text
DeploymentSelectionManager
DeploymentFacingManager
DeploymentPlacementManager
DeploymentValidationManager
DeploymentConfirmationManager
```

unless those concerns independently become substantial enough to justify such boundaries.

Prefer:

```text
GameDeployment
├─ placement state
├─ selection state
├─ tentative facing
├─ reset behavior
└─ plan-level state
```

with GameMaster orchestrating authoritative cross-system validation.

---

## 10. Ownership Should Be Obvious

Every important state should have one obvious owner.

Examples:

```text
physical Unit position        → GameMap
runtime Unit mutable state    → GameUnits / RuntimeUnit
tentative initial placement   → GameDeployment
match orchestration           → GameMaster
authored Unit data            → Definition_Unit
Unit presentation             → UnitPresentationCatalog / UnitView
HUD layout/presentation       → HUD presentation layer
```

Do not create shadow copies of authoritative state because a consumer finds them convenient.

A UI may cache references or derived display information, but should not become a second gameplay model.

An AI may read filtered gameplay output, but should not maintain a parallel authoritative simulation merely to interact with the game.

---

## 11. One Concept = One Name

Naming stability is part of architecture.

Do not introduce synonyms for established project concepts.

Examples:

```text
OriginPoint
MovementPivot
RuntimeUnit
Definition_Unit
GameMap
MapAuthoring
GameDeployment
Participant
FleetEntry
```

These names carry specific meanings.

If two things have different names, assume the distinction may be intentional until the repository/docs prove otherwise.

If two things are truly the same concept, avoid giving them multiple names across layers.

This reduces both human confusion and agent hallucination.

---

## 12. Type Safety Should Be Used Where It Prevents Real Mistakes

The project is not opposed to extra types.

It is opposed to extra types with no substantial payoff.

Create distinct types when mixing them would cause real semantic errors.

Examples include:

- WeaponType versus UnitType;
- Definition versus Runtime state;
- authored map versus runtime map;
- tentative Deployment versus committed board state;
- participant-visible state versus authoritative hidden state.

Do not replace useful type distinctions with:

- strings;
- generic object references;
- giant untyped dictionaries;
- Inspector-only restrictions.

Conversely, do not invent a new type for every primitive value if normal strongly typed fields already express the concept adequately.

---

## 13. Prefer Semantic Cohesion Over Pattern Purity

Design patterns are tools, not goals.

Do not introduce:

- Strategy;
- Command;
- Service Locator;
- Repository;
- Mediator;
- Factory;
- Adapter;
- Event Bus;

merely because the pattern could theoretically apply.

Use a pattern when it solves a concrete Admirals problem better than the simpler alternative.

For example, the shared human/AI gameplay-input boundary exists because there are genuinely two different control sources that must converge on one authoritative API.

That is a real architectural boundary.

By contrast, wrapping one GameDeployment method in three layers of commands/adapters without a concrete need would only add indirection.

---

## 14. Minimize Public Surface Area

Expose what consumers genuinely need.

Keep implementation details private/internal when possible.

A smaller stable API:

- reduces coupling;
- makes refactoring safer;
- prevents consumers from bypassing authority;
- makes AI/HUD integration easier to document;
- reduces accidental misuse.

Do not expose internal collections or mutable objects purely because another system might someday want them.

Where participant knowledge matters, expose filtered/query-oriented data rather than the whole authoritative object graph.

---

## 15. Data Should Flow Through Clear Boundaries

Prefer direct, explicit data flow.

For example:

```text
Human pointer
    ↓
UI converts to board coordinate
    ↓
plain gameplay input
    ↓
GameMaster validation
    ↓
GameDeployment / GameMap / GameUnits
```

Avoid:

```text
UI GameObject
    ↓
global singleton
    ↓
scene lookup
    ↓
hidden mutable state
    ↓
another MonoBehaviour polling it
```

Do not make consumers scrape presentation state to recover gameplay information already available as data.

---

## 16. Avoid "Voodoo" Editor Workflows

Authoring should remain understandable from the Inspector/project structure.

Avoid designs that require:

- obscure drag/drop order;
- manually synchronized duplicate references;
- hidden scene searches;
- magic hierarchy names;
- manually copied IDs between unrelated objects;
- implicit setup that only one developer remembers.

When serialized references are required, make them semantically clear.

When a reference can be resolved from an authoritative existing relationship without ambiguity, avoid making the user maintain a duplicate manual link.

---

## 17. Folder Structure Should Reflect Concepts, Not Every Type

Keep folder depth shallow.

Prefer:

```text
GameMaster/
HUD/
Map/
Units/
Systems/
Weapons/
Definitions/
Classifications/
```

over:

```text
Systems/
└─ Search/
   └─ Capabilities/
      └─ SearchCapability/
         └─ Runtime/
            └─ ...
```

Do not create a folder just because one file exists for a concept.

Create deeper folders when there is enough real content to make the grouping useful.

The same principle applies to prefabs and GameData.

---

## 18. Prefixes Are Useful When They Improve Searchability

The project uses semantic naming prefixes such as:

```text
Definition_Unit
Definition_Weapon
Definition_System
Definition_Status
Definition_Pattern
```

A good prefix can provide organization without forcing deeper folders.

Prefer naming that makes related things easy to find over folder proliferation whose only purpose is hiding file count.

---

## 19. Preserve Separation of Data, Runtime State, and Presentation

These three layers should not collapse into each other.

### Authored data

Defines what something is.

Examples:

```text
Definition_Unit
Definition_Weapon
Definition_System
Definition_Status
```

### Runtime state

Defines what is currently happening to a specific match instance.

Examples:

```text
RuntimeUnit
GameUnits
GameMap occupancy
GameDeployment placement
```

### Presentation

Defines how permitted information appears.

Examples:

```text
UnitView
Animator
HUD
UnitPresentationCatalog
VFX
```

Do not put runtime mutable values into definitions.

Do not put presentation assets into gameplay definitions.

Do not infer authoritative state from presentation.

---

## 20. Prefer Built-In Unity Systems Before Custom Infrastructure

When Unity already cleanly solves the problem, use it.

Examples:

- Layout Groups for UI layout;
- Content Size Fitter for content-driven dimensions;
- Layout Element for real layout constraints;
- ScrollRect for scrolling;
- Animator / Blend Trees for animation-state behavior;
- SpriteRenderer flip for West-facing presentation reuse.

Do not write a custom MonoBehaviour for a layout problem merely because scripting it feels more controllable.

Custom code is appropriate when built-in behavior cannot express the required game behavior cleanly.

---

## 21. UI Should Be Structurally Dynamic, Not Arbitrarily Fixed

Avoid excessive magic dimensions and manually positioned children where content varies.

Names, Unit previews, and other variable content should be allowed to determine layout within reasonable constraints.

Use:

- minimum sizes;
- preferred sizes;
- flexible sizes;
- layout groups;
- content fitters;
- anchor presets.

Avoid fixing every element to dimensions that only work for the current example content.

The Deployment card work established this clearly:

- real Unit names must fit;
- long Unit sprites may require wider cards;
- count should stay aligned correctly;
- card layout should remain driven by content.

Do not solve dynamic content by manually tuning every prefab instance.

---

## 22. Wireframes Define Intent, Not Literal Resolution

Wireframes are authoritative for:

- composition;
- hierarchy;
- placement;
- information priority;
- overall clean visual design.

They are not authoritative for exact runtime pixel measurements.

Do not turn a 1280×720 design mockup into a hard constraint that causes cramped UI on a 1920×1080 display.

Preserve the design while allowing layout to scale naturally.

---

## 23. The User Owns Art Decisions Unless Explicitly Delegated

Do not infer final art choices from filenames.

Do not assign UI sprites, palettes, badges, or decorative assets because they "look appropriate."

The agent should normally own:

- hierarchy;
- components;
- data binding;
- interaction;
- layout logic;
- gameplay behavior.

The user/art team owns visual asset selection unless explicitly delegated.

---

## 24. Validation Belongs With the Owner

Validation should live near the data/rules it validates.

Examples:

- authored definition validity → owning definition;
- physical board legality → GameMap;
- tentative Deployment plan state → GameDeployment;
- cross-system gameplay legality → GameMaster orchestration.

Avoid validation classes whose only purpose is calling methods back on the actual owners.

Nested `[Serializable]` classes should generally be validated by their owning Unity object rather than each trying to behave like an independent Unity lifecycle object.

---

## 25. Shared Logic Should Be Centralized, Not Copied

When the same rule appears in multiple consumers, centralize it at the correct owner.

Examples:

- D20 outcome resolution;
- status application rules;
- modifier semantics;
- pattern geometry;
- board occupancy;
- Deployment legality;
- participant knowledge filtering.

Do not let HUD and AI each implement their own interpretation of legality.

Do not let multiple gameplay systems each create their own version of damage/status handling.

One rule should have one authoritative implementation whenever practical.

---

## 26. Do Not Generalize Before the Commonality Is Real

Do not build an abstraction for hypothetical future use before at least one substantial shared need exists.

Bad progression:

```text
one feature
→ imagine six future features
→ build universal framework
→ current feature becomes harder
```

Preferred progression:

```text
implement coherent current feature
→ identify real repeated behavior
→ extract shared primitive
→ keep consumers simple
```

The project has enough genuine cross-cutting systems that abstraction is useful, but abstraction should be earned by actual reuse.

---

## 27. Avoid Premature Future-Proofing That Makes the Present Brittle

Future extensibility matters, but present implementation must remain usable.

Do not create complicated generalized systems purely because:

- more Units will eventually exist;
- more phases will eventually exist;
- more animations will eventually exist;
- more AI behaviors will eventually exist.

Instead, preserve the architectural boundary that allows future expansion.

Example:

The animation solution should allow Unit-specific Animator Controllers without forcing every future possible animation state into `UnitView` today.

That is flexible architecture without premature implementation.

---

## 28. Prefer Capability Growth Over Class Explosion

When a substantial authored object supports several optional related behaviors, adding embedded capability data may be cleaner than creating an inheritance tree or a new ScriptableObject class per combination.

`Definition_System` is an example of this philosophy.

A System can expose meaningful capabilities such as:

```text
Search
FireControl
Passive
Defense
UnitDeployment
MineDeployment
```

without requiring every combination to become a separate subclass hierarchy.

Do not automatically solve variation with inheritance.

Use composition when the data naturally supports it.

---

## 29. Preserve Semantic Distinction Even Inside Consolidated Code

Consolidation does not mean flattening every concept together.

A file may contain several closely related types while still preserving their semantic distinction.

Examples:

- related shared enums in `GameplayTypes`;
- nested serializable outcome/capability records near their owning definition;
- participant-plan and placement data inside Deployment.

This gives the project fewer files without losing conceptual clarity.

---

## 30. Subordinate Files Should Earn Their Existence

A new subordinate script should answer at least one of these strongly:

- Does it own substantial independent state?
- Does it define a real trust/authority boundary?
- Does it need independent reuse?
- Does it enforce important semantic type safety?
- Does it have an independent Unity lifecycle?
- Does it isolate a large concern that would otherwise make the parent genuinely monolithic?
- Does it materially improve change locality?

If not, keep the concern inside its current owner.

---

## 31. Consolidation Should Improve Updatability

A consolidated system should not become a tangled block.

Within a larger owner:

- group related state;
- keep private methods focused;
- use meaningful sections;
- centralize repeated internal logic;
- avoid cross-calling unrelated internal areas unnecessarily;
- keep public entry points small.

The desired result is:

> fewer files, fewer external dependencies, fewer places to edit, but still clear internal structure.

---

## 32. Avoid Fragile Chains of Delegation

Every additional delegation layer is another contract that may break.

Prefer:

```text
consumer
→ authoritative owner
```

or:

```text
consumer
→ GameMaster
→ substantial subordinate
```

over:

```text
consumer
→ adapter
→ facade
→ manager
→ service
→ helper
→ state object
```

unless each layer has a concrete independent reason to exist.

The architecture should be easy to trace in both directions:

- "Who owns this state?"
- "Who validates this action?"
- "Who is allowed to mutate this?"
- "Where does this data originate?"

The answer should normally be obvious within one or two jumps.

---

## 33. Do Not Create Duplicate Sources of Truth

If the project already has authoritative state, reuse it.

Examples established during Deployment:

- `GameDeployment.Placement` already stores tentative placement state;
- UI cards should reference that state rather than invent a second placement model;
- RuntimeUnit should not duplicate position owned by GameMap;
- presentation should not store gameplay-facing truth merely because it needs to draw it.

Duplicated truth inevitably causes synchronization bugs.

Prefer derivation/reference over copying authoritative state.

---

## 34. Favor Explicit Relationships Over Scene Searches

Avoid runtime logic that relies on:

```csharp
Find(...)
FindObjectOfType(...)
transform.Find("Some/Fragile/Path")
```

when an existing owner/reference can provide the relationship directly.

Hierarchy paths are presentation details and are fragile architecture contracts.

Serialized references are acceptable where the relationship is genuinely authored.

Programmatic relationships should derive from actual domain ownership where practical.

---

## 35. Avoid Temporary Hacks Becoming Architecture

Deadline pressure does not justify knowingly embedding a bad ownership model into the core.

Temporary presentation shortcuts are acceptable when isolated and clearly temporary.

Temporary authoritative-state shortcuts are much more dangerous.

In particular, avoid:

- fake RuntimeUnits merely to preview Deployment;
- UI-owned gameplay legality;
- AI-specific rules paths;
- presentation transforms being treated as Unit positions;
- hardcoded map dimensions;
- hidden scene searches as permanent dependency injection.

If a shortcut crosses an authority boundary, reconsider it.

---

## 36. Prefer Transactional Authoritative Operations

When an operation changes several authoritative systems, validate enough up front to avoid partial state.

Deployment Confirm is the key example:

```text
validate entire plan
        ↓
prepare runtime state
        ↓
commit full valid result
```

Avoid:

```text
create Unit 1
create Unit 2
fail Unit 3
leave half-committed fleet
```

The more systems an operation touches, the more important this becomes.

---

## 37. Keep Consumer-Specific Convenience Out of Core State

Do not distort authoritative data solely because one consumer finds another shape easier.

Examples:

- HUD wants display strings → derive/present them in HUD;
- AI wants structured state → expose structured participant-filtered data;
- Animator wants float facing → presentation adapter converts gameplay facing;
- drag input wants screen coordinates → human adapter converts to board coordinates.

Core state should remain expressed in gameplay concepts.

---

## 38. Prefer Domain Values Over Presentation Values

At gameplay boundaries, prefer things such as:

```text
FleetEntryID
Vector2Int tile coordinate
UnitFacing
GameplayPhase
RuntimeUnit
Definition_Weapon
```

over:

```text
screen coordinates
world-space mouse position
GameObject
RectTransform
sprite name
button reference
```

Presentation values should be translated before entering authoritative gameplay.

---

## 39. Avoid Inheritance Unless the "Is-A" Relationship Is Strong

The project generally benefits more from composition and embedded capability data than from deep inheritance trees.

Do not create class hierarchies merely because several definitions share some fields.

Use inheritance when substitutability is real and useful.

Otherwise prefer:

- shared reusable data;
- capabilities;
- composition;
- small semantic types.

This keeps future authored combinations flexible.

---

## 40. Make Invalid States Hard to Represent Where Practical

Type safety, owner-controlled mutation, and validation should work together.

Prefer:

- private setters;
- internal mutation methods;
- typed references;
- explicit enum states;
- validation before authoritative mutation.

Avoid making every field public merely for convenience.

Consumers should not be able to casually bypass the owner.

---

## 41. Editor Convenience Must Not Weaken Runtime Architecture

Custom selectors, Inspector tooling, and editor helpers are encouraged when they improve authoring.

But editor conveniences should sit on top of the runtime data model.

Do not redesign runtime types merely to make one Inspector dropdown easier.

Likewise, a filtered ScriptableObject selector may improve authoring without replacing actual compatibility validation.

---

## 42. Authoring Workflow Is Part of Maintainability

The project contains many Units, weapons, systems, patterns, outcomes, statuses, etc.

A technically clean runtime model that is miserable to author is still a bad fit.

When designing authored data, consider:

- how many assets must be created;
- how many repeated fields must be wired;
- whether asset pickers become enormous;
- whether related data is easy to inspect together;
- whether invalid combinations can be caught early;
- whether the content hierarchy remains understandable.

Prefer fewer meaningful assets with cohesive embedded data over excessive micro-assets.

---

## 43. Preserve Existing Working Workflows

Once a workflow has proven functional, do not casually replace it during an unrelated task.

Examples:

- working Deployment card/tray;
- current Unit presentation mapping;
- existing Match handoff;
- current map instantiation;
- current GameMaster initialization order.

Improve a working workflow only when:

- it blocks the next required feature;
- it violates a settled architectural boundary;
- it contains a demonstrated defect.

Do not repeatedly redesign completed passes.

---

## 44. Architecture Should Be Explainable in Plain Language

A good subsystem should be describable simply.

Examples:

```text
GameMap owns where Units physically are.

GameUnits owns what each runtime Unit currently is.

GameDeployment owns where Fleet Entries are tentatively placed before confirmation.

GameMaster validates and coordinates gameplay actions.

HUD and AI both talk to GameMaster through gameplay data.
```

If explaining ownership requires several paragraphs of exceptions, the implementation may be too fragmented or responsibilities may be leaking.

---

## 45. Prefer Directness Over Ceremony

A direct method call between two properly related owners is acceptable.

Not every interaction needs:

- an interface;
- an event;
- a command object;
- a dependency-injection container;
- a mediator;
- an observer layer.

Introduce ceremony only when it solves a concrete coupling, reuse, testing, or authority problem.

The project prefers architecture that can be followed by reading the relevant few files.

---

## 46. Events Are for Real Decoupling, Not Avoiding Method Calls

Use events when multiple independent observers legitimately need notification without the source owning them.

Do not use events merely to avoid calling a known authoritative dependency.

Gameplay authority should remain traceable.

An event should not obscure who actually owns the state change.

---

## 47. Interfaces Must Represent Real Substitutability

Do not create an interface because a class has public methods.

Use interfaces when multiple implementations genuinely need to satisfy the same contract.

The human/AI distinction does not automatically require every gameplay subsystem to become interface-driven.

The shared gameplay input contract is the important abstraction; implementation details can remain concrete until real substitution exists.

---

## 48. Avoid Generic Frameworks for Project-Specific Semantics

Do not erase meaningful Admirals concepts behind overly generic names such as:

```text
Entity
Thing
DataObject
GenericManager
ActionHandler
SystemManager
ValueProvider
```

when the domain already has a precise concept such as:

```text
RuntimeUnit
FleetEntry
GameDeployment
GameMap
GameplayAction
Definition_System
```

Domain-specific names make ownership and behavior easier to reason about.

---

## 49. Use Shared Files for Closely Related Lightweight Types

The project does not require every enum, nested payload, or tiny semantic record to have its own `.cs` file.

Closely related lightweight types may live together when that improves discoverability.

`GameplayTypes` is an example of deliberate consolidation.

Split them when:

- file responsibility becomes unclear;
- independent reuse becomes substantial;
- type-specific behavior becomes large;
- semantic separation becomes important enough to improve navigation.

---

## 50. Do Not Confuse Encapsulation with File Count

Encapsulation comes from:

- controlled mutation;
- limited public APIs;
- clear ownership;
- semantic types;
- dependency direction.

It does not require every private concern to exist in a separate source file.

A well-encapsulated substantial class may be preferable to six tiny public classes with mutual dependencies.

---

## 51. Practical Decision Test Before Adding Architecture

Before adding a new class, ScriptableObject, manager, wrapper, interface, or folder, answer:

1. **What exact state or behavior does it own?**
2. **Why should the existing owner not own that?**
3. **Who consumes it independently?**
4. **Does the split improve type safety, reuse, lifecycle, authority, or change locality?**
5. **Will ordinary future changes now require touching more files?**
6. **Is the new abstraction solving a current problem or an imagined future one?**
7. **Can an embedded/private implementation solve the same problem more simply?**

If the split does not produce a strong answer, do not add it.

---

## 52. Practical Decision Test Before Consolidating

Before merging concerns together, answer:

1. Do they share the same owner?
2. Do they change for the same reasons?
3. Do they share the same lifecycle?
4. Would consumers ever need one independently of the other?
5. Would merging weaken type safety?
6. Would merging cross gameplay/presentation/knowledge authority boundaries?
7. Would the result become genuinely monolithic rather than merely substantial?

If consolidation would blur authority or semantic distinction, keep the split.

---

## 53. Preferred Architectural Outcome

The preferred Admirals codebase should feel like:

```text
few substantial owners
        ↓
clear responsibilities
        ↓
small stable public boundaries
        ↓
reusable common primitives
        ↓
embedded local details
        ↓
minimal duplicate truth
        ↓
minimal cascading edits
```

not:

```text
many tiny files
        ↓
many wrappers
        ↓
many forwarding calls
        ↓
unclear ownership
        ↓
changes fan out everywhere
```

and not:

```text
one enormous manager
        ↓
every subsystem mixed together
        ↓
presentation + gameplay + data coupled
```

The target is the middle:

> **Consolidated, cohesive, type-safe where it matters, deliberately separated where responsibility is substantial, and structured so future changes remain local rather than cascading through the project.**
