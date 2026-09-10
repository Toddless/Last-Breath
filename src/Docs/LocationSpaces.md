# Location and Battle Spaces

Status: architecture approved. Arena isolation, participant return, spatial services, and raid/marker targeting are implemented for #274–277. Location transitions and persistence remain planned for #278.

## Implementation Status

Implemented:
- MainWorld runs inside its own World2D/SubViewport; a transient battle presentation owns a distinct World2D and restores the origin view/input on disposal.
- BattleContext records each participant's original parent and transform, including late arrivals, and returns them after battle end or cancelled preparation. Nested parents are supported.
- EntitySpot owns the deferred transfer to its slot and cancels a pending transfer when the slot is released.
- Battle drops retain their origin parent; pickup, dialogue, checkpoint, and corpse-distance guards reject other physical spaces.
- ISpatialQuery resolves live World2D instance identity. Unknown or detached native objects do not share a space. Durable LocationId/SpaceId contracts remain part of #278.
- NPC sightings, hunting, skirmish recruitment, noise, smart-point claims, recovery zones, and witnesses respect spatial membership. Battle witnesses retain the origin space after the player transfers.
- BattleSiteRegistry holds the active marker's BattleId, origin space, and position without retaining scene nodes. Marker teardown and session reset remove this record.
- Reinforcement requests carry BattleId and remain bound to the receiving BattleContext, rejecting stale requests and arrivals from another space.
- Raids spawn in their source point's space and pursue the local player or the matching origin marker during battle. An inaccessible target clears movement while normal expiry and cleanup continue.
- Narrative NPC spawning also uses the named source point's space, independent of the player's current parent.

Validation:
- Main and Testing projects build with zero errors; Testing also builds the Battle dependency. Existing nullable/analyzer warnings remain.
- 73 focused logic tests pass, covering raids, spatial services, witnesses, claims, recovery, activities, reputation, world brains, narrative spawning, and session reset.
- The Godot SpaceIsolationTest scene passes 31 checks, including native sighting/noise isolation and marker identity, battle abort/return, late admission, cancelled preparation, hidden-world physics, target picking, and view sizing.
- The earlier rendered Compatibility run passed; world, arena, and return screenshots were inspected. Existing Effekseer path-case warnings remain.
- Run: Godot --headless --path src/Main res://Tests/SpaceIsolationTest.tscn --quit-after 1800 (build Main first).

Remaining:
- Data-driven location connections, side-location snapshots, elapsed-time reconciliation, and save orchestration (#278).
- Broader gameplay validation of death/flee/quit and spatial lighting/audio remains part of the integration work.

## Overview

The player can explore connected locations and enter a battle while MainWorld continues to live. A location preserves the consequences of previous visits. Battle remains connected to its place of origin through an audible marker that admits reinforcements.

## Concerns

- Scene parenting alone does not distinguish coordinates, collision contacts, or targets.
- Some consumers use the player's current GlobalPosition as a position in MainWorld.
- Current battle transfer and loot presentation assume a single world parent.
- Unloading a location can leave registrations, subscriptions, population reservations, or live node references behind.
- Rebuilding a scene can reroll loot, duplicate NPCs, or restore permanently removed objects.
- Replaying elapsed time twice can duplicate recovery and lifecycle transitions.
- Existing save restoration is synchronous; loading scenes inside individual save participants would introduce ordering problems.

## Success Criteria

- MainWorld continues its clock, NPC movement, raids, and abstract NPC combat while the player is elsewhere.
- Equal coordinates in different spaces never imply physical contact, perception, or interaction.
- Nearby NPCs in a battle's origin location hear the marker and can join that battle.
- Returning to a side location restores its state and applies elapsed game time once.
- A battle in any location returns participants and drops to that location.
- Authored entrances and exits support both dead ends and routes through several locations.

## Requirements

### Must Have

- MainWorld stays loaded and simulated throughout the game session.
- Side locations unload after the player leaves for another location. Their dynamic state persists.
- Elapsed time uses the existing game clock. Closing the application does not itself advance that clock.
- NPCs do not travel between exploration locations. Entering a battle and returning from it are controlled battle transfers.
- At most one player battle is active. NPC-versus-NPC combat retains its existing abstract rules.
- Connections explicitly identify both source and destination endpoints. A dead-end entrance has a reverse connection to the same outside entrance; a through-location may have multiple endpoints.
- BattleSiteMarker belongs to the location where combat began and remains an intentional source of noise and reinforcement requests.
- Location state and player placement integrate with the existing save system.
- Runtime services do not require C# Tool instances.

### Should Have

- A failed destination load leaves the player in the source location.
- Invalid endpoint references and duplicate object IDs are detected before play.
- Tests cover same-coordinate spaces, repeated transitions, and restored state.

## Proposed Architecture

### Identity and Coordinates

| Value | Meaning |
| --- | --- |
| LocationId | Stable authored location identity, independent of scene name and file path. |
| SpaceId | Runtime spatial identity. Each loaded location has one; each battle has a fresh one. |
| EntityId / ObjectId | Stable state identity within a location; persistent records use the location and object IDs together. |
| SpatialPosition | SpaceId plus coordinates relative to the space root. |
| EndpointId | Stable authored entrance/exit identity within a location. |
| BattleId | Identity of the current battle, used to reject stale join requests. |

A location can be unloaded while its LocationId and state remain valid. SpaceId identifies a live space and is resolved again on load; native node instance IDs are never saved as location identity.

Spatial queries first require a valid common SpaceId, then evaluate distance, visibility, faction, or other gameplay rules. Unknown or transferring membership is not eligible for spatial interaction. IsFighting remains a combat state, not a substitute for location identity.

### Scene Ownership

Suggested composition; names are illustrative:

```text
Main
  SimulationHost
  LocationCoordinator
  SpaceHost
    MainWorldViewport
      MainWorld (LocationRoot)
    SideLocationViewport (when needed)
      SourceOfPowerNearVillage (LocationRoot)
    BattleViewport (during a battle)
      BattleArena
  ActiveSpacePresenter
  UiLayerManager
```

Main owns these children and wires services. It does not implement travel rules or object persistence.

Each space uses a SubViewport with an explicitly distinct World2D. ActiveSpacePresenter presents only the selected space. The existing HUD/window/overlay layers stay at the session level. The implementation must preserve rendering quality, resize behavior, camera transforms, and coordinate conversion for picking.

Godot exposes a viewport's World2D and describes World2D as the holder of 2D rendering and physics resources. These are the basis for the proposed separation; actual scene integration is a validation task in #274. See [Viewport](https://docs.godotengine.org/en/stable/classes/class_viewport.html) and [World2D](https://docs.godotengine.org/en/stable/classes/class_world2d.html).

Visibility and simulation are separate controls. Hiding a viewport must not disable MainWorld processing or physics. Input is permitted only for the active playable space; polling global input must also check that permission. Spatial audio follows the active space; global notifications remain available.

### Responsibilities and Contract Sketches

Names below are proposed contracts, not claims about existing APIs.

| Component / service | Responsibility and key data |
| --- | --- |
| LocationRoot | LocationId, entity container, endpoints, state participants, and local registrations. Contains authored terrain, lighting, NPCs, and objects. |
| LocationCatalog | Data-driven LocationId to scene resource mapping and directed endpoint connections. Loaded through the existing game-data pipeline. |
| LocationCoordinator | Serializes travel, prepares destinations, commits player transfers, unloads abandoned side locations, and restores active location placement. |
| SpaceRegistry | Resolves loaded spaces and spatial membership. Provides space-filtered queries; does not calculate combat decisions. |
| SpaceHost | Owns viewport/native-world creation and disposal. Handles deferred tree changes without exposing half-transferred bodies. |
| ActiveSpacePresenter | Selects the displayed viewport, camera/input context, and audio listener. |
| LocationStateStore | Session-owned snapshots for unloaded locations, with versioned object state and LastSimulatedAt. Contains no live Godot references. |
| LocationStateParticipant | Captures/restores an object's dynamic state and, where applicable, reconciles elapsed game time. |
| BattleContext | Owns battle roster, local bus, origin, and per-participant return records. Uses the transfer mechanism instead of assuming MainWorld. |
| BattleSiteMarker | Origin SpaceId, BattleId, noise/contact settings; emits scoped noise and join requests. |
| SimulationHost | Ticks global clock and singleton world services exactly once. Loaded location simulation uses its own membership. |

Location changes enter through a request/response message such as TravelRequest(SourceLocationId, EndpointId). The response reports success or a specific refusal. After a successful commit, a notification announces the new active location. Arbitrary callers cannot teleport an entity by supplying unchecked coordinates.

### Authoring Locations and Connections

Each LocationRoot exposes its LocationId. Each endpoint scene contains an interaction/trigger area and a separate arrival marker. The marker supplies arrival position and facing; it must not immediately retrigger travel.

Directed connection data:

```text
MainWorld.VillageSource -> SourceOfPowerNearVillage.Entrance
SourceOfPowerNearVillage.Entrance -> MainWorld.VillageSource
```

A two-way passage is two explicit directed edges. Several exits in a location each resolve their own edge; no implicit "last visited scene" or global return stack determines the destination.

Catalog validation checks unique IDs, scene availability, endpoint existence, duplicate outgoing mappings, and valid arrival placement. New-game start also uses an authored location/arrival address; saved placement overrides it.

### Location Lifecycle

| State | Scene present | Simulation |
| --- | --- | --- |
| MainWorld active or background | Always | Continuous |
| Side location explored | Yes | Continuous |
| Battle origin side location | Yes, retained until battle ends | Continuous |
| Side location left through a connection | No, after transfer commits | Deadline reconciliation on next load |
| Destination being prepared | Temporary | Gameplay side effects suppressed until activation |
| Battle arena | Only during player battle | Battle rules |

Entering an arena does not leave the origin exploration location. The origin must remain resident for its marker, reinforcements, and safe return. After combat the player resumes there; normal travel can then unload it.

An unloaded location is not simulated frame by frame. On return, timed systems apply their own elapsed-time rules. Initial scope includes object recovery/disappearance and NPC lifecycle/recovery deadlines; schedules select their current activity. This does not simulate unseen movement paths or invent outcomes of historical NPC fights.

If unloading would interrupt an in-progress local abstract skirmish or another multi-entity operation, that subsystem must capture enough state to resume/reconcile it or reach a stable snapshot boundary before unload. Silently deleting the operation is not acceptable; exact subsystem adapters are implementation work.

### Travel Transaction

1. Validate the source endpoint, player state, and destination connection; reject a second transition or travel during battle.
2. Gate player movement/interactions and saving during transfer. MainWorld remains running.
3. Prepare the destination scene, validate its arrival, and restore its snapshot before enabling spawn or interaction side effects.
4. Reconcile unloaded time up to a single activation timestamp. Prepare the destination for registration and placement.
5. Detach the player through a safe tree/physics boundary; update membership, parent, placement, camera, and input as one coordinated operation.
6. Commit active location, then snapshot and unload the previous side location. Never unload MainWorld.
7. Publish completion and release the gate.

Before commit, failure discards the prepared destination and restores source controls. The source stays available until placement succeeds. Snapshot capture and deactivation must use one boundary so processing cannot occur after the recorded time and then be applied again.

### Battle Transfer and Reinforcements

Battle start records origin location/space and engagement position before moving any entity. Create the marker there and retain the origin location.

For every accepted participant, record EntityId, source location, stable source container identity, space-relative position, and necessary facing/transform state. Late arrivals get their own records at admission.

The transfer changes physical world and membership; world discovery no longer sees an arena participant. Reserve the arena slot and validate the request before detaching a late arrival. A refused join leaves the NPC in its origin space. A stale BattleId or mismatched origin is rejected.

At battle end, stop admission, return surviving entities and bodies according to existing lifecycle rules, and route drops to the battle's origin location. Then release battle context, marker, and arena. Maintain the transfer gate until participants are spatially valid. Existing local/game battle-end consumers must retain their behavior without exposing partially returned entities.

An active raid targets the player's accessible position in its own location, or the battle marker if the player is fighting there. When the player leaves through a location connection, the proposed initial behavior is to end the pursuit, stop stale movement, and let normal raid expiry/cleanup finish; NPCs never follow through the connection. This behavior is implemented for #276; raid rewards and duration are unchanged.

### Persistence and Time

Location unload captures state into LocationStateStore; it does not automatically write or overwrite a user save slot. Saving captures both unloaded snapshots and the current state of loaded locations.

Store:
- LocationId, schema version, and LastSimulatedAt in game time.
- Dynamic object records and permanent-removal records.
- Concrete remaining container/drop contents using existing item converters.
- NPC state, stable identities needed by references, spawn ownership, and lifecycle deadlines.
- Player location and space-relative placement in an extended playerPlacement section.

The existing playerPlacement v1 stores only X/Y. Migration assigns legacy placement to MainWorld. Fresh sessions use the configured start address.

Use one canonical game-time timestamp with sufficient precision for existing timers. Do not mix elapsed real seconds with game minutes: convert at subsystem boundaries and preserve timer precision when extending the clock/save representation.

On activation, restore state first, reconcile the interval from LastSimulatedAt to activation time once, then update the timestamp. A removed chest stays removed; an unopened/restored chest does not reroll contents. Resource recovery that would block an arrival or entity remains pending until its occupancy rule allows it.

Do not call the global SaveManager.Restore when entering a location: it resets the whole session. Per-location restore reuses subsystem serializers through the location-state layer and a local side-effect gate.

For full save loading, proposed sequence:
1. Stage and validate the save, then prepare required scenes without gameplay activation.
2. Perform the existing session reset exactly once.
3. Restore global clock and staged location snapshot data.
4. Restore existing global player sources/consumers in their established dependency order.
5. Hydrate required location objects, NPCs, spawn ownership, and drops before player placement.
6. Place the player in the saved location, reconcile any previously unloaded destination state, then activate simulation/input.

Implement scene preparation outside synchronous ISaveParticipant.Restore; extend restoration coordination rather than awaiting scenes inside a participant. Existing NPC, spawn-point, ground-item, and placement participants need location-aware ownership to avoid duplicate restoration. A corrupt/missing required location section must resolve to a coherent fallback or explicit load failure, not a mixture of two sessions.

Existing restrictions on saving in combat, death, and active raids remain. Add only the transition/load gate.

### Registries and Global Events

Live registries contain only loaded entities, indexed or filtered by SpaceId. Unloading unregisters entities, callbacks, smart-point claims, and spatial subscriptions without publishing fictional deaths.

Persistent identity/state survives unload. Population accounting must distinguish unloading from death; retain reservations for dormant saved residents so revisiting cannot duplicate NPCs or unexpectedly consume the same slot twice.

Keep the game clock, faction relations, quests, and other session systems global. Noise, proximity, witnesses, spawn queries, corpse interactions, and location drops carry or resolve spatial context. Battle witnesses use the origin location and existing battle-origin rules, not arena coordinates.

## Implementation Order

1. #273: review this architecture and accepted gameplay constraints.
2. #274 and #277: implement space hosting and safe battle transfer/return.
3. #275 and #276: migrate spatial consumers and raid/marker targeting.
4. #278: implement location connections, unload snapshots, and save/load orchestration.
5. Resume interactive environment implementation on the shared location and interaction contracts.

Each stage includes its regression checks. No separate rewrite of combat resolution or NPC-versus-NPC combat is required.

## Validation Matrix

| Scenario | Required result |
| --- | --- |
| World NPC and arena participant at identical coordinates | No cross-space collision, sighting, dialogue, or corpse interaction. |
| Battle in either exploration location | Marker remains at origin; local reinforcements can join; returns and drops use origin. |
| Raid already active when battle begins | Remaining raiders approach the marker, never arena coordinates. |
| Travel while a raid pursues | NPCs remain in origin; old target movement is cleared; normal expiry remains valid. |
| Revisit an emptied chest or mined vein | Contents/removal preserved; elapsed recovery applied once. |
| Save while in a side location, then load | MainWorld and side location state restored, player appears in correct location. |
| Repeat load or visit without advancing time | No extra recovery, duplicate residents, or rerolled items. |
| Destination missing or invalid | Source remains usable and player ownership is intact. |
| Hidden MainWorld during exploration/battle | Clock, movement, and world services continue exactly once. |
| Quit, battle abort, failed preparation | No stale callbacks or retained native/managed objects after teardown. |

## Questions for Technical Validation

- Verify independent World2D behavior, hidden-space physics, input picking, audio, and rendering scale in the actual project (#274).
- Determine the minimum persisted fields for unfinished local skirmishes and the NPC lifecycle adapters (#278).
- Audit game-time precision and conversions before timer migration (#278).
- Verify all registrations and existing drop/spawn/save consumers participate in location ownership (#275, #278).

## Decision

Main owns persistent world, optional side location, and transient battle hosts. Locations use explicit connections and durable state; side locations unload after exploration travel and reconcile elapsed game time on return. NPCs remain within their exploration location. A single player arena connects to its origin through BattleSiteMarker.

The proposed runtime uses separate physical worlds plus explicit spatial identity. Physical separation prevents accidental contacts; spatial identity makes global-service queries correct. The trade-off is coordinated migration of placement, input, registries, drops, and save restoration, rather than isolated guards in individual NPC methods.
