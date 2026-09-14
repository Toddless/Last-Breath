# Shared Interaction Architecture

> Текущий статус: #265 и #266 реализованы. Настройка сцен сундука описана в разделе «Реализовано в #265»; интеграция NPC и её проверки — в NpcInteractions.md. Ниже сохранён исходный проект #264.

Status: design proposal for #264, ready for review. Runtime implementation belongs to #265 and #266. The unlocked starter chest and partial-stack transfer rules were confirmed on 2026-09-11; remaining technical choices below are proposed within the approved concept.

## Overview

Exploration uses one target and one interaction key across containers, NPCs, resource nodes, and location exits. The player sees what can happen before acting. An object owns its behavior; the shared system selects it and presents the same availability rules that execution checks.

## Concerns

- LocationEndpoint currently consumes E independently; adding chest input beside it would make scene order choose the action.
- DialogueActor checks distance and NPC capability, but has no obstruction check or read-only dialogue-entry admission check.
- A hidden MainWorld still simulates. Distance alone cannot identify an eligible interaction.
- A menu can outlive an NPC, its location, or the conditions that made an action available.
- IInventory.TryAddItem is all-or-nothing and emits its own failure notification. Calling it repeatedly cannot implement partial stacks with one notification per Take All.
- Narrative conditions are not universally read-only: QuestOfferRollCondition consumes RNG and writes facts. Hint polling must never trigger that behavior.

## Success Criteria

- One press addresses exactly the indicated object, with no simultaneous travel or conversation.
- A nearer unusable object does not hide a nearby usable object; unavailable actions explain their reason when relevant.
- The same state produces the same availability in the hint, menu, and command handler.
- Neither a menu nor a delayed command can interact across a location transfer, battle, or unload.
- Opening, closing, saving, and revisiting the first chest preserve the concrete remaining items and their slot positions.
- New object behavior plugs into the shared selection flow without its own keyboard listener.

## Requirements

### Must Have

- Nearest reachable target in the player's active exploration space; no obstacle between the player and interaction point.
- Shared InputMap action interact, currently E, and a prompt that reflects its current binding.
- One available action executes directly; several available actions open a compact menu.
- Explicit action identity, availability, localized refusal reasons, and authoritative revalidation.
- Scene-authored UI and composition-based object behavior.
- Location-state integration using the existing ILocationStateParticipant contract.
- An unlocked first chest with authored JSON contents, partial-stack transfers, and one capacity notification per transfer operation.
- Permanent chest removal only after emptying and its game-time delay.

### Should Have

- Stable target selection at equal distance, editor-configurable anchors/range, and clear data-validation diagnostics.
- A common action contract that can later describe resource gathering and locked chests without implementing those mechanics now.

### Might Have

- NPC resource gathering and robbery after their gameplay rules are specified.

## Proposed Design

### Target Selection and Reach

The player has one PlayerInteractionController. Targets register while inside the scene tree and unregister on exit. The registry contains live objects only; it never retains unloaded native nodes.

Each physics update:

1. Suppress discovery while the player is dead, fighting, transferring/loading, has text focus, or is blocked by an interaction/window session.
2. Restrict candidates to the same live World2D as the player and the active, prepared LocationRoot. Reject hidden/inactive viewports even when their simulation is running.
3. Measure squared distance from the player's InteractionOrigin to each target's InteractionPoint. Use the target's configured maximum distance.
4. Ray-test the segment in that World2D against physical interaction blockers. Exclude the player's own body and the target's own body; other walls, trunks, solid containers, and blocking bodies remain obstacles. Visual foliage, prompt controls, and trigger areas are not blockers.
5. Evaluate visible actions without changing game state. Prefer the nearest candidate with at least one enabled action. At an exact distance tie, retain the current candidate if tied; otherwise use a stable target key.
6. If no actionable target exists, select the nearest reachable target with visible disabled actions and display its reason. If none exists, clear the prompt.

A wall-obstructed target receives no interaction prompt. A reachable vein without the required tool can explain that requirement. This keeps physical access separate from action requirements.

InteractionPoint is placed on the object's accessible footprint, not at the top of its artwork. A large object may have several authored points: choose its nearest point that passes both distance and obstruction checks, then rank the object once using that point. All points share one target identity. No automatic walking/pathfinding is introduced.

The default range is a shared exploration setting, initially matching the current 150-unit NPC reach for playtesting. Existing exit ranges can override it (currently 180). These values are tuning defaults, not game-balance commitments. Any candidate broad phase must include the largest authored range.

Interaction blockers use named collision-layer settings/masks. A solid object's entire collision body must not be globally ignored just because it is interactable; only the selected target's own colliders are excluded from its ray.

### Contracts and Responsibilities

Names below are proposed APIs, not existing implementations.

| Element | Responsibility |
| --- | --- |
| InteractionTarget : Node2D | Live registration, stable object identity, interaction points, hint anchor, range, and composed action sources. No keyboard input or gameplay switch by object type. |
| IInteractionActionSource | Supplies actions for the object's current state and implements its domain checks/execution adapter. Chest, dialogue, and travel are separate sources. |
| InteractionActionView | ActionId, LabelKey, display order, Visible, Enabled, ReasonKey, optional reason values, and activation policy. It contains no executable UI callback. |
| InteractionTargetHandle | LocationId, local object ID, and a transient binding generation. Saved identity uses the first two; requests also require the current live binding so a reloaded object rejects old requests. |
| InteractionGate | Shared actor/space/reach checks plus action-source checks. Evaluating availability does not consume items, roll RNG, award experience, open UI, or mutate facts. |
| InteractionSnapshot | Target handle, presentation anchor, and the currently evaluated actions. A snapshot is display data, never permission to execute later. |
| PlayerInteractionController | Chooses the target, handles unconsumed input once, opens a menu or submits the chosen action. Keeps no durable world state. |
| InteractionSession | Pins the target while a menu or container window is open. Owns cancellation and busy state, and ends before the player or target leaves its valid space. |
| ExecuteInteractionRequest / InteractionResult | Request/response entry through GameMessageBus. Returns Completed, Started, or a refusal with a reason; dispatches by ActionId to the source. |
| InteractionPresentation | Projects the active target anchor to the session UI and presents hints/menu using the existing UI manager. Does not recompute gameplay availability. |
| InteractionChangedMessage | Optional notification after a committed object change. Consumers refresh snapshots; it carries IDs and data, not ownership of nodes. |

Author-created targets use a stable ObjectId within LocationId. NPC targets use the restored NPC InstanceId. An endpoint can derive its interaction identity from EndpointId. Native Godot instance IDs are never saved as object identity.

Check order is stable: valid actor/session -> active space and binding -> physical access -> target lifecycle -> action-specific requirements -> busy state. Expected refusals are localized player feedback, not exception logs. Invalid configuration is reported through Tracker and disables the affected action.

Execution resolves the exact submitted handle and action ID again. If the displayed target disappeared or changed binding, fail that request; do not silently apply it to a newly selected neighbor. Run the same checks immediately before mutation. A delayed operation repeats relevant checks after an await/deferred boundary. A per-session busy guard suppresses repeated presses and duplicate transfers.

### Input, Hint, and Compact Menu

InteractionTarget and individual action sources never listen for E. PlayerInteractionController handles the unconsumed action in the active player viewport. Ignore key echoes; UI receives its opportunity to consume input first. Check text focus in both the player viewport and root window.

| Evaluated actions | Presentation and E behavior |
| --- | --- |
| None visible | No hint. |
| Visible, none enabled | Muted object/action label and reason; no enabled key affordance. |
| Exactly one enabled | Key + its verb; execute that action after revalidation. |
| Two or more enabled | Key + localized Interact label; open a menu with visible actions. |
| One enabled action with explicit-choice policy | Key + its verb; open a menu requiring a separate selection. This is a proposed exception for Attack and other destructive actions. |

Disabled visible rows explain a temporary requirement, for example missing tool, missing key, or busy target. Unsupported actions are absent: a wolf does not show a disabled Talk row, and unimplemented robbery is not a selectable placeholder.

Menu rows keep their authored order when availability changes. Pin the target while open; do not retarget the menu as another NPC moves closer. Refresh conditions, disable invalid rows, and close if the target becomes physically inaccessible, changes space, dies, or is removed.

When an action opens a container or dialogue window, the session replaces its menu ownership before accepting more input; closing the replaced menu must not cancel the new window. The menu accepts mouse selection and keyboard navigation/accept; Escape closes it through the existing layer manager. Opening the menu must not also accept its first row. A row never activates because it became the only enabled row. Attack requires a distinct menu selection even if Talk becomes unavailable. This safety exception is presented for review, not recorded as prior approval.

InteractionMenuWindow is an IWindow, allowed in UiContext.World, with BlocksMovement=true. It pauses player walking only; NPC simulation, raids, and the clock continue. The session's own window must not invalidate its commands: discovery is blocked while the menu is open, but a request tied to that live session remains eligible. Other blocking windows and battle/load/travel invalidate it. Implement this distinction in one shared interaction/UI policy, not special cases in every action source.

Use one persistent passive hint in the world HUD. It has MouseFilter.Ignore, is not part of canopy dissolution, and remains above the object after camera movement. Menu rows consume mouse events; empty fullscreen UI roots do not. Project HintAnchor through the source viewport/camera and its container to root UI coordinates, then clamp the menu using UiPlacement.PlaceClamped. Validate resize, zoom, and side-location viewports.

### Scene Composition and Authoring Properties

Proposed scene structure:

```text
Player
  InteractionOrigin (Marker2D at the movement footprint)
  PlayerInteractionController

Chest (Node2D, ChestComponent, ILocationStateParticipant)
  Visuals (closed/open appearance)
  SolidBody (StaticBody2D + CollisionShape2D)
  InteractionTarget
    InteractionPoint (Marker2D)
    HintAnchor (Marker2D)
    ChestActionSource

NPC (existing body and gameplay)
  InteractionTarget
    InteractionPoint
    HintAnchor
    DialogueActionSource
    NpcAttackActionSource

LocationEndpoint (existing EndpointId and Arrival)
  Arrival
  InteractionTarget
    InteractionPoint
    HintAnchor
    TravelActionSource

WorldHud
  InteractionHint (instanced scene)

WindowLayer (existing)
  InteractionMenuWindow (instanced on demand)
    Panel
      ActionRows (instanced InteractionActionRow scenes)
  ChestContentsWindow (instanced on demand)
    ContentsGrid (instanced slot scenes)
    TakeAllButton
```

ChestComponent may be the chest root script; a separate state-adapter node is unnecessary when the root can implement ILocationStateParticipant. All UI modules are authored as .tscn scenes. Use shared visual components where their functionality matches; scene instances receive view data through their normal initialization APIs.

| Owner | Authored properties |
| --- | --- |
| InteractionTarget | ObjectId or identity source; InteractionPoint nodes; HintAnchor; optional range override; action-source children. |
| Shared interaction configuration | Default range and blocker mask; named InputMap actions; no per-object key bindings. |
| ChestComponent | ObjectId; DefinitionId referring to JSON; visual references. No item list embedded in the script or scene. |
| DialogueActionSource | Optional NPC-definition override and standalone faction fallback, preserving DialogueActor authoring behavior. |
| TravelActionSource | Reference to the existing LocationEndpoint; no duplicate destination field or graph. |
| Action row | Label, key/selection indicator, disabled reason, and danger presentation; no domain services. |

Interaction geometry is distinct from solid collision and visual occlusion. New runtime components do not need C# Tool execution; use exported scene properties and ordinary scene authoring to avoid the documented unload problem.

### Migration from Existing Interactions

| Existing owner | Planned integration |
| --- | --- |
| DialogueActor | In #266, replace its click listener with DialogueActionSource under the shared target. Preserve definition override, parent InstanceId/faction, and standalone static-talker support. Never leave both listeners enabled. |
| DialogueService / IDialogueService | Add a read-only entry-admission check shared with Start; actual Start still owns conversation state and effects. No speculative Start/End call for a hint. |
| LocationEndpoint | In #265, move E handling into the common controller and expose Travel. Keep EndpointId, Arrival, catalog connections, and LocationCoordinator as the travel authority. |
| LocationCoordinator | Its execution path must call the shared physical access check too, so a direct TravelRequest cannot bypass obstruction checks. Its existing destination/transition checks remain authoritative. |
| Neutral NPC combat | In #266, adapt explicit Attack to the existing world battle-start path, including group admission and marker creation; do not construct a separate arena or publish fictional contact events. |
| Checkpoint / corpse burning / ground loot | Keep their existing behavior until separately migrated; do not register duplicate shared actions beside their old listeners. Their remaining click paths are documented migration work, not a claim that every interaction has already moved to E. |

Read-only dialogue admission checks CanTalk, identity/catalog, lifecycle, and a matching entry rule. Preserve current reputation behavior unless authored entry rules reject it; do not add a blanket hostile-NPC dialogue prohibition implicitly.

Dialogue-entry evaluation needs a purity boundary. QuestOfferRollCondition currently mutates facts/RNG and is used in dialogue options. It must not run while selecting a target. Validate that entry conditions are preview-safe, including nested conditions and indirect quest-acceptance dependencies; reject unsupported effectful entry rules with a diagnostic. Existing effectful option evaluation remains at actual conversation execution. Pure entry resolution and Start share priority/order logic so they cannot disagree on unchanged state.

Robbery remains outside #266, as its issue explicitly states. Lockpicking, force opening, inspection, gathering, and keys use the same action contract later; this design does not decide their unresolved outcomes or numerical balance.

### First Chest: Data and State

The first chest is unlocked and uses an Authored contents definition. Scene placement supplies ObjectId and DefinitionId; the shared game-data pipeline resolves the definition.

Proposed authored definition fields:

| Field | Meaning |
| --- | --- |
| id / nameKey | Stable definition ID and localized name. |
| contents.mode | Authored for the first chest. Random and Mixed are future schemas using the approved category/rarity rules. |
| contents.items | Ordered records containing stable slotId, itemId, and positive amount. Item IDs use existing item factories/catalogs. |
| access.mode | Unlocked for the starter chest. Other modes await lock design. |
| emptyRemovalDelayMinutes | Non-negative delay on the existing game clock; chosen by content authoring. |

Create concrete authored item instances on the first successful Open, through IItemCreationService. Stage all entries before committing initialization: a missing item definition must not leave a partly generated chest. A restored initialized chest never re-mints its items. Saving an unopened, uninitialized chest records that explicitly. The first chest has fixed item IDs and counts; any requirement for exact rolled equipment values must be represented by authored item data, not a new roll on every open.

Proposed persistent state:

| Field | Meaning |
| --- | --- |
| version / definitionId | Object-state version and source definition. |
| initialized | Distinguishes never opened from an initialized empty container. |
| state | Closed, Open, or Empty. Removed objects use the location snapshot's existing null tombstone. |
| slots | Original slot IDs/order, concrete item payloads, and remaining quantities. Empty positions remain empty; do not compact the grid. |
| emptyAtMinutes / removeAtMinutes | Absolute game-clock timestamps set once when the final quantity leaves. |

State transitions: Closed -> Open on successful initialization/open; Open -> Empty when the last quantity is transferred; Empty -> permanent removal at its deadline. Closing the UI does not close the lid or restart a timer. Open nonempty chests remain available indefinitely. Empty chests retain their empty appearance until removal and do not advertise a usable Open action.

Capture through ILocationStateParticipant; serialize items through the existing InventoryItemSaveConverter. Restore object state before enabling interaction and never generate contents during location preparation. Reconcile disappearance against IWorldClock.TotalMinutes and the saved absolute deadline; repeated visits do not add elapsed time to the deadline twice. Use the existing location tombstone when removal commits.

### Container Session and Transfer Contract

Open creates ChestContentsWindow for one target-bound session. Right-click a slot requests transfer of that slot's remaining quantity. A named InputMap action container_take_all is active only inside this window, with a visible button and binding; the proposed initial key is R, independently rebindable from E.

The window and every transfer request check the same active session, target existence/binding, open/access state, physical reach, and player state. Close on Escape, removal, obstruction/out-of-range, battle, death, travel/load, or replacement by another object session. Player walking is blocked while this window is open; external movement and NPC actions can still invalidate it. Closing never mutates remaining contents.

Partial-stack transfer is confirmed. Add an inventory-domain operation such as TryReceiveUpTo(item, requestedAmount) -> acceptedAmount. It computes actual room from compatible stacks and empty slots, commits exactly the accepted quantity, and returns zero without mutation when full. Keep existing TryAddItem all-or-nothing semantics for its current callers. Do not infer capacity from the count of empty slots.

Container transfer proceeds synchronously under one operation guard:
1. Resolve and revalidate the exact source slot and session.
2. Ask the inventory to accept up to the requested amount.
3. Subtract exactly the accepted quantity from that source slot. Preserve its item data and position for any remainder.
4. Commit chest lifecycle changes before publishing inventory/object notifications that may trigger another action or save.

No await, animation completion, or UI callback may sit between destination acceptance and source reduction. Provide a deferred-notification boundary for the transfer path: ordinary inventory events currently fire during TryAddItem, before a container could subtract its amount. A re-entrant save must see either the before or after state, never both copies. On failure before commit, neither owner loses items.

Take All enumerates stable source slot IDs in order. It tries the remaining quantity of each slot even after an earlier refusal or partial result. Return aggregate accepted/remaining quantities and CapacityLimited. The controller emits one refusal sound and one inventory-full notification per operation when contents remain because of capacity; the quiet inventory operation must not issue its own repeated toast. Revalidate on any re-entrant state change; cancel remaining work if the session becomes invalid.

An equipment item transfers as one concrete instance. Stackable quantities use existing compatibility rules; do not reroll, rebuild, or change source item properties. A partially transferred stack may use the inventory's normal stack identity handling, while the chest retains the untransferred quantity.

### Validation Plan

These are acceptance checks for implementation, not tests already executed in #264.

| Scenario | Required result |
| --- | --- |
| Two reachable targets | Closest actionable target wins; exact ties remain stable. |
| Disabled nearer vein, usable chest behind it | Chest selected if its own ray is clear; unavailable vein does not steal E. |
| Wall, tree trunk, solid object between actor and target | No interaction; target's own collider does not obstruct its own anchor. |
| Same coordinates in hidden MainWorld and active side location | Only active same-space target is eligible. |
| Chest beside an exit | One displayed target and one action per E press, independent of scene order. |
| Chat/console focus or blocking window | No exploration interaction or unintended player movement. |
| One / several / zero enabled actions | Direct action / menu / explanatory disabled presentation. |
| Menu opens on an input press | That press never also accepts a row; Attack needs explicit selection. |
| NPC moves, dies, begins battle, or changes dialogue facts | Availability updates; stale selection refuses without retargeting. |
| Repeated hint polling | No quest-offer RNG, fact writes, experience, item generation, or dialogue execution. |
| Missing dialogue entry / invalid target data | Disabled/absent action and diagnostic as appropriate; no dead-end window. |
| Root-window input through HUD, scaled side viewport | Correct hint/menu placement; interactive controls consume clicks, empty overlays do not. |
| Slot has 10 resources, bag can accept 3 | Bag gains 3; 7 remain in the same chest slot. |
| Take All hits full equipment capacity then a compatible stack | Equipment remains; fitting later resources transfer; one capacity notification. |
| Duplicate request or save from an item-change listener | No duplicate items, partial ownership, or reset deadline. |
| Close/reopen, full reload, side-location unload/revisit | Identical residual items/rolls/slots; no reroll or double elapsed-time application. |
| Empty chest deadline expires while away | Permanent removal, retained after save/load. |

Authoring validation checks unique target/object and slot IDs, nonempty action IDs, existing definition/item references, positive quantities, finite non-negative ranges/deadlines, and required anchors. NPC and endpoint identity sources must not collide with authored IDs.

## Implementation Order

1. Review this contract (#264).
2. In #265, implement the shared target/gate/controller and passive hint; migrate travel input so E has one owner. Use a fixture action source for multi-action and refusal tests.
3. Implement the starter chest definition, concrete state, transfer operation, scene-authored menu/container UI, and location-save adapter. Validate partial transfer and repeated saves/revisits.
4. In #266, migrate DialogueActor and add explicit NPC Attack through the existing battle pipeline, including read-only dialogue admission checks.
5. Later stages add locks, inspection, resource gathering, and visual environment behavior using their own approved rules.

## Questions and Review Defaults

| Topic | Proposed default / status |
| --- | --- |
| First chest lock | Confirmed: unlocked. |
| Partially fitting stack | Confirmed: transfer what fits; preserve the remainder in its original slot. |
| A destructive action is the only enabled action | Proposal: require explicit menu selection; ordinary single actions remain direct. |
| Menu/container window movement | Proposal: block player walking, keep the world running, and close on lost physical access. |
| Take All binding | Proposal: R via a named InputMap action, with a visible button. |
| Distances and disappearance delay | Tune range in the level; author delay in data before chest implementation. |
| Exact starting equipment | Supply valid authored item IDs/counts before #265's content acceptance. |
| Locks, inspection, lost contents, robbery, mining details | Remain separate design questions; they do not block the unlocked starter-chest contract. |

## Decision Record

Confirmed additions: the starter chest has no lock, and transfer accepts partial stacks. Both preserve a simple first interaction and prevent capacity from wasting otherwise usable inventory space.

The proposed architecture composes action sources under one target, uses one shared validation gate and one input owner, and binds open UI to a live target session. This adds a small coordination layer and migration work for exits/dialogues, while keeping chest contents, combat, travel, and narrative behavior in their existing domains. Approval of #264 should cover the review defaults before runtime implementation.
## Уточнение реализации: обнаружение через коллизии

Уточнение от 2026-09-11 заменяет требование полного обновления выбора на каждом физическом тике. У игрока есть Area2D-датчик, а у целей — области обнаружения на отдельном слое. Сигналы входа/выхода поддерживают небольшой набор кандидатов; изменения набора помечают выбор для обновления. Дистанция, препятствия и доступность пересчитываются только для этого набора с настраиваемым интервалом (начальное значение 0,1 с), в физическом цикле. Это необходимо и при неподвижном игроке: NPC и препятствия могут двигаться.

Нажатие E ставит запрос на ближайший физический тик: система повторно проверяет именно показанную цель, затем выполняет действие. Передача управления/загрузка, удаление цели и закрытие окна сбрасывают ожидающий запрос. Полного сканирования локации нет; датчик после смены World2D очищает старых кандидатов и получает новые перекрытия из текущего пространства. Области обнаружения покрывают все точки взаимодействия и их допустимые дистанции, а сами не считаются препятствиями.

Реализация начинается с общего взаимодействия, переноса E у выходов и первого авторского сундука (#265); перенос диалогов и атака NPC остаются этапом #266. Стартовый комплект подтверждён и указан в отчёте о реализации ниже.


## Реализовано в #265 (2026-09-11)

Общий выбор цели, подсказка, меню и первый авторский сундук подключены. Выходы между локациями используют общий контроллер E. NPC пока используют прежний механизм: перенос диалога и явная атака относятся к #266. Проверка меню с несколькими действиями выполнена на отдельном тестовом источнике действий.

### Настройка сцены

- У игрока один PlayerInteractionController. Сигналы Area2D поддерживают кандидатов; RefreshSeconds задаёт период проверки расстояния, препятствий и действий (0,1 с). Нажатие повторно проверяет показанную цель в физическом цикле.
- У объекта — InteractionTarget с уникальным ObjectId в локации, Reach в мировых единицах (по умолчанию 150), точками Marker2D с префиксом InteractionPoint и HintAnchor для подсказки. При отсутствии точек используется позиция цели.
- Слой физики 16 назван InteractionTargets и зарезервирован для обнаружения. ObstacleMask у цели задаёт слои твёрдых препятствий (по умолчанию 1–15); триггерные области и собственные коллайдеры игрока/выбранного объекта не закрывают доступ.
- Поведение реализует IInteractionSource на родителе цели либо её дочернем компоненте. Actions только описывает текущие действия, без генерации лута и иных изменений состояния. Execute выполняет действие после общей проверки.
- Для сундука используется Chest.tscn. У экземпляра задаются стабильный ObjectId и DefinitionId из каталога Chests. ObjectId нельзя переиспользовать для другого объекта в той же локации: по нему восстанавливается состояние и постоянное удаление.
- Форма области обнаружения рассчитывается при создании из дистанции и точек взаимодействия. Их расположение, масштаб цели и Reach задаются до входа в сцену; динамическая перестройка этой формы пока не предусмотрена.

### Данные первого сундука

StarterChest находится в SourceOfPowerNearVillage рядом с входом. DefinitionId — Chest_Starter, ObjectId — starter_chest. Каталог Chests содержит id, nameKey, emptyRemovalDelayMinutes и items; у каждой позиции — стабильный slotId, существующий itemId, положительный amount и строковая rarity.

Все пять предметов выдаются по одному, с редкостью Uncommon:

| Предмет | ID |
| --- | --- |
| Простой кинжал | Weapon_Simple_Dagger |
| Шлем Stoneheart | Helmet_Stoneheart |
| Перчатки Stoneheart | Gloves_Stoneheart |
| Сапоги комплекта | Boots_Stone_Tread |
| Доспех Stoneheart | Body_Stoneheart |

Boots_Stone_Tread — ID сапог в текущем каталоге; Boots_Stoneheart отсутствует. Сундук не заперт. Предметы создаются при первом открытии, затем сохраняются конкретные экземпляры и их параметры. Подсказка не создаёт содержимое.

### Управление и сохранение

E открывает доступный сундук или активирует выбранный выход. Несколько доступных действий открывают меню; действие с ExplicitChoice всегда требует выбора, даже если оно осталось единственным. ПКМ по позиции переносит её содержимое, R или кнопка Take All перебирает все позиции по порядку. Если часть стака помещается, переносится только она; отказ в одном слоте не мешает попытке перенести следующие. Оставшиеся предметы сохраняют свои позиции.

Окно блокирует ходьбу игрока, но не симуляцию мира. Потеря доступа, закрытие, удаление цели и переход между пространствами отменяют сессию и ожидающий запрос. Старый идентификатор привязки не может обратиться к новому экземпляру объекта после загрузки.

Пустой сундук исчезает через 5 игровых минут — это начальная настройка emptyRemovalDelayMinutes. Сохраняется абсолютный срок; время вне локации учитывается при возвращении. Удаление фиксируется существующим механизмом tombstone. Непустой сундук по таймеру не удаляется.

Визуал сундука — временные настраиваемые полигональные формы открытого/закрытого состояния. Окна, строки списка и подсказка оформлены отдельными сценами.

### Выполненная проверка

- 6 управляемых тестов ChestContentsTests: частичная вместимость, продолжение перебора после отказа, отсутствие повторной генерации, согласованность уведомлений и защита от повторного входа.
- InteractionTest внутри Godot: 38 проверок выбора, препятствий, фокуса ввода, меню, ПКМ/R, переноса, полной загрузки через SaveDirector, выгрузки/возврата, устаревших запросов и постоянного удаления.
- LocationTravelTest: 34 проверки переходов и восстановления.
- SpaceIsolationTest: 31 проверка физических пространств и входа/выхода из боя.

Нативные проверки выполняются в Godot. Для чистых .NET-тестов используются управляемые подстановки IItem, без создания нативных классов.
