# AI, создание и жизненный цикл NPC

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- Боевой AI: `UtilityTurnPlanner` играет ход NPC (скоринг способностей → касты → базовая атака) через `ICombatEnvironment` (реализует BattleArena). Поведение = `BehaviorProfile` из `NpcBehavior.json` (архетип жёстко от стойки: Dex=агрессия, Str=защита, Int=смешанный), интеллект — явное поле `AiIntellect`.
- Мировой AI: `WorldBrain` (Stateless FSM Calm/Suspicious/Alert/Search/Flee) + активности Idle/Wander/Patrol; тело за `IWorldAgent` (BaseNpc: движение прямой, зрение поллингом игрока). Стимулы (`WorldStimulusEvent`) через GameEventBus. Бой может стартовать физическим контактом или явной атакой через общее взаимодействие.
- Данные NPC: `Npc.json` → `NpcProvider` → `NpcDefinition` → `BaseNpc.ApplyDefinition` (статы/стойка/способности/мозг/lifecycle). EntityType задаёт макс. уровень и число способностей (Regular=Обычный 15 … Archon 150; Boss/Archon вручную).
- Цикл нежити: `NpcLifecycle` — тело после смерти лежит в мире; не-нежить восстаёт нежитью по таймеру (сильнее ∝ времени), нежить в анабиозе; сжигание кликом = финальная смерть (`NpcFinalDeathEvent`).
- Спавн: `NpcSpawnPoint` (лимиты точки, респавн замены по событиям) + `NpcPopulationService` (глобальный лимит живых NPC).
- **Мирные жители (2026-08-08)**: у циклов жизни общий контракт `INpcLifecycle` + два суб-интерфейса исходов подъёма — `IUndeadRiseLifecycle` (`ResurrectionReady(float)`, нежить, сила ∝ пролёжанному) и `IAliveRiseLifecycle` (`ReviveReady`, встал живым). Подписка на исход, которого у цикла нет, невозможна по контракту — это несущее различие, не менять на флаги. `VillagerLifecycle`: сбит → лежит → по таймеру встаёт ЖИВЫМ (та же фракция, без бонусов/тинта, БЕЗ событий — слот спавн-точки занят, личная репутация игрока переживает нокдаун); не горит, финальной смерти нет; любая лежачая стадия из сейва = «лежит и восстановится» (вечный труп запрещён). Цикл строится ТОЛЬКО `NpcLifecycleFactory.Create(definition, rnd)` — switch в одном тестируемом месте, обе копии `BaseNpc` зовут фабрику. Выбор — данными: `lifecycle.kind: "Villager"` (+`recoverMin/MaxSeconds`; отсутствие kind = цикл нежити, опечатка = отказ записи). Сейв: `RestoreAsRisen` только для `IUndeadRiseLifecycle` — старый сейв не поднимет жителя нежитью.
- **Авторский именованный спавн**: секция `authored` в записи Npc.json (`stance`/`level`/`rarity`/`modifierCount` — все опциональны, названное заменяет свой ролл; `modifierCount: 0` = модификаторы не роллятся, отрицательное = 0). Приоритет: рантайм-overrides > authored > старые поля (`rarity`, одна стойка, `levelMin==levelMax`) > ролл; архетип следует за авторской стойкой. При новом создании состав модификаторов и выбор способностей роллятся; восстановление снимка локации использует сохранённые списки. Фиксированная позиция = спавн-точка radius 0 / max 1.
- **Сохранение NPC в Main**: `LocationCoordinator` пишет секцию `locations` v1. `LocationNpcState` сохраняет `InstanceId`, вид, уровень, редкость, стойку, исходные параметры, списки способностей и модификаторов, HP/MP/барьер, позицию и дом, маршрут, группу, владельца спавна, признак `Wild`, стадию lifecycle и таймеры. `RestoreLocationNpc` восстанавливает эти значения; живой раненый NPC не становится автоматически здоровым. Прежняя секция `npcWorld` относится к совместимости старых сохранений и не описывает текущий основной путь Main. `SpawnNpcAction` создаёт дикого NPC; принадлежность спавн-точке и признак `Wild` остаются разными понятиями.
- **Активность Work**: `{activity: "Work", point: "Forge"|"TradeStall"}` в расписании — клейм smart-точки по тегу + рабочая поза (третья конфигурация `PointPoseActivity`, чувства не глушит, фактов не тикает); без точки — мягкая деградация «стоять дома». Образец полного дня — `Npc_Ronald` (кузня → прилавок → костёр → сон; торговец `Trader_Ronald` через диалоговое `StartTrade`).
- ВАЖНО: `BattleEndEvent` публикуется в ОБЕ шины (game — лут, battle — выход Player/NPC из Fight-состояний; публикует BattleContext).

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Ai/World/WorldBrain.cs`, `Core/Ai/World/NpcLifecycleFactory.cs`, `Main/Npc/LocationNpcState.cs`, `Main/World/Locations/LocationCoordinator.cs`.

## Связанные документы

- [Группы NPC, оповещения и подкрепления — проект архитектуры](EntityGroupsArchitecture.md)
- [Боевой урон и события](../Combat/CurrentSystem.md)
- [Локации, пространства боя и снимки мира](../Locations/CurrentSystem.md)
- [Общее взаимодействие, сундуки и NPC](../Interaction/CurrentSystem.md)
- [Репутация и рейды](../Reputation/CurrentSystem.md)
- [Генерация лута](../Loot/CurrentSystem.md)
- [Карта документации](../README.md)
