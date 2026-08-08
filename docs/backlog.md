# Бэклог: minor / nit / «замечено, не исправлено»

Пополняется из отчётов ревью и исполнителей. Формат: источник → пункт.

## Из ревью P-01 (перевес тестов дерева, 2026-08-08)

- (minor, отложенный перенос) Кейс условной строки дерева в `TurnConditionTests` — не в своей семье: дом предикатов «о состоянии носителя» — `ModifierConditionTests` (там готовые `Wounded`/`MaxHealth`/`Band`). В rework входит только правка summary; сам перенос — сюда.
- (nit) `PassiveTreeServiceTests` — guard `TouchesGranted(tree, granted, second)` в `FindChain` объяснён неверным комментарием и без нужды сужает пул кандидатных цепочек (WouldOrphan держится и без него). Снять guard или переписать комментарий.
- (nit) `TurnConditionTests.ShippedTreeGatesALineOn` повторяет обход каналов условий из `PassiveTreeDataTests.EveryConditionTheShippedTreeNamesIsInTheShippedCatalog` — знать о дублировании, хелпер не тянуть.
- (окружение, не диф) `ShippedTree()`/`ShippedTreePath()` написаны трояко (ручной подъём по каталогам / `SharedData.Catalog` / `PassiveWheelLayoutTests`); `TreeProviderStub` скопирован в 12 файлов. Кандидат на общий тестовый хелпер.
- (окружение, не диф) В `PassiveTreeServiceTests` правило «сид» написано двояко: старые места `Kind == PassiveNodeKind.Start` против канона `NodeKindRules.CostsPoint`.

## Из ревью P-11 (домен VillagerLifecycle, 2026-08-08)

- (minor→P-13/P-15) `INpcLifecycle.Stage : NpcLifeStage` — стена для роста мирного цикла: будущие состояния жителя придётся добавлять в общий енам и свитч сейва; потребители на деле спрашивают «лежит ли тело и как его писать». Решать одним проходом с переименованием `ResurrectDelay`.
- (nit) `BodyRiseTimer.Advance` расходится со старым кодом только на NaN из сейва (старый поднимал на первом тике, новый — никогда).
- (nit) `BodyRiseTimer` не различает «не запущен» и «истёк» (нероллённый `Advance(0)` → true) — оба владельца прикрыты гвардом по стадии, третий цикл должен знать.
- (nit) ~20 строк почти дословного повтора между циклами после выноса таймера — не обобщать, пока житель не отрастил свои состояния.

## Из ревью P-19 (правило втулки, 2026-08-08; accept with minors)

- (minor) `PassiveTreeValidationTests` — фикстура не читает `provider.Issues`: полузагруженное дерево (скипнутая битая строка) пройдёт валидацию зелёным. Одна строка assert на `Issues.Count`.
- (minor) Счёт сидов — арифметика, не распределение: «два сида одной стойки + втулка» теперь молчит (единственная прибавка молчания). Лечится счётом по стойке, как в `TreeValidator` инструмента (~3 строки + правка подстроки в двух тестах).
- (minor) Третья редакция «загрузить поставляемую разметку» в тест-проекте — просится общий хелпер `ShippedPassiveTree.Load()` по образцу `ShippedAbilityData`; схлопнет три частных `ShippedTree()`/`ShippedTreePath()`.
- (nit) `StartPointCount` теперь значит «стоечные сиды» — имя врёт, переименовать в `StanceSeedCount` (2 потребителя).
- (nit) Синтетические фикстуры завязаны на `Enum.GetValues<Stance>()` против константы 3 — четвёртая стойка уронит тесты с диагностикой в тест, а не в константу.
- (nit) `PassiveTreeValidationTests:156` — `Find(HubId)?` без `Assert.IsNotNull` (соседи делают).
- (nit) Шапка `NodeKindRules` говорит «Per-class content rules», а новые предикаты — по узлу.
- (архитектура, наблюдение) `Validate()` пропускает связность для всех `Start` — втулка без единого ребра прошла бы молча (связность держит инструмент); `CLAUDE.md` про «файлы-алиасы» редактора устарел — фактически `ProjectReference` на Core целиком.

## Из отчёта P-11 (домен VillagerLifecycle, 2026-08-08)

- Имя `INpcLifecycle.ResurrectDelay` для жителя врёт (это задержка восстановления, не воскрешения); переименование ломает `NpcWorldSaveParticipant` — сделать в P-15 вместе с сейвом жителя.
- `NpcLifecycle.cs` — старый TODO «нежить со временем восстаёт из Dormant» (не в объёме, дизайн-решения нет).
- `Battle\Source\Abilities\AbilityUpgrade.cs` — `AbilityUpgradeChanged` никогда не вызывается (CS0067), похоже на мёртвый член.

## Прочее

- `PassiveTreeServiceTests.StubSource.SourceChanged` — предсуществующий CS0067.
- Дефолты таймера жителя 30/180 с — плейсхолдер, балансом не подтверждён (всплывёт в P-13/балансном проходе).
