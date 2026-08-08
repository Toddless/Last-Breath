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

## Из верификации возврата P-01 (2026-08-08)

- (nit) `PassiveTreeServiceTests.TakingANodeNothingLinksTo_IsRefusedInsteadOfThrowing` — комментарий ссылается на «the case below», уехавший в `PassiveTreeDataTests`.
- (nit) `Reaches(line, parameter)` — второе написание фолда `Reached(line)`; первое выражается через второе одной строкой.
- (держать в уме) Непустота `NodeChain.Parameters` гарантирована неявной связью `FindChain` ↔ `OnlyFlatStrength` — при правках `OnlyFlatStrength` связь легко порвать молча.

## Из ревью P-12 (тесты домена жителя, 2026-08-08; accept with minors — minor #1/#2 закрываются до коммита)

- (nit) `VillagerLifecycleTests:59` — тавтологичное утверждение диапазона (проверяет стаб, не домен).
- (nit) `foreach`-кейсы без опознавания итерации в сообщениях (образец правильного — кейс сейв-стадий); попутно: `DynamicData` в проекте есть (`PassiveSkillStrengthTests`), обоснование «нет параметризации» неверно.
- (nit) Кейс «RestoreState(Alive) отвергнут» не показывает, что RestoreState вообще способен менять (ловится соседним кейсом — набор безопасен).
- (уточнение к P-15) Мусорная пара `ResurrectDelay`/`Elapsed` стоящего тела пишется в сейв, но `RisenKind` при загрузке её не читает — уборка, не баг.

## Из ревью P-15 (сейв жителя, 2026-08-08; accept with minors — №1/№3 закрываются до коммита)

- **(minor, РАЗВИЛКА ВЛАДЕЛЬЦА при сшивке P-16/P-18)** Легаси-запись `risen` для NPC, ставшего жителем, спавнит стоящего живого — категорию, которую участник сейва принципиально не восстанавливает («живых перекатывают точки»). С авторским именованным спавном это ВТОРАЯ копия уникального кузнеца + съеденный слот популяции. Сегодня невоспроизводимо. Вариант починки: решать до `Spawn` по дефиниции — но тогда участник читает `LifecycleKind` сам, вразрез с принципом «спросить у построенного цикла».
- (nit) Тестовый дублёр слил `RestoreAsRisen` и обработчик `ResurrectionReady` в один метод — нода различает их событием `NpcFactionChangedEvent`; кейс «восстановление молчит» потребует расщепления.
- (nit) `IsAlive` дублёра — публичный сеттер ради одной строки; просится `ZeroHealth()`.
- (nit) `RaiderNpcId = "undead_raider"` при `Fraction = Human` (имя читается как «уже нежить»); литерал `0f` при существующей `NoRisingBonus`.
- (nit) Третий приватный `FakeSpawner` в сборке (формы разные, сливать пока нечего — на случай четвёртого).

## Из ревью P-17 (активность Work, 2026-08-08; accept with minors)

- (minor, привязать к P-18) Опечатка в `point` расписания молчит: тег — свободная строка, промах клейма = NPC работает «дома». Закрыть аудит-тестом данных в духе `NpcDataAuditTests` («слот с активностью, требующей точку, называет тег из `SmartPointTags`»), когда в Npc.json появятся Work-слоты.
- (minor) Верхние стыки канала `point` не покрыты (NpcProvider.BuildScheduleSlot → WorldBrain.BuildRoutine → фабрика): один тест мозгом со слотом Work/TradeStall и ассертом на запрошенный тег.
- (nit) Тест деградации снимает весь контекст; частый сценарий «реестр есть, точки нет» — те же 4 строки на FakeRegistry.
- (nit) `FakeAgent` продублирован с `WorldBrainTests` — просится общий `FakeWorldAgent` в WorldTesting.
- (nit) Клип `Activity_Work` в анимациях NPC не проверялся (поза деградирует к Idle; Harvest уже использует ту же константу) — увидится в Godot-прогоне.

## Из ревью P-18 (данные Роналда, 2026-08-08; accept with minors → сюда)

- (minor, балансный проход) `Npc_Ronald.Strength: 7` выше ветерана-элиты (6) — вилка «крепче жителя, слабее ветерана» выдержана по всем осям, кроме атрибута; предложение: 5–6.
- (minor) `ru.po` без `Dlg_Opt_Leave`/`Dlg_Opt_Back` — общие опции диалогов сырые в русской локали (старый пробел, теперь с новым потребителем); две пустые записи.
- (minor) `NarrativeDataParseTests` не регистрирует `StartTradeActionFactory` → диалоги с торговлей (Роналд, Мерчант) выбрасываются парсом ЦЕЛИКОМ и тестами не проверяются вовсе; добавить фабрику + assert `Get("Npc_Ronald") != null`.
- (minor) Противоречие: док `NpcAuthoredData` («authored-запись оставляет `stances` пустым») vs `NpcDataAuditTests` (`Stances.Count > 0` безусловно). Править ТЕСТ: `Count > 0 || Authored?.Stance непуст`; после — убрать дубль стойки из записи Роналда.
- (nit) Арт Роналда: `NpcVisualProvider` на неизвестный id даёт плейсхолдер — отдельный шаг владельца (`NpcVisuals.tres`).

## Из ревью P-20 (опт-аут модификаторов, 2026-08-08; accept with minors → сюда)

- (minor) Док обещает «число, не состав — состав по-прежнему роллится», но мок-стенд состав не наблюдает (`GetModifier` → безымянные моки): дать мокам реальные id и тест «N всегда N, множество id варьирует».
- (nit) При `modifierCount: 0` всё равно материализуется копия каталога модификаторов (`GetAllModifiers().ToList()`) — `if (count == 0) return [];` до пула.
- (nit) Док `NpcDefinitionOverrides` («abilities and NPC modifiers re-roll») не знает о стабильном числе при `modifierCount` — дописать «перекатывается состав, число берётся с записи».

## Из ревью P-16 (авторский спавн, 2026-08-08; accept with minors — №1–3 закрыты до коммита)

- (nit) `AuthoredRarity_BeatsTheWeightedRoll` (старый тест про верхнеуровневое поле) коллизирует именем с новым `AuthoredRarity_BeatsTheTopLevelRarityField` — переименовать старый в `FixedRarityField_BeatsTheWeightedRoll`.
- (nit) Три копии парсера «опциональный enum» в `NpcProvider` (`ParseAuthoredStance`/`ParseAuthoredRarity`/`ParseFixedRarity`) — просится `EnumParser.ParseEnumOrNull<T>`.
- (nit) При заполненной секции старые поля (`rarity`, `stances`) не парсятся — опечатка в них не замечается; парсить оба и отдавать победу авторскому, либо жить с доком.
- (общий пункт) Валидация чисел в `Npc.json` отсутствует как класс (`level: 0`, `levelScaling`, радиусы); частично прикрыто `ShippedCatalogs_...` (`Level > 0` на поставляемых записях).

## Из ревью P-19 (правило втулки, 2026-08-08; accept with minors)

- (minor) `PassiveTreeValidationTests` — фикстура не читает `provider.Issues`: полузагруженное дерево (скипнутая битая строка) пройдёт валидацию зелёным. Одна строка assert на `Issues.Count`.
- (minor) Счёт сидов — арифметика, не распределение: «два сида одной стойки + втулка» теперь молчит (единственная прибавка молчания). Лечится счётом по стойке, как в `TreeValidator` инструмента (~3 строки + правка подстроки в двух тестах).
- (minor) Третья редакция «загрузить поставляемую разметку» в тест-проекте — просится общий хелпер `ShippedPassiveTree.Load()` по образцу `ShippedAbilityData`; схлопнет три частных `ShippedTree()`/`ShippedTreePath()`.
- (nit) `StartPointCount` теперь значит «стоечные сиды» — имя врёт, переименовать в `StanceSeedCount` (2 потребителя).
- (nit) Синтетические фикстуры завязаны на `Enum.GetValues<Stance>()` против константы 3 — четвёртая стойка уронит тесты с диагностикой в тест, а не в константу.
- (nit) `PassiveTreeValidationTests:156` — `Find(HubId)?` без `Assert.IsNotNull` (соседи делают).
- (nit) Шапка `NodeKindRules` говорит «Per-class content rules», а новые предикаты — по узлу.
- (архитектура, наблюдение) `Validate()` пропускает связность для всех `Start` — втулка без единого ребра прошла бы молча (связность держит инструмент); `CLAUDE.md` про «файлы-алиасы» редактора устарел — фактически `ProjectReference` на Core целиком.

## Из ревью P-13 (выбор цикла в данных, 2026-08-08; accept with minors)

- (minor) Дока поля `NpcData.Lifecycle` осталась в старой рамке «override правил нежити» — секция теперь ещё и точка выбора цикла.
- (minor) Тест поставляемого каталога (`NpcProviderTests:187`) может пройти вхолостую, когда все записи получат `kind` — нужен assert «записи без kind ещё есть»; фильтр дублирует решение `ParseEnumOrDefault` о пустой строке.
- (minor, не диф P-13) Отказ спавна в обеих копиях `NpcSpawnPoint` гасится в `GD.PrintErr` мимо Tracker — в файле лога отказа нет вовсе; правка: `Tracker.TrackException` + текущий PrintErr. Плюс `Main/World/NpcSpawnPoint.cs:242` (`ResolveFraction`) зовёт `CreateDefinition` вне try/catch.
- (nit) Нет кейса «kind явно = Undead» (4 строки; станет актуальным, если P-18 захочет явности).
- (nit) Дубль дефолтов 30/180 DTO↔домен (запинен тестом); соседняя пара нежити 60/600/1 такого пина не имеет.
- (nit) Дока `NpcData:85` «fields of the kind not chosen are simply not read» — читает провайдер, не читает цикл.
- (nit) Три чтения `Npc.json` с диска в одном методе теста.
- (→ в DoD P-14) Тест «`LifecycleKind.Villager` → построенный цикл является `IAliveRiseLifecycle` / `CanBeBurned == false`» — ловит перепутанную ветку switch одним ассертом.

## Из отчёта P-11 (домен VillagerLifecycle, 2026-08-08)

- Имя `INpcLifecycle.ResurrectDelay` для жителя врёт (это задержка восстановления, не воскрешения); переименование ломает `NpcWorldSaveParticipant` — сделать в P-15 вместе с сейвом жителя.
- `NpcLifecycle.cs` — старый TODO «нежить со временем восстаёт из Dormant» (не в объёме, дизайн-решения нет).
- `Battle\Source\Abilities\AbilityUpgrade.cs` — `AbilityUpgradeChanged` никогда не вызывается (CS0067), похоже на мёртвый член.

## Прочее

- `PassiveTreeServiceTests.StubSource.SourceChanged` — предсуществующий CS0067.
- Дефолты таймера жителя 30/180 с — плейсхолдер, балансом не подтверждён (всплывёт в P-13/балансном проходе).
