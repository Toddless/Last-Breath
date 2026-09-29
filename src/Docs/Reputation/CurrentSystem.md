# Репутация и рейды

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- **Очки, не уровни**: int per-фракция −6000..6000, `RelationLevel` — производный от порогов из `SharedData/Factions/FactionRelations.json` (Hatred −6000 / Hostility −2000 / Dislike −800 / Neutral −200 / Friendly 1000 / Respect 3000 / Alliance 5000; смена уровня с гистерезисом 50 — уровень хранится как состояние). Дефолты: Undead −1000 (Hostility — атака on sight сохранена), Human 500, остальные 0. Флаги фракций: `hasReputation` (Animal/MysticalCreature false — реп заморожена, свидетелями не являются), `canRaid`. Матрица фракция-vs-фракция СТАТИЧНА — динамичен только слой игрока.
- **`IFactionRelationService`** (единственная реализация в `Core/Reputation`): `AddReputation(faction, delta, reason)` — единственный игровой путь; `SetPlayerRelation(level)` = прыжок в середину диапазона уровня (квесты/миграция); события `PlayerReputationChanged`/`PlayerRelationChanged` (только пересечения порогов). ВАЖНО: пустой ctor нужен DI (данные приходят через Apply), ctor с данными — тестовый.
- **Деяния**: `ReputationDeedProcessor` — единственное место превращения поступков в репутацию; каталог `SharedData/ReputationDeeds/ReputationDeeds.json` (дельта; `noPenaltyAtOrBelow` — анти-death-spiral пол: убийство уже-враждебных бесплатно; `hostileToTargetBonus` — плюс врагам жертвы по матрице; `personal`; `requiresWitness`; `repeatDecay` — сессионное затухание против фермы). Убийства выводятся из `EntityDiedEvent.Killer` (= последний источник урона из TakeDamage; `DefeatInWorld`/`Kill()` обнуляют — скирмиши не винят игрока); остальные системы публикуют `PlayerDeedEvent(deedId, faction, instanceId?, position)`. Всё глушится при `ILoadScope.IsLoading`.
- **Свидетели (уровень 0)**: `IWitnessQuery`/`WorldWitnessQuery` — живой не-сражающийся NPC реп-фракции в `witnessRadius` ИЛИ сбежавший из текущего боя (гарантированный свидетель до BattleEndEvent; `EntityFledBattleEvent` теперь публикуется и в game bus). Якорь радиуса — мировая точка боя из `BattleInitializedEvent` (в бою Position бойцов = спот арены!). Нет свидетелей → деяние с `requiresWitness` не произошло вовсе (ни штрафа, ни бонусов, decay не сжигается).
- **Личный слой**: `IPersonalReputationService` — очки per-NPC (InstanceId), секция `personalReputation` в FactionRelations.json (±1000, 300 очков = 1 ступень лестницы, кап ±2). Эффективное отношение = фракционный уровень + сдвиг; его читает `BaseNpc.ConsidersPlayerAnEnemy` (спасённый гном не атакует; флаг `hostileToPlayer` бандитов — абсолютный оверрайд). Память умирает с носителем: FinalDeath/FactionChanged → Forget.
- **Транслятор**: `ReputationBroadcaster` зеркалит события сервиса в GameEventBus (`ReputationChangedEvent`/`PlayerStandingChangedEvent` — заинтересованные подписываются на шину вместо инжекта сервиса) + шлёт тосты. Пайплайн нотификаций умеет шаблоны: `SendNotificationMessageMessage.Values` → попап рендерит `Localization.Render`. Ключи: `Fraction_*`, `RelationLevel_*`, `UI_Reputation_Changed`, `UI_Standing_Changed`, `UI_Raid_Started`.
- **Рейды**: `RaidService` (каталог `SharedData/Raids`, тикается `NpcWorldDirector`): Hatred + `canRaid` → кулдаун/шанс → отряд 2–4 у БЛИЖАЙШЕЙ к игроку спавн-точки фракции (`NpcSpawnPoint : IRaidSpawnSite`, фракция лениво из первого npcId; реестр `IRaidSpawnRegistry`). Рейдеры БЕЗ мирового мозга (`definition with { World = null }`) — сервис сам ведёт их `IWorldAgent.MoveTo` на игрока, бой по контакту; `INpcPopulationService.ReserveOutsideLimit()` — вне капа, но занимают счётчик (спавн-поинты ждут). Таймаут → выжившие уходят ЧЕРЕЗ `NpcFinalDeathEvent` + `INpcWorldSpawner.Despawn` (штатная очистка популяции/личной памяти); активный бой доигрывается. Активный рейд блокирует `CanSave`. Убийство рейдеров бесплатно автоматически (пол Hostility).
- **Перки**: `IReputationPerkProvider` + `SharedData/ReputationPerks/ReputationPerks.json` — уровень → ПОЛНЫЙ набор перков (`Perk_Price_Change`, `Perk_Reward_Change`, `Perk_Mythic_Gear_Access`, `Perk_Settlements_Closed`, `Perk_Entrance_Fee`). TradePricing уже читает Perk_Price_Change для покупки и продажи; наличие остальных записей не означает наличие потребителей каждого перка.
- **Сейвы**: `factionRelations` v2 (очки+уровень; миграция v1: level → середина диапазона), `personalReputation` v1, `raids` v1 (кулдаун) — последние два регистрируются опционально (проект без сервиса не пишет секцию).
- **Сим-инварианты** (`src/Testing/ReputationSimulation`, реальный пайплайн + реальные SharedData-конфиги, быстрые — в обычном прогоне тестов): потолки фермы (убийства капятся полом на Hostility — Hatred одними убийствами НЕДОСТИЖИМ, это осознанный анти-death-spiral; ферма нежити ≤ ~+100 без смены уровня), глухота без свидетелей, отсутствие мигания порогов, когерентность всех json, seeded-статистика прихода рейдов. Баланс репутации меняем ТОЛЬКО с зелёными инвариантами.

Пространственная привязка рейдов учитывает локацию: выбор спавн-точки фильтруется через `ISpatialQuery`, а преследование участвующего в бою игрока использует маркер исходного боя и `BattleId`.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Reputation`, `Core/Ai/World/Raids/RaidService.cs`, `Core/Trade/TradePricing.cs`.

## Связанные документы

- [AI, создание и жизненный цикл NPC](../AI/CurrentSystem.md)
- [Торговля и кошелёк](../Trade/CurrentSystem.md)
- [Локации, пространства боя и снимки мира](../Locations/CurrentSystem.md)
- [Регистрация сервисов и межсистемные сообщения](../Services/CurrentSystem.md)
- [Карта документации](../README.md)
