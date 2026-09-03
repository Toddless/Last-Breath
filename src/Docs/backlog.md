# Бэклог: minor / nit / «замечено, не исправлено»

## Из волны крафтовых описателей (2026-09-03; rework → accept)

Находки владельцу по данным:
- **Нет ключа локализации `Crafting_Mastery`** ни в `en.po`, ни в `ru.po` (есть только `Passive_Skill_Crafting_Mastery`); две строки `MasteryLevel` в `Recipes.json` — окно крафта (`CraftingWindow.StaticCardView`) рисует сырой ключ. Аудит локализации id требований не собирает.
- **4 рецепта чеканят несуществующие шаблоны**: `Body_/Helmet_/Boots_/Gloves_Iron_Bastion` — верстак возьмёт ресурсы и не выдаст ничего (пин `s_unansweredCraftingIds`).
- **`ItemEffects.json`: `Passive_Skill_Regeneration` записан дважды** (веса 100 и 20) — ролл берёт обе, суммарный вес 120 (пин `s_recordsWrittenTwice`).
- **`Items.json` мёртв и уносит квест** (карточка #251): парсер читает массив под `items`, файл — секции `quest`/`common`; единственная запись `Coal` — `Quest_Field_Of_Bones` просит и забирает его, больше `Coal` нигде не объявлен и в лут не входит ⇒ квест непроходим.
- `Recipes.json` `optionalResourceCategories: ["Essence"]` — тег ресурса, категория зовётся `Category_Essence`; поле никто не читает (размечено `NotARef`).

Код:
- (minor, карточка) `RecipeRequirementsData.id` размечен type-слепо: под `MasteryLevel` `id` — ключ локализации, не ссылка; обход тестов пропускает такие строки по значению `type`. Настоящая разметка — варианты по значению дискриминатора (`Discriminator(type)`), первое применение, требует поддержки в инспекторе.
- (minor) `CatalogDescriptorTests.cs` — `References` умеет варианты (`Shapes`), а `Unanswered` разрешает адрес через `Locate` по базовой записи: в день вариантной разметки требований обход упадёт «writes no 'id'». Стало: разрешение адреса по `Shapes` либо варианты из `References` убрать с причиной.
- (nit) комментарий над `s_settingsMapKeys`: `expRewards.modeFactors` при пропуске платит ПОЛНУЮ ставку (`GetValueOrDefault(mode, 1f)`), не ноль.
- (nit) `NamesNoRecord` (исключение `MasteryLevel`) зашит в общий ходок `ValuesAt` — предикатом параметром; `Part` рядом с `Section` — выразить один через другой; `Shapes` — третье место «запись плюс её формы» (`Records`, `CatalogSchemaBuilder.Shapes`); страж `addressed` глобальный — исчезновение ссылок одного каталога (Ornaments их не имеет) не заметит.
- (знать) `ItemEffects.id` → PassiveSkills+Effects не резолвится автоматом, пока цели не описаны (вручную: все 25 id есть в `PassiveCatalog.json`); `GrantKind.Modifier` каталога не называет.

## Из сухого прогона диалога (2026-09-03; rework → accept)

- (nit) `DialogueDryRunTests.cs` — `ADialogueThatWillNotParse_IsSaidRatherThanThrown` идёт тем же путём, что `AnNpcNobodyWroteADialogueFor` (провайдер отдаёт null); общая ветка `catch (Exception)` в `DialogueDryRun.Guarded` не покрыта. Стало: случай, где бросает сам шаг разговора (узел с `next` на несуществующий id), либо признать непокрытой.
- (nit) `NarrativeSandbox.cs:107-108` — `SeededQuestNote` добавляется всегда, даже без засеянных квестов. Стало: под `if (State.Quests.Count > 0)`.
- (знать) стадию засеянного квеста задать нельзя (всегда первая стадия записи); личная репутация — уровнем, не очками; атрибуты игрока в панель не выведены.
- (знать) `DefaultRandomNumberGenerator.RandFloat` — `(float)NextDouble()`: значения в 2⁻²⁵ от единицы дают ровно `1.0f` (~3·10⁻⁸); предсуществующий код, в игре недостижимо.
- (знать) панель Godot тестами не покрыта: отложенная перестройка (`_pending`/`_typed`/`_shown`) — рассуждением; первое живое включение за владельцем.

## Из формы столов лута (2026-09-03; rework → accept)

- (nit) `LootTablePanel.cs` — пикер редкости пишет безусловно: `item_selected` приходит и при выборе уже выбранного пункта, `Put` того же значения → лишняя перерисовка. Стало: ранний выход при `chosen == group.Rarity`.
- (nit, вопрос владельцу) xml-доки в `Tooling` (`LootTableForm`, `LootTablePanel`, `JsonTreeDocument`, `EditHistory`) — по 3–5 строк обоснований; CLAUDE.md просит одну-две строки. Системный стиль подсистемы, а не небрежность одной задачи — решение за Todd: принять стиль для тулинга или ужать волной.
- (знать) `JsonTreeDocument.Put` добавлен в `Tooling.Core/Json` вопреки исходным рамкам задачи — по замечанию ревью (одно правило «нет ключа → Insert, есть → SetValue» в одной точке), ведущий принял.
- (знать) `chance` у тира не существует (шансы тиров глобальные в `LootConfiguration`), `ChanceKey` раскладки null — тест зафиксировал; порядок позиций внутри тира не редактируется (игра выбирает равномерно).
- (знать) панель Godot тестами не покрыта: `_stale` по адресу (`Within`), холостой `FocusExited`, попап переноса — рассуждением; первое живое включение за владельцем.

## Из волны описателей каталогов-настроек (2026-09-03; accept with minors)

- (minor) `ConfigCatalogDescriptors.cs` — Recovery описан через публичный двойник `RecoveryConfig`, а игра парсит приватный `RecoveryConfigProvider.RecoveryData`; охраны от расхождения нет (поле, добавленное только в двойник, инструмент нарисует, игра молча не прочтёт). Стало: провайдер десериализует прямо в `RecoveryConfig` и двойник удаляется, либо тест сравнивает json-имена/виды обоих типов через `SchemaReflector`.
- (minor) `CatalogDescriptorTests.cs` — пути CombatRules голыми литералами (`"controlResistance.hardControlStatuses"`, `"multicast.stages.cap"`), у соседей волны — константы описателей через `PathSeparator`. Стало: константы полей в `CombatRulesCatalogDescriptor`.
- (minor) `CatalogDescriptorTests.cs` — новый `Under(JToken, string)` обобщает существующий `Keys(JToken, string)`, две ручные рекурсии одного обхода. Стало: `Keys` поверх `Under`.
- (nit) `SingleObjectDescriptor.cs` — обоснование решения в xml-доке (`<para>` про «дюжину копий»); имена файлов литералами там, где равны имени каталога, тогда как `NpcCatalogDescriptor` пишет `FileName = DataCatalog.Npc` — две конвенции в одной папке.
- (nit) `ParameterFormatsData.cs` тянет `using Core.Localization` ради `ParameterUnit` — слой Data смотрит в Localization.
- (nit) нет теста «папка Single-каталога держит ровно один json» — инструмент второй файл не увидит. Стало: `Assert.AreEqual(1, ShippedFiles(catalog).Count)` в `DataRow`-тесте.
- (знать) `PlayerStats` не описан: провайдер парсит `Dictionary<string, Dictionary<string,float>>`, DTO нет — нужен DTO (правка парсера). `CraftingMastery`, `NpcSpawnRolls` по файлу тоже «один объект настроек» — следующая волна.

## Из редактора условий и действий (2026-09-03; rework → accept)

- (знать) `InspectorPanel` без тестов — согласованность пикера полей и блока словаря держится общим `TypedRecords.Blank`; порядок ключей внутри `Any`-полей при сохранении — рассуждением по `SchemaKeyOrder`, теста канонической записи Dialogues/Quests нет.
- (знать) пикер типа не фильтрует по уместности в месте (словарь плоский) — по замыслу.

## Из нормализации диапазонов экипировки (2026-09-03; accept)

- (minor) `src/Testing/BattleSystemTests/EquipItemDataFormAuditTests.cs:57-58` — `Bounded` принимает `Integer or Float` для обеих записей, а `LevelRangeDataConverter.TryReadBound` берёт только `Integer`: `{"min": 0.0, "max": 2}` пройдёт аудит и выкинет предмет на загрузке. Стало: род границы следует за записью (пара «запись → допустимый род» рядом с `s_rangeRecords`).
- (nit) имена границ `min`/`max` в четырёх местах (два конвертера, два теста); `<para>` теста на четыре строки.
- (знать) в `ModifierPools` диапазоны и так объектные; аудит сторожит только `EquipItems`.

## Из отказа провайдера на провальный исход у canFail:false (2026-09-03; accept)

- (nit) `src/Core/Narrative/Quests/QuestLogService.cs:348-349` — `if (!Fail(...))` печатает причину, которую `Fail` не сообщает (четыре повода вернуть false); честнее `if (!quest.CanFail) TrackError` перед `Fail`; формулировка с повтором «outcome … failing outcome» и двумя двоеточиями — привести к форме соседей.
- (nit, док) `src/Core/Data/QuestData/QuestsData.cs:79-80` — док DTO у `fails` не говорит про требование `canFail: true` (сказано только на рантайм-записи).
- (знать) ветка `TrackError` в `ReachOutcome` и клауза `OutcomeId == null` в `EvaluateQuest` без покрытия после замены теста (состояние недостижимо через провайдер).


## Из подключения локализации к хосту нарратива (2026-09-03; accept with minors) — minor'ы раздела «Из скелета NarrativeEditor» (двойной Refresh, (not a list)/outcome, опция без key, failNext, заметки чужих каталогов, страж инспектора, `_tree`, перевод из TranslationServer) закрыты

- (minor) `src/Tooling/NarrativeEditor/Source/View/NarrativeRoot.cs:264,273-278` — `Same(null, null)` = true → `Rebuild(null)` не зовётся никогда: при пустых каталогах инспектор — голый прямоугольник вместо «nothing selected»; посеять `_inspector.Rebuild(null)` в `BuildBody`.
- (minor) `NarrativeRoot.cs:268` — `Rebuild` без третьего аргумента-соседа: первый ключ записи ложится в конец .po, тогда как DataEditor кладёт к соседям; отдать `CurrentId` предыдущей записи как `EditorRoot.Neighbour`.
- (minor, карточка) `GameNarrative.cs:27,84` — префикс ноты `"{0}: "` продублирован из приватного `CatalogWorkspace.Notes.Named`; дать `CatalogWorkspace.Report` структурный вид (каталог → нота) или открыть формат.
- (minor, карточка, дубли по ограничению) `NarrativeRoot.LoadTexts` ≡ `EditorRoot.LoadTexts` (≈25 строк), `OutlineTree.Translation` ≡ `InspectorPanel.Translation`; `LoadTexts` принадлежит `ToolShell`, «что ключ говорит сейчас» — `LocalizedTexts`.
- (nit) `ToolShell.ReportIssues(workspace, also, report)` — «добавить» и «заменить» в одном методе; `protected virtual Reported(workspace)`; `Outline.cs:289` — кривой `speechCheck` (не объект) молчит — дописать `· not a record`.

## Из статического словаря нарратива (2026-09-03; accept with minors)

- (minor, док) `src/Tooling/NarrativeEditor/Source/App/GameNarrative.cs:19-21` — ремарка утверждает, что словарь вне игры перечислить нельзя; теперь `NarrativeSchemas.Conditions()/Actions()` читают его без игры — переписать одной строкой (условия/действия пока рисуются сырым json).
- (minor) `src/Core/Narrative/Conditions/CompositeConditions.cs:26` — `ConditionsKey` расширен до `protected` без нужды; вернуть `private`.
- (minor, порядок) `NarrativeVocabulary.cs:14-15` обещает порядок регистрации игры, но тест сравнивает множества, потому что `NarrativeTestFactories.Actions` ставит `GrantTreePoints` раньше `StartTrade`/`SpawnNpc` (игра — после); поправить тестовое зеркало под `GameServiceProvider:200-208` и сравнивать `AreEqual`.
- (minor, дубль ×3) `NarrativeVocabularyTests.cs:136-138` — рефлексивный обход фабрик повторяет `NarrativeConditionSchemaTests:78-79`/`ActionSchemaTests:105-106`; `NarrativeTestFactories.FactoriesInCore<T>()`.
- (nit) `DeclaredSpecs` делает `yield break` после `Spec` (тип с обоими `Spec`/`SpecFor` проверится наполовину); xml-док реестра на 8 строк; отчёт занизил счётчик (22, не 21).

## Из локализации в инспекторе (2026-09-03; rework → accept)

- (карточка) undo правки id не откатывает переименование ключей .po — теперь только предупреждение в статусе; нужен либо шаг .po в общую историю, либо журнал переименований, отматываемый вместе с json.
- (функция) Ctrl+Z/Ctrl+Y не ходят по истории `PoDocument` (`ToolShell.Stepped` типизирован `JsonTreeDocument`).
- (граница) ключ ПЕРВОЙ реплики диалога якоря не имеет → уходит в конец файла; вторая и далее садятся под первой.
- (по решению) удаление записи оставляет ключи сиротами — снимает аудит ключей-сирот по всему корню данных (заведено: #249 расширить).
- (знать) плюральные записи не редактируются (отказ в статусе).

## Из скелета NarrativeEditor и общей оболочки ToolShell (2026-09-03; accept with minors)

- (minor) `src/Tooling/NarrativeEditor/Source/View/NarrativeRoot.cs:203-204` — `ShowRecord` строит аутлайн и открывает элемент, а следующий `Refresh()` → `Redraw()` делает то же ещё раз; оставить один `Refresh()`.
- (minor, граница) `src/Tooling/Tooling.Core/Narrative/Outline.cs:170-201,266` — `Rows`/`Branch` не различают «ключа нет» и «ключ есть, но не список» (`nodes (0)` при `"nodes": {}`), `outcome` не объектом не даёт строки; подпись `(not a list)` и тесты.
- (minor) `Outline.cs:242-257` — опция без `key` подписывается `—`, хотя несёт `id`; `Or(key)` → `Identity(schema, token, index)`.
- (minor, структура) там же — `speechCheck.failNext` (вторая ветка беседы, в данных `Flatter → FlatterGood/FlatterBad`) в аутлайне не виден; строка опции называет оба маршрута либо `speechCheck` — дочерняя строка со своим указателем.
- (minor) `NarrativeEditor/Source/App/GameNarrative.cs:33` + `NarrativeRoot.cs:111,114` — чтение всего корня оправдано (ReferenceIndex), но заметки ВСЕХ каталогов уезжают в стартовый диалог и счётчик нарративщика; фильтровать по `GameNarrative.Catalogs`.
- (карточка/задача) `LastBreath.Descriptors/NarrativeSchemas.cs:20-25` — словарь условий/действий берётся из ЭКЗЕМПЛЯРОВ фабрик, которых вне игры нет (единственный список без игры — тестовый `NarrativeTestFactories` на моках); тулу нужен статический реестр `NarrativeRecordSpec` в Core, фабрика читает свой спек из него — задача P1.9.
- (nit) `NarrativeRoot.cs:224` страж инспектора не покрывает `_shown == null && standing == null`; `:35` `_tree` — поле только для `BuildBody`; `OutlineKind` без потребителя; `ToolShell.cs:95-104` сеттер `Texts` не отписывает прежнее; `ToolShell.cs:24-27,79-121` порядок членов (protected const перед private, чередование); `.uid` для `ToolShell.cs`, `GameNarrative.cs`, `NarrativeRoot.cs`, `OutlineTree.cs` появятся при первом импорте — закоммитить вместе.
- (знать) перевод в строках аутлайна — из `TranslationServer` на старте; когда блок «Текст» приедет в NarrativeEditor, правка .po не отразится в дереве без чтения из `LocalizedTexts`.

## Из единого источника ссылок на предмет (2026-09-03; accept with minors) — пункт «дубль ×3» предыдущего раздела закрыт

- (minor) `src/Core/Narrative/Conditions/HasItemCondition.cs:19-21,32-39` — три константы-алиаса и рукописная копия `Require`/`Amount` только из-за слова «action» в тексте трекера `ItemReference.Require`; убрать род записи из сообщения и свернуть `Create` на `ItemReference.Require/Amount`.
- (minor) `src/Core/Data/QuestData/QuestsData.cs:126,135` — разметка награды пишет `"itemId"`, `"amount"`, `= 1` литералами при наличии `ItemReference.Key/AmountKey/DefaultAmount`.
- (nit) `NarrativeSchemas.cs:64-65` — тернарник с `Whole` ничем не отличается от `new ReferenceTarget(c, s)`; `CatalogDescriptorTests.cs:1589-1609` — `NamesAHandOut` копия `NamesADrop` (общий `Names(field, what, where, emptyNote)`); `ItemReference.cs:46` — приватный `s_targets` между публичными; xml-док на 8 строк.
- (знать) `Items`, `Recipes`, `Ornaments` в `NotYetDescribed` — пикер `itemId` по-прежнему «непроверяем», сужение видимого эффекта не даёт, пока эти каталоги не описаны.

## Из секционных ссылок (2026-09-03; rework → accept) и описателей Dialogues/Quests (accept with minors)

- (minor, дубль ×3) список четырёх каталогов предмета живёт в `QuestsData.cs:126-129`, `GiveItemAction.cs:42-43` (`ItemIdParameter`) и `CatalogDescriptorTests.cs` (`s_handedOutCatalogs`); плюс нарративные `GiveItem`/`HasItem` и `rewards.items[].itemId` указывают в `Resources` целиком, а лут уже сужен до `upgradeResources`+`craftingResources`. Одна задача: `NarrativeParameterSpec.Catalogs` несёт секцию, `ItemIdParameter` — источник правды, разметка наград и тест сверяются с ним.
- (minor, тест) `CatalogDescriptorTests.cs:1982-1984` — пин награды квеста через `Points` по одному каталогу; форма `NamesADrop` (набор целиком + `AllowEmpty`).
- (minor, тест) `CatalogDescriptorTests.cs:2045` — квестовый обход по шипнутым файлам холостой (0 маршрутов); дешевле и шире — пин «провайдер не уронил ни одной записи» (сколько записей в файлах, столько в `_dialogues`/`_quests` после `Apply`).
- (nit) новые константы/таблицы блока Dialogues/Quests приписаны после методов (поднять в поле-регион после слияния); `typeof(Core.Narrative.Dialogues.DialogueSpeaker)` полностью квалифицированы; `s_questReferences`/`s_questChoices` литералами; `giverNpcId` без `AllowEmpty` при терпимом провайдере (`GiverNpcId` никто не читает) — зафиксировать строгость как решение; `<remarks>` описателей на 6–8 строк; отчёт занизил счётчик (+10, не +8).
- (знать) `LocalizationAuditTests.CollectDataIds` квесты и диалоги не собирает — их локализация ничем не аудируется; `_Outcome_<id>` объявлен в доке, но UI не читает.
- (знать, контракт) секция, названная для каталога без описателя, на сборке не проверяется; ключи карт (`DictionaryKey`) сузить до секции нечем.

## Из описателя Abilities (2026-09-03; accept with minors)

- (minor, комментарий врёт) `src/Tooling/LastBreath.Descriptors/AbilitiesCatalogDescriptor.cs:61` (+ `CatalogDescriptorTests.cs:1103,1125,1468`) — комментарий утверждает порядок форм «как читает правило, от узкого», а `AugmentFit.CheckBinding` читает `fitsAnyAbility` → `abilityId` → теги; безопасно только благодаря `ContradictoryDeclaration`. Сказать факт: несущий только `tags` — последним, порядок первых двух выбран для инспектора.
- (КОНТРАКТ, второй случай) `AbilitiesCatalogDescriptor.cs:72-77` — обе секции с `IdField = "id"` → `ReferenceIndex` для каталога Abilities отдаёт 25 способностей ∪ 97 аугментов: пикер `abilityId`/`abilities[]` у NPC предлагает `Augment_*` и считает их валидными. Первый случай — `MaterialData.categoryId` → секция `materialCategories`. Решение ведущего: завести секционно-ограниченную ссылку `CatalogRef(catalog, section)` в разметке, рефлекторе, `ReferenceIndex` и пикере — отдельной задачей.
- (minor, док) `AugmentForms.cs:30-34` — форма «универсальный» надевается наличием ключа, а игра читает значение: переключение в форму впишет `"fitsAnyAbility": false`, и запись привяжется тегами; строка в xml-доке формы.
- (nit) `Shape(key, record)` продублирован в `AbilitiesCatalogDescriptor.cs:83` и `LootTablesCatalogDescriptor.cs:72` — общий `internal static` в адаптере; `tier` без `[Range]` (максимум живёт в `NodeKindRules` без именованной константы).
- (ДАННЫЕ → владельцу) `BaseAbilityData.json`: enum'ы секции `abilities` записаны числами (`stance` 0/1/2, `costType` 1, `targetType` 0/2), а `minRarity`/`maxRarity` у аугментов — именами; первый сейв из тула перепишет числа именами (десериализатор читает оба вида — проверено). Все 97 аугментов пишут `tags`, 45 — рядом с более сильным ключом (31 из них `"tags": []`).
- (контракт, вход для решения) три оси аугмента (форма значений, `effectId`/`effectPool`, поведение) описаны плоско — `VariantSet` одноосевой; `tags`/`grantsTags` и ключи `upgradeProperties`/`rarityLadder` (имена `AbilityParameter` — класс констант, не enum) размечать нечем.

## Из полировки хоста 2 (2026-09-03; accept with minors) — предыдущий раздел «Из создания/дубля/удаления записей» закрыт этой задачей, кроме отмеченного ниже

- (minor) `src/Tooling/DataEditor/Source/View/EditorRoot.cs:550` — обнуление `_touched` в `ShowCatalog` закрыло undo чужого каталога, но при возврате в каталог после удаления единственной записи `Stepped` = null: удаление неотменяемо из интерфейса, Ctrl+S его запишет. Стало: `Stepped => _record?.File.Document ?? (Owns(_touched) ? _touched : null)`, `Owns(doc) => _catalog?.Files.Any(f => ReferenceEquals(f.Document, doc)) == true`.
- (minor, домен) `src/Tooling/Tooling.Core/Catalogs/CatalogEditing.cs:93` — `RemoveRecord` формы `Single` не проверяет (в отличие от `AddRecord`/`Beside`): у `Single` с именованной секцией указатель `/key`, `Remove` унесёт всё содержимое; ворота только погашенная кнопка. Стало: первой строкой `if (view.Schema.Shape == RootShape.Single) return Refused(Notes.OneRecordOnly);`.
- (nit) `EditorRoot.cs:783-785` комментарий про «имя файла» вместо «id при чтении»; `:547-551` `_recordIndex` не сбрасывается при смене каталога (спасает кламп); `:705-708` посев `_fileBox.Text` в `FillFilePicker` затирается сменой секции (недостижимо сегодня); `:758` имя с расширением даёт `X.json.json` — срезать `FileExtension`; README раздел «Границы» — висячий огрызок «exe (плагин .NET…)» и «нет экспорта» против описанного запуска exe.

## Из ветвления квестов (2026-09-03; accept with minors) — два пункта ждут владельца

- (ВОПРОС владельцу) `src/Core/Narrative/Quests/QuestLogService.cs:296-299,341-352` — `canFail: false` + исход `fails: true` = зомби-квест: `OutcomeId` выставлен, `Fail` отказал, статус навсегда `Active`, журнал не узнаёт о конце. Варианты: провайдер отвергает такой квест на загрузке (дефект данных) либо `ReachOutcome` при отказе `Fail` ставит `ReadyToTurnIn`.
- (ВОПРОС владельцу, имя) `decisions.md` называет поле перехода `toStage`, DTO читает `to` (`QuestsData.cs:62`); данных с переходами ещё нет — выровнять в любую сторону одним словом.
- (minor, дубль правила) `QuestProvider.cs:193-200` vs `QuestLogService.cs:327-337` — «нет переходов = следующая по списку, исход = никуда» написано дважды; на их совпадении держится ацикличность. Одна точка на `QuestDefinition` (`Successors(stage)`), ручной `StageIndex` в сервисе исчезает.
- (minor, сейв) `QuestLogSaveParticipant.cs:36-44,69-92` — гейт миграции по пустому `stageId`, а не по `savedVersion < 3`; v3 тоже пишет пустой `stageId` у Declined. Единственный гейт — версия.
- (minor, авторинг) `QuestsData.cs:60-66` — безусловный переход не последним молча съедает следующие маршруты; `TrackInfo` в `ParseTransitions`.
- (nit) `Abandon` не чистит `OutcomeId`; `CurrentStage` резолвится дважды за итерацию; тесты: повторное принятие `Repeatable` после исхода, `Abandon`/`Decline` после исхода, сейв с пропавшим исходом, переход НАЗАД (легален только если у стадий на пути есть явные переходы в обход источника — иначе «reachable from itself»; зафиксировать строкой в доке).

## Из описателя NpcBehaviors (2026-09-03; accept with minors)

- (minor, дубль) `src/Testing/BattleSystemTests/CatalogDescriptorTests.cs:479-499` — `TheNpcBehaviorsSchemaRanksEveryKeyTheShippedFileWrites` посимвольная копия Npc-теста и оба перекрывают хелпер `UnknownKeys`; общий `RanksEveryKey(catalog, file)` либо `Assert.AreEqual(0, UnknownKeys(...).Count)`.
- (nit) `:490,494,498` — сообщения называют файл без расширения (`FileName + CatalogWorkspace.FileExtension`).

## Из создания/дубля/удаления записей и пикера ссылок (2026-09-03; accept with minors после доработки)

- (уточнение отчёта) тест `RemoveRecord_LeavesTheStepOnTheFileTheRecordWasTakenFrom` пинит доменную посылку, а не хостовую логику `_touched` — регрессия хоста тестами не ловится.
- (nit) `InspectorPanel.cs:330-335` док `Box` противоречит `ReferenceBox`; `Reread` на каждый `Refresh` и полный обход индекса ссылок на символ (`ReferenceIndex.cs:177`) — инвалидация по каталогу; «имя файла без расширения» тремя выражениями — `CatalogFile.BaseName`; после undo удаления запись не выбирается; xml-доки абзацами (`CatalogEditing.cs:11-14,34-41,241-244,257-261`, `CatalogView.cs:28-31`, `EditorRoot.cs:152-155,873-876`); `CatalogEditingTests.cs:408` `Mythic*Modifiers` — на POSIX не упадёт.

## Из разворота зависимости, шаг 3: спеки нарратива в Core (2026-09-02; accept)

- (nit) `src/Core/Narrative/NarrativeParameterSpec.cs:15` — вид назван `Choice`, не `Enum` (коллизия с хелпером `Enum<T>`), в отчёте отступление не названо; `:35` — `JsonName` без гуарда пустой строки (держится хелпером и пином ключей); `:46,49` — `Choices`/`Catalogs` как `IReadOnlyList<string>` сравниваются по ссылке, равенство record не контентное (латентно). `LastBreath.Descriptors/NarrativeSchemas.cs:47-56` — `Default` протекает на составные виды (рефлектор обнуляет для Array/Object); `:20,24,27` — нет `ArgumentNullException.ThrowIfNull`. `NarrativeSchemaAdapterTests.cs:118` — декоративный `IsNotNull`.

## Из правки коллекций в инспекторе (2026-09-02; accept)

- (minor) `src/Tooling/DataEditor/Source/View/InspectorPanel.cs:810` — подсказка поля свободного ключа берётся из схемы ЗНАЧЕНИЯ (`Hint(item)` при `field.Key == null`) — рекламирует каталоги значения как требование к ключу; передавать готовую подсказку, при отсутствии `Key` — `KeyPlaceholder`.
- (minor) `InspectorPanel.cs:904-907` — проверка границ выбора пикера написана дважды (`Picked` для карты и заново в `NewField`); общий `Chosen(picker, fields)`.
- (nit) «×» строится тремя способами (`FieldGesture` мимо `Gesture`); тело `Gesture` переписано в `TextSubmitted` — общий `Attempt(build, change)`; отказ смены формы оставляет пикер на невыбранном; `EveryKeyHint` врёт при пустом enum; ключ-носитель формы (различение по наличию ключа) удаляется как обычное поле — гасить «×» с причиной; `RecordTemplates.cs:132` недостижимая ветка молча даёт запись без формы; `/modifiers/0` литералом в двух тестах.

## Из разворота зависимости, шаг 1: разметка в Core, чтение по имени (2026-09-02; accept with minors)

- (minor) `src/Tooling/Tooling.Core/Schema/SchemaReflector.cs:473` — «свойство есть, но держит null» молчит для всех; законно только у `DictionaryKey.EnumType`/`Catalog`. `CatalogRef.Catalog = null`, `Discriminator.Field = null`, `LocalizedKey.Suffix = null` — нота `MarkupUnreadable`; `DictionaryKey` с обоими null — нота. Стало: `Reads<T>(…, bool mayHoldNothing)`, `true` только в двух чтениях `DictionaryKey`.
- (minor) `SchemaReflector.cs:414,423` — `Any(...)`/`FirstOrDefault(...)` с побочной нотой останавливаются на первом — ноты по второму/третьему одноимённому атрибуту теряются по порядку; материализовать (`Count(...) > 0`, `[.. Select]`).
- (minor) `SchemaReflector.cs:407` — `GetCustomAttributes(inherit: false)` против прежнего `inherit: true` и соседей `Converted`/`IsRequired`; в `Core/Data` нет `virtual`/`override`, но правило внутри рефлектора стало двумя. Либо `inherit: true`, либо двойник с `override` и нота.
- (minor, процедура) `MarkupNames.cs:3-9`, `SchemaMarkup.cs:4-6`, обе `Convention()` — переименование трогает четыре места (атрибут/свойство, константа `MarkupNames`, две таблицы `Convention()`), нигде не записано; одна строка в xml-доке с обеих сторон.
- (nit) `SchemaReflector.cs:461-462` — `GetProperty(property)` бросит `AmbiguousMatchException` на `new`-свойстве, `GetValue` — что угодно из чужого геттера; `GetProperties(Public|Instance)` + `try/catch` с нотой. `SchemaMarkup.cs:1` — папка `Markup`, namespace `Core.Data.Schema`; пин игры фильтрует по namespace.

## Из описателей ModifierPools и Resources (2026-09-02; accept with minors)

- (minor) `src/Core/Data/CraftingData/MaterialData.cs:14` — `Id` (`Material_*`, 41 запись) размечен `NotARef`, но поле мёртвое: парсер контекст берёт из `craftingData.Id`, ключей `Material_*` в .po нет; инспектор нарисует живое текстовое поле. Стало: `[Hidden]` рядом либо карточка на снос поля из DTO и 41 записи.
- (minor, сказать вслух) `ModifierPoolsCatalogDescriptor.cs:9-15` — один `ItemModifier` обслуживает два каталога с противоположной политикой аффикса (`Required` в пулах, `Forbidden` в экипировке), схема в обоих говорит «необязателен»; контракт не выражает — назвать в `<remarks>`.
- (minor, контракт) `MaterialData.cs:15` — `CatalogRef(Resources)` на `categoryId` честен на уровне каталога, но парсер резолвит только против секции `materialCategories` (6 валидных из 73 id) — первая ссылка в ОДНУ секцию многосекционного каталога; карточка на `CatalogRef(catalog, section)`.
- (minor, пин формы данных) `CatalogDescriptorTests.cs:284-285,996-1007` — `s_unknownKeysInsideComposites` через `AreEquivalent` для Resources (3 композита, 6 частей) роняет тест схемы при штатной правке данных; для малой выборки — `IsSubsetOf`.
- (minor) `ResourcesCatalogDescriptor.cs:34,38` — `CategoryField`/`OverlayField` не используются в `Describe`, единственный потребитель — тест; перенести к `PartsField`/`GrantField`/`AffixField` в тест.
- (nit) `Choice(field, members)` печатает `field.JsonName` = `""` у элемента массива — параметр `path`; `NamesItsPartsInAConstructor = "cannot be built"` совпадает и с `Shapeless`/`NoSample` — `"cannot be built without arguments"`; док `UpgradeResourceData.cs:5-6` дублирует комментарий парсера.
- (ДАННЫЕ → владельцу) `CraftingResources.json`: плоский `modifiers` пуст у всех 6 категорий и 41 ресурса (весь пул в `byCategory`); `Category_Essence` без строк вовсе; `MaterialData.Id` не читается; `CLAUDE.md:158` порядок ключей секции `Weapon/Armor/Jewellery` — в данных `Armor/Weapon/Jewellery` в разном порядке (на парс не влияет).

## Из схем действий нарратива (2026-09-02; accept with minors)

- (minor, ловушка перегрузки) `src/Core/Narrative/NarrativeParameterSchema.cs:33` — `Text(name, string fallback)` рядом с `Text(name, bool required, params string[] catalogs)`: забытый `required:` биндится в fallback и даёт необязательную строку с «дефолтом» = имя каталога. Переименовать в `TextOr`/`OptionalText`.
- (minor, дубль) `GiveItemAction.cs:21`, `TakeItemAction.cs:18` — поле `amount` собирается дважды; `ItemIdParameter.AmountField`.
- (minor, дубль между половинами; нужна санкция на правку условий) `GiveItemAction.cs:37` vs `Conditions/HasItemCondition.cs:18-25` — ссылка на предмет (`itemId`/`amount`/1/четыре каталога) существует дважды; перенести `ItemIdParameter` в `Core/Narrative` рядом с `NarrativeParameterSchema` и указать на него условие.
- (minor, для тула) ограничения, невыразимые через `Required` (`delta ≠ 0`, `amount > 0`, `pointId` — точка сцены, `modifiers` отсутствие ≠ пустой список) живут только в xml-доке C#; передавать как `FieldSchema.Documentation` на поле — то же для схем условий.
- (nit) `s_unwatchableDefaults` несёт два смысла (дефолта нет vs не подсмотреть) — два списка; классы-действия (`SpawnNpcAction.cs`, `PublishDeedAction.cs`) пишут имя типа литералом в 7 сообщениях; док `QuestIdParameter` «four conditions» устарел; `ItemIdParameter` лежит в `GiveItemAction.cs`, а читает его и `TakeItem` — отдельный файл или `ItemActions.cs`; у 6 фабрик из 13 собственных `*Key`-констант нет, прямое направление пина пусто (дрейф невозможен конструктивно); нет нарративного аналога пина «*Id — ссылка либо отказ» (`FreeText` введён ради него).

## Из описателя EquipItems (2026-09-02; accept with minors) — данные владельцу отдельно в отчёте

- (minor) `src/Testing/BattleSystemTests/CatalogDescriptorTests.cs:650-651` — `ShippedFiles` перечисляет `*.json` без `SearchOption.AllDirectories`, а игра и тул читают рекурсивно; стало: `[.. CatalogWorkspace.FilePaths(SharedData.Catalog(catalog))]` (заодно уходят локальные `JsonFiles`/`JsonExtension`).
- (nit, контракт) `src/Core/Data/EquipData/GrantData.cs:14-21` — `grants[].id` размечен двумя каталогами, но у `GrantKind.Modifier` id не именует запись; лечение — `[Discriminator("kind")]` + три формы в описателе, когда появится валидатор.
- (nit) `CatalogDescriptorTests.cs:150,158-161` — док `s_equipItemChoices` обещает «каждое поле, которое парсер превращает в enum», а `affix` в этом каталоге парсер отвергает (`AffixPolicy.Forbidden`); `:185-188` — `CriticalDamage` читается регистронезависимо и живо, `effectId` выбрасывается — развести формулировки; в файле две конвенции allowlist нот (точные строки у Npc/LootTables, пары у EquipItems) — перевести первые две на `Unexpected`.
- (ДАННЫЕ → владельцу) `Weapon.json` — ключ `CriticalDamage` вместо `criticalDamage` в 8 записях (читается регистронезависимо, тул покажет неизвестным); `Body.json:253,296,330` (файл переименован 2026-09-02) — мёртвый `"effectId": ""`; `maxStackSize` в DTO и в 48 записях, но `ParseEquipItems` его не читает; канонический сейв переставит 132 записи (`basePrice` с конца на место по DTO).
- (данные, предсуществующее) теги экипировки в поставке (`Body`, `Boots`, `Equipment`, `Gloves`, `Helmet`, `Ring`) не входят в `TagConstants.AllTags` (там нижний регистр, `Ordinal`) — `HasTag` на них отвечает `false`.
- (контракт, вход для решения) скалярная форма диапазонов `ValueRange`/`LevelRange` («число ИЛИ {min,max}») вариантами не выражается: по девяти файлам 108 скаляров (почти все `updateLevel`, 104/105) против 202 объектов; расширение контракта «скаляр как вариант» затронет каждую запись каталога.
- (знать) `ItemModifier.parts` рекурсивен — в схеме массив объектов без записи (нота цикла), ключи внутри не ранжируются; `ModifierPools`/`Resources` на том же DTO столкнутся при своих описателях.

## Из полировки хоста DataEditor (2026-09-02; accept with minors)

- (minor) `src/Tooling/DataEditor/Source/View/InspectorPanel.cs:210,214,222` — `Documentation` навешивается только на скалярные строки; `Section(parent, name, documentation)` + общий `Described(Label, string?)` для заголовков Object/Array/Dictionary.
- (minor) `src/Tooling/Tooling.Core/Catalogs/CatalogWorkspace.cs:100` — нота из `broken.Message` без имени типа исключения (после `catch (Exception)` `NullReferenceException` чужого описателя даёт бессмысленную строку): `"{0}: the schema could not be built: {1}: {2}"` с `broken.GetType().Name`.
- (minor, экспорт)  — когда редактор дерева переподключится к , отобразить библиотеку ,  (как сделано в ).
- (nit) `JsonScalars.Written`: `Date`/`Guid`/`TimeSpan` вернутся в кавычках — ветвить по `value is JValue { Value: string text }`; тест культуры — `[DoNotParallelize]` и сообщение к `AreNotEqual`; `ScrollVertical` диалога сбрасывать перед показом; `InspectorPanel.cs:196` `Wears` сравнивает дискриминатор через `ToString()` — через `JsonScalars.Written`; xml-доки на 3–4 строки в `GameCatalogs.cs:15-17`, `EditorRoot.cs:27-29`, `CatalogWorkspace.cs:86-90`, `InspectorPanel.cs:259-261`.

## Из описателя LootTables (2026-09-02; accept with minors)

- (minor) `src/Core/Data/Schema/LootTablesCatalogDescriptor.cs:96-109` — `Meaning` копирует с подменяющего поля только `Kind`/`EnumValues`/`RefCatalogs`/`RefusedAsReference`, остальное (`AllowEmpty`, `Range`, `Item`/`Key`) молча теряется. Стало: `names with { Required = field.Required, Default = field.Default, Hidden = field.Hidden }`.
- (minor, имя) там же `:29-37` — `IdField` в описателе означает ключ формы позиции, а не id записи (в Npc-описателе — id); переименовать в `ByIdKey`/`ByGroupKey`.
- (minor, хрупко) `CatalogDescriptorTests.cs:62-68` — allowlist нот пинит точные предложения из `SchemaReflector.Notes`; сравнивать пары «о чём / опорное слово» через `Contains` + `AreEqual(4, notes.Count)`.
- (minor, размен) `CatalogDescriptorTests.cs:198-215` — в Npc-тесте пин идемпотентности `once == twice` убран, а не дополнен `DeepEquals`; вернуть обе строки (`DeepEquals` к порядку нечувствителен).
- (minor, ДАННЫЕ → владельцу) `src/SharedData/LootTables/LootTables.json:1016` — ключ `npc_id` в секции `individual` не существует ни в одном `Npc.json` (плейсхолдер); тул нарисует битую ссылку; `LootTablesAuditTests` ключи таблиц не проверяет — снести запись либо дописать в аудит проверку ключей `individual` против Npc и `fractions`/`types` против enum'ов.
- (nit) `FileName = "LootTables"` литералом против `FileName = DataCatalog.Npc` у соседа — выровнять правило; цикл по секциям проверяет один и тот же объект `tiers`; `FileFor` сравнивается сам с собой; `DataCatalogNames.All()` пересчитывает рефлексию на каждый вызов, `"*.json"` остался литералом в `ShippedCatalogTests`; `LootTablesAuditTests.GroupProperty` дублирует `AugmentsField`.
- (знать, несущий инвариант) `SchemaKeyOrder.Merge` слепляет поля вариантов и `TableRecord` только пока `Alike` — лишний `[CatalogRef]`/`[Range]` на одной стороне сделает `augments` → `Any`, `tier`/`rarity` потеряют ранг, тест «все ключи известны» упадёт.

## Из скелета хоста DataEditor (2026-09-02; accept with minors) — первые три закрываются задачей полировки

- (minor, ДЕФЕКТ на русской Windows) `src/Tooling/DataEditor/Source/View/InspectorPanel.cs:231` + `Tooling.Core/Catalogs/CatalogRecords.cs:115` — `JToken.ToString()` без культуры: `0,5` и `True`. Стало: строка → `Value<string>()`, остальное → `ToString(Formatting.None)`.
- (minor) `EditorRoot.cs:142` — «всего каталогов» считается от собравшихся схем; знаменатель — `CatalogDescriptors.All.Count + NotYetDescribed.Count` из `GameCatalogs.TotalCount`.
- (minor) `EditorRoot.cs:189-194` — весь `Report` в `AcceptDialog.DialogText` без скролла; `ScrollContainer` + `Label` внутри диалога либо первые N.
- (minor) `InspectorPanel.cs:222-238` — не использует `Documentation` (tooltip), `LocalizedKey` (`TranslationServer.Translate`), `EnumValues`/`Range`.
- (minor) `CatalogWorkspace.cs:96` — ловит только `ArgumentException`, а `Describe` — чужой код; ловить `Exception` в ноту `Undescribed`.
- (minor, тесты) `CatalogWorkspaceTests.cs` — не покрыты: схема не собралась (каталог выбывает), дубли id между файлами (оба сохраняются), `Single`/`Dictionary` не той формы.
- (minor) у `DataEditor` нет своего `.sln` рядом с `project.godot` (кнопка Build в Godot и экспорт ждут его с конфигурациями `ExportDebug/ExportRelease`) — скопировать `PassiveTreeEditor.sln` со сменой имён/GUID.
- (minor, дубль ×6) хелпер `Text(...)` в `CatalogWorkspace.cs:150`, `CatalogRecords.cs:123`, `EditorRoot.cs:196`, `InspectorPanel.cs:250` + `Note()` в `CatalogSchemaBuilder.cs:138`, `SchemaReflector.cs:617` — один `internal static` в `Tooling.Core`.
- (nit) `main.tscn` без uid — первый запуск перепишет файл; `Locate` и `SchemaKeyOrder` читают «пустой ключ = корень» по-разному (единственная секция vs всегда) — `SchemaGuard` отвергать пустой ключ при `Sections.Count > 1`; `Select(index)` без `EnsureCurrentIsVisible()`; `OS.HasFeature("editor")` литералом (как у образца).

## Из выноса канона эффектов в Core (2026-09-02; accept with minors)

- (minor, ловушка; ревьюер счёл обязательным) `src/Battle/Source/EffectProvider.cs:181,254-257,299-303` — вердикт сверки «канон ↔ фабрики» взводится один раз навсегда: если первый вопрос задан при пустом каноне, `_refused` остаётся пустым и после поздней загрузки строка с опечаткой в ключе пройдёт гейт; старый `Apply` перевзводил защёлку на каждый файл. В игре недостижимо (каталог грузится до первого вопроса, файл один), пин `TheRegistryTakesItsCanonFromTheCatalogAtTheMomentItIsAsked` закрепляет эту дорогу. Стало: защёлка по содержимому — `if (_judgedRows == canon.Ids.Count) return; _judgedRows = canon.Ids.Count; _refused.Clear();` (три строки) либо ревизия на `IEffectCanonCatalog`.
- (minor, док) `src/Core/Battle/Abilities/IEffectCanonCatalog.cs:5-9` — док приглашает описательные поверхности инжектить каталог, а строки каталог отдаёт НЕПРОВЕРЕННЫМИ (гейт `_refused` только в `EffectProvider`); сказать, что описательные поверхности читают канон через `IEffectProvider`.
- (minor, шум) `src/Testing/BattleSystemTests/DotPotencyFromCanonTests.cs:88-89` + `EffectCanonCatalog.cs:41-42` — повторное чтение тех же файлов даёт ~55 ложных «declared more than once» за тест (×14): убрать строку 89 теста; `Apply` сделать идемпотентным по имени файла (снять строки, прочитанные ранее из того же `file.FileName`).
- (nit) комментарии регистраций (`BattleSystemModuleDependencies.cs:70-72`, `GameDataDependencies.cs:39-41`) обосновывают вынос потребителями, которых ещё нет; сообщение о дубле — копия `AbilityAugmentCatalog.Declare` (общий хелпер `Declare(dictionary, id, record, kind, file)` в `Core/Data/GameData`); `Ids => _canon.Keys` — живой вид, сказать в доке; пин `ARowTheFactoryCannotTakeWholeIsWithheldRatherThanHalfUsed` не проверяет `PowerOf`; неиспользуемые usings в `EffectPowerTests`, `EffectStackCeilingTests`, `CriticalCalculationTests`, `DotPotencyFromCanonTests`; три копии «сырого» читателя строк канона в тестах (`EffectCanonCatalogTests.ShippedRows`, `EffectCanonTests.Canon`, `DeepFreezeDamageTests.Numbers`) — в `EffectProviders`.
- (знать, вне скоупа) `src/Core/Data/IGameServiceProvider.cs:17` — `TryGet<T>()` отдаёт ПЕРВУЮ регистрацию, `GetService<T>` — ПОСЛЕДНЮЮ: композиция, перекрывающая общую регистрацию, читается двумя дверьми по-разному.

## Из расширения контракта схемы: NotARef-след и ключи словарей (2026-09-02; accept with minors)

- (minor, молчание) `src/Tooling/Tooling.Core/Schema/SchemaReflector.cs:628,670` — `[DictionaryKey(typeof(A))][DictionaryKey(typeof(B))]` схлопывается в первый enum без ноты (`Narrowing.Ways` считает роды, не число ответов). Стало: собирать различающиеся enum-типы, `Ways => EnumWays + (Catalogs.Count > 0 ? 1 : 0)`, нота `KeyNarrowedTwice` обобщается «names more than one thing its keys may be; '{2}' was taken».
- (minor, порядок атрибутов) `Tooling.Tests/Schema/SchemaReflectorTests.cs:251` — `AreEqual` на `RefCatalogs` ключей держится за неспецифицированный порядок `GetCustomAttributes`; как у значения — `AreEquivalent`.
- (minor, ОБЯЗАТЕЛЬНО при разметке baseParameters) `src/Testing/BattleSystemTests/CatalogDescriptorTests.cs:244` — `FreeKeyedMaps` опознаёт свободную карту по `Key == null`; как только на `NpcData.BaseParameters` встанет `[DictionaryKey(typeof(EntityParameter))]`, поле выпадет из исключений и тест «все ключи файла известны» покраснеет (`SchemaKeyOrder` ключи карт не ранжирует вообще). Стало: критерий «карты вообще» (`Kind == Dictionary`), комментарий метода переписать; альтернатива дороже — научить `SchemaKeyOrder` ранжировать ключи карт по `Key.EnumValues` (решение про данные: порядок `baseParameters` станет каноническим).
- (nit) дубли каталогов в `RefCatalogs` не схлопываются ни у ключей (`:671`), ни у значений (`:659`) — если дедуп, то в обеих строках; `:670` лишний `Select`; `:644` `Markup.Narrowings` дублирует счёт `Narrowing.Ways`; решение «`[NotARef]`+`[EnumOf]` → Enum с флагом» тестом не закреплено; `[DictionaryKey]` на `List<Dictionary<string,T>>` доходит до элемента — поведение верное, не записано; будущий пин по `RefusedAsReference` через `Leaf` покроет значения, но не ключи карт (отказ для ключей выразить нечем) — назвать вслух при написании.

## Из описателя каталога Npc (2026-09-02; accept with minors) — часть уходит в следующие описатели

- (minor, тест не фальсифицируем) `src/Testing/BattleSystemTests/CatalogDescriptorTests.cs:181-186` — `once == twice` структурно не может упасть (стабильная сортировка по рангу идемпотентна); заменить на «канонический прогон ничего не теряет»: `JToken.DeepEquals(root, Parse(once).Root)`; четыре переставленные записи боссов держать именованным списком, чтобы пятая стала падением. Док теста привести к проверяемому.
- (minor, адресность) `CatalogDescriptorTests.cs:171,241` — свободные словари исключаются по имени последнего сегмента (`properties`, `baseParameters`), а не по узлу схемы; словарь под массивом не ловится. Вести `FieldSchema?` рядом с обходом json и исключать по `Kind == Dictionary && Key == null` этого узла.
- (minor, дубль) `CatalogDescriptorTests.cs:293` ↔ `src/Testing/ShippedCatalogTests.cs:38` — рефлексия по константам `DataCatalog` и `"*.json"` дважды; общий `DataCatalogNames.All()` рядом с `SharedDataRoot.cs`.
- (minor, не удержано) `CatalogDescriptorTests.cs:104-118` — тест (б) не пинит `LocalizedSuffixes` (только имя) и `Sections.Count`.
- (minor, не удержано) `CatalogDescriptorTests.cs:39-46,130` — ссылки проверяются `Contains` без `AllowEmpty` и без точного состава `RefCatalogs`; тройка `(Path, Catalog, AllowEmpty)` + `AreEquivalent`.
- (minor → следующий описатель) пин «поле с суффиксом Id — ссылка либо отказ» теперь пишется по схеме: `FieldSchema.RefusedAsReference` появился (T2.7); добавить в `CatalogDescriptorTests` общий проход по `CatalogDescriptors.All`.
- (nit) `src/Main/LastBreath.sln` не содержит `LastBreath.Descriptors`/`Tooling.Core`, на которые ссылаются тесты — добавить в решение.
- (nit) `NpcBehaviorData.cs:16` — `NpcBehaviorData.Stance` парсится `ParseEnum<Stance>`, не размечен (каталог NpcBehaviors без описателя); `NpcData.cs:211-213` — атрибуты разнесены на три строки; `NpcCatalogDescriptor.cs:36` — имя файла взято из константы имени каталога, завести `FileName`.
- (данные, находка владельцу) `Npc.json`: у `Npc_Boss_Digri`, `Npc_Boss_Zigri`, `Npc_Boss_Rat_King`, `Npc_Boss_Bone_Pack_Leader` ключи стоят не в порядке DTO (`rarity`/`authored` после `entityType`; у последнего `passives` перед `abilityBehaviors`) — первый сейв из тула переставит их (ожидаемо по политике записи).

## Из переноса PlayerLifecycleConfigProvider (2026-09-02; accept with minors)

- (minor, IDE0005) `src/Battle/Services/GameServiceProvider.cs:3` `using Core.Ai.World;`, `src/Main/Services/GameServiceProvider.cs:5,45` `using Core.Ai.World;` и `using World;` — осиротели после переезда (ни один тип этих пространств в файлах больше не называется); `.editorconfig` держит IDE0005 как error, но `EnforceCodeStyleInBuild` не выставлен. Снять три строки при следующей правке бутстрапов.

## Из переноса GameWorldClock в Core (2026-09-02; accept with minors)

- (minor, мёртвая ветка) `src/Core/Ai/World/Recovery/RestRecoveryService.cs:9,23,42` — после общей регистрации часов ни одна композиция не даёт сервису `clock == null`; фолбэк `_fallbackMinutes` и док «without a clock (sandbox scenes)» описывают несуществующий сценарий, а стоящие часы песочниц (никто не тикает) хуже отсутствующих: если песочница начнёт тикать восстановление, `minutes` всегда 0. Стало: `IWorldClock` обязательный параметр (тесты подают `SettableClock`), ветку снять.
- (nit, комментарий) `src/Main/Services/GameServiceProvider.cs:92` — «World NPC stack: providers, population, skirmishes, spawner — raids sit on top» перечисляет то, что регистрируется ниже по файлу; сократить до «providers and configs».
- (гигиена) осиротевшие `.uid` от прежних переездов: `src/Main/Npc/NpcProvider.cs.uid`, `NpcPopulationService.cs.uid`, `NpcWorldRegistry.cs.uid`, `FactionRelationService.cs.uid`, `src/Battle/Internal/Npc/NpcProvider.cs.uid`, `NpcPopulationService.cs.uid`, `src/LootGeneration/Services/NpcModifierProvider.cs.uid` — снести одним проходом (заказано вместе с переносом `PlayerLifecycleConfigProvider`).

## Из переноса NpcBuffs и базовых данных способностей в Core (2026-09-02; оба accept)

- (nit, комментарий обещает больше) `src/Core/Data/GameData/GameDataDependencies.cs:37-38` — «…or its corpse is paid out»: LootGeneration каталог бафов не спрашивает; настоящая причина общей регистрации — два тела, спавнящие NPC, живут в разных композициях.
- (гигиена, предсуществующее) `src/Battle/Internal/Npc/NpcProvider.cs.uid`, `NpcPopulationService.cs.uid` — осиротевшие сайдкары от прежних переездов (классы в Core без сайдкаров); `git rm` отдельной уборкой вместе с `LootGeneration/Services/NpcModifierProvider.cs.uid`.
- (nit, порядок членов) `src/Core/Battle/Abilities/IAbilityAugmentCatalog.cs:24-32` — свойства `Abilities`/`AbilityIds` объявлены после метода `Find`; поднять под `All`.
- (nit, док) `AbilityAugmentCatalog.cs:21` — «immutable, nothing here copies them»: `Tags` это `string[]` по ссылке; честнее «handed out as they were parsed».
- (nit, репорт дубля) `AbilityAugmentCatalog.cs:64` — сообщение называет только файл второго объявления; при разбиении каталога на файлы начнёт врать: `"{kind} '{id}' is declared again in '{file}'; the last declaration wins"`. Тем же махом `TryAdd` вместо `ContainsKey`+индексатор.
- (nit, имя) `IAbilityAugmentCatalog`/`AbilityAugmentCatalog` теперь отвечают за обе секции файла (abilities + augments); переименование в `IAbilityCatalog` задело бы partial-файлы `AbilityProvider` и два стаба тестов — отдельной задачей.
- (nit, не запинено) `src/Battle/Source/BattleSystemModuleDependencies.cs:43-45` — единственность инстанции `AbilityProvider` теперь собрана вручную (три регистрации), мутация во вторую инстанцию переживает прогон; один `Assert.AreSame` на `IAbilityProvider` vs `IAugmentLaidEffects` в `AbilityAugmentCatalogTests` закрыл бы.
- (дубль, предсуществующее) `AugmentFitTests.cs:447` и `AugmentGrantedTagsTests.cs:671` — `AugmentCatalogStub` байт-в-байт в двух файлах; один общий стаб рядом с `AugmentCopies`.
- (док) CLAUDE.md «Данные»/«Аугменты» не говорит, что базовые записи способностей отдаёт Core-каталог `AbilityAugmentCatalog`, а `AbilityProvider` — больше не участник загрузки; дописать при следующей правке файла (сейчас в незакоммиченных правках владельца).

## Из переноса парсеров в Core под тулы (2026-09-02; CombatRules — accept with minors, ItemDataProvider шаги 1–2)

- (minor, комментарий лжёт) `src/Battle/Source/BattleSystemModuleDependencies.cs:55-58` — комментарий над `AugmentMinter` ссылается на «those rules … the two are registered together»: регистрация `CombatRulesProvider` уехала в общие участники, антецедента нет; полоса редкости аугмента и раньше жила в `AbilityAugmentData.RarityBand`, не в `CombatRules.json`. Переписать на «minting is a job of this module».
- (minor, имя теста обещает то, чего он не меряет) `src/Testing/BattleSystemTests/MulticastStageRulesTests.cs:94-104` — после общей регистрации null-ветка `?? MulticastRules.Default` в `MulticastActivation.ResolveRules` в этом прогоне не исполняется (провайдер есть, но не загружен); переименовать в `TheStanceRollsTheWorkingLadderWhenTheCompositionCarriesNoRulesOfItsOwn` и сказать в комментарии, что null-ветка резолвера здесь не доказывается.
- (minor, комментарий лжёт) `src/Testing/LootSimulation/LootPipeline.cs:52-54` — стенд не читает `ICombatRulesProvider`, числа аугмента не «вокруг полосы из CombatRules.json»; правила приезжают просто как часть общего набора.
- (minor, док-ссылка в пустоту) `src/Docs/SharpeningAndHitCap.md:79` — ссылка на удалённый `src/LootGeneration/Services/ItemDataProvider.cs:67`; заменить на имя метода `GetEquipItemBaseModifierPool` в `ItemDataProvider` без пути (правило документации №4).
- (nit, порядок членов) `src/Core/Data/ItemDataProvider.cs` — `AllBlueprints` стоит между методами, приватные хелперы внизу; унаследовано дословно из Main-копии, перекладывать вместе с следующей правкой файла.
- (знать, кандидат в карточку) `src/Core/Data/ItemDataProvider.cs:58-60` — `_upgradeCosts = ParseUpgradeCosts(...)` — присваивание, не слияние: второй файл в каталоге `UpgradeCosts` молча затрёт первый (у остальных каталогов провайдера — слияние). Теперь правило общее для четырёх проектов.
- (гигиена) `src/LootGeneration/Services/NpcModifierProvider.cs.uid` — осиротевший сайдкар: класса в папке нет, живёт в `src/Core/Services/`.
- (знать) `src/Core/Data/ItemDataProvider.cs:26` — Crafting и LootGeneration теперь грузят каталог `Items` (пустой по факту: `ParseItems` читает `ItemDataList.Items`, а файл несёт секции `quest`/`common` — см. запись про `Items.json` выше); потребителей нет.

## Из Б-191 (2026-08-24, коммит `d9d03739`; accept with minors)

- **(кандидат, тест-инфра)** Node-половина replay-фикса без пинов (гонка гейта `WaitUntilIdleAsync`, идемпотентность `ShowDeathAsync`, деление hold на скорость, очистка `_shownDead`) — три мутации ревьюера зелёные. Предложение ревьюера: вынести за Godot тем же приёмом, что `DeathBeat`, состояние гейта (какой насос ждём) и реестр упавших — обе половины чистые, обе получили бы пины.
- (знать, тихая деградация) `IAnimationsComponent.HasClip` — default-реализация `false`: тип бойца, забывший переопределить, молча получает hold 0 и возврат бага #191 для себя (сегодня обе реализации переопределяют).
- (знать) Правило «мертвец не кастует» живёт в трёх местах (гейт аккорда, ReplayInstant, DeathBeat-фильтр — последний для каста недостижим, наследие формы).
- (знать, цена названа) Аборт/выход во время НАЧАВШЕГОСЯ падения доигрывает его на старой скорости (до ~0.46с) — `SceneTreeTimer` не укорачивается сменой скорости; дедлайн выхода 5с покрывает с запасом.

## Из аудита подвязки эффектов (2026-08-24, read-only; карточка #201 — механизм за владельцем)

- **(единственное непокрытое обещание)** `Augment_Ice_Aegis_Crit_Mitigation_Under_Shield` «пока щит держится» — баф живёт своим 3-ходовым таймером, совпадающим с аегидой случайно; сломанный барьер оставляет 50–80% митигации висеть.
- (лок, 4 расхождения) плашка `Effect_Ice_Crit_Mitigation` говорит «for N turns» против карточки «while the shield holds»; плашка мифика обещает «by {Duration} turns» при коде `Extend(1)` (и печатает остаток СВОЕЙ длительности); терминология «shield» двоится (барьер аегиды vs слой IShieldEffect — `Condition_Shielded` аегиду НЕ увидит); эхо Дикобраза — рабочая подвязка, о которой текст молчит.
- (фундамент, знать) `EffectRemovedEvent` объявлен и мёртв; `AbilityTrace`/CastId штампуется на каждый эффект и никем не читается (`IsFromSameCastAs` — только тесты); «последний носитель» переписан руками трижды (мифик/OblivionSeal/статусы); 36 из 38 условий каталога без потребителя в данных; предикатный слой не дотягивается до эффектов (эффекты кладут декораторы, не модификаторы); доля барьера по источникам не учитывается (признано в `IceAegisEffect`).
- (гигиена, знать) `Detach` переопределяют 2 райдера из 13 — остальные без подписок, но дефолт-но-op может молча течь при будущих подписках.

## Из Т-9 (2026-08-24, коммит `88920cdd`; сверка ведущего) — хвосты владельцу

- ~~(похоже на БАГ механики) Crit Mitigation-аугменты кладут Effect_Enhance_Defense~~ — ПОДТВЕРЖДЕНО Todd 2026-08-24: «эффект не тот» → карточка #199 (Todo), задача С-3: правильный эффект в `CriticalDamageMitigation` с канон-строкой, значения по доку (80% / 50–80% лестницей по редкостям), записи на behaviour-дорогу.
- (данные) `Augment_Stage_Four_Damage`: теги [spell, stage], док «Урон 4 стадии» (Перегрузка), а en-имя/описание — про Ice Block («Extra Block Damage»).
- (лок, дубли имён en в трее) «Incoming Reduction» ×2, «Crit Mitigation» ×2, «Ignore Resistances» ×2 — развести формулировками.
- (лок, знать) En-имена-эхо id (Apply Buff Critical Chance и др.) и три ru-имени, разошедшихся с доком (Снижение стоимости/перезарядки, Регенерация) — оставлены как были; «Чистое эхо» — возможно «Священное эхо» (запись давно sacred).
- (данные) `Augment_Cost_Barrier` — tier 3 при близнеце tier 2 и позиции дока в Т2. Позиции дока без записей: «Объем щита», «Усиление яда/горения/кровотечения», «Цепь снарядов», «Раскат», «Клинок падальщика», «Баф при активации способности».
- (данные, дыра) Рецепты ссылаются на несуществующие `*_Iron_Bastion` (Body/Helmet/Boots/Gloves) — в EquipItems таких id нет.
- (лок, мусор) 33 ru-only ключа (`*Btn`, `Test_*`, две пассивки) — аудит их не видит (проверяет en→ru); чистка при случае.
- (несопоставленные ru-имена) `Augment_Apply_Enhanced_Defence`, `Augment_Chain_Lightning_Ignore_Resistances` — пар в доке нет, msgstr пустые.

## Из Т-11 (2026-08-24, коммит `9d287068`; сверка ведущего)

- **(развилка ведущему/владельцу)** Тултип аугмента в СУМКЕ/торговле никогда не ходил через `AugmentText.Card`: общий путь предмета — без тира и без строки посадки. Отдельная задача «карточка аугмента в общем тултипе предмета» (ItemTooltipPopup).
- (данные, чистка или гуард) Мёртвые теги: 3 записи с `abilityId` и все 11 с `fitsAnyAbility` несут теги, которые не гейтят подгонку и теперь не видны в карточке — чистка данных либо гуард «привязанная запись тегов не объявляет».
- (знать) Противоречивая запись (`fitsAnyAbility` + `abilityId`) напечатала бы «Fits: any ability», ворота её отвергают (`ContradictoryDeclaration`); в данных таких нет.

## Из Т-14 (2026-08-24, коммит `5bd58328`; accept with minors → возврат закрыт)

- (nit, авторинг) Записи одной группы не обязаны быть соседними: опечатка штампа на далёкой записи молча сольёт две несвязанные строки и переставит порядок — репорта нет; в поставляемых данных чисто. Кандидат: репорт «группа с разрывом внутри канала».
- (знать) Авторинг групп — только руками в json: редактор (заморожен) группу сохраняет (round-trip/undo/копия узла), но создать не даёт; новая запись из редактора всегда без штампа. `TreeValidator` редактора правил композитов не знает — Check инструмента промолчит там, где Validate игры отрепортит.
- (знать) Счётчик `MODIFIERS n/max` инспектора считает ГРУППЫ: объединённый Small показывает 1/2, кнопки открыты — узел можно довести до 3–4 параметров (названное владельцу следствие Т-14).
- (знать) Расщеплённое условие группы при ручной правке json печатается безусловным: загрузчик `Validate` не зовёт (как и для потолков) — держит только пин «поставляемое дерево валидно».
- (знать) Flag-записи теперь входят в счётчики условных/скейл-строк сводки (раньше пропускались); параметрических Flag в дереве ноль.

## Из Т-12 (2026-08-21, коммит `f67edb0a`; rework по 2 major закрыт возвратом)

- ~~(лок, компромисс) `UI_AbilityCooldown` без плюрала~~ — РЕШЕНО Todd 2026-08-21: дробных кулдаунов не существует (все движения целые; «долевые срезы» — только гипотетика док-строки `MinimumCooldown`), плюрал возвращается микро-правкой после Т-13. Если когда-нибудь появится долевой срез кулдауна — вспомнить про `RenderPlural`/`Convert.ToInt32`.
- ~~(владельцу) `Tag_npc` = «Monster»~~ — подтверждено Todd.
- ~~(владельцу, фильтр тегов) кандидаты activation/critical/damage~~ — РЕШЕНО Todd: `activation` — в исключения показа; `critical`/`damage` остаются.
- (nit, перф) `RefreshAbilityCards` на каждый `AllocationChanged`/`Changed` доски тянет весь лист: `AbilityArt.LoadIcon` на строку + `minter.Restore` на занятую ячейку — всё ради четырёх текстовых полей. При жалобе на отклик — облегчённая дорога запроса без арта/ячеек.
- (nit, допущение) Свежесть карточки после покупки узла держится на ПОРЯДКЕ подписок на `AllocationChanged` (книга способностей подписана раньше окна — bootstrap-порядок); ничем не закреплено.
- (знать) Разводка `ShowNodeTooltip → AppendAbility` — Godot-ветка без пина (чистая часть `AbilityCard.Body` запинена); мутация «попап перестал звать карточку» тестами не ловится — чек-лист владельца.

## Из Т-7b (2026-08-21; rework по major «петля пересчёта» закрыт возвратом)

- **(архитектура канала, предсуществующее — цена выросла)** `PassiveTreeParameterSource` раздаёт ОДИН экземпляр модификатора всем зарегистрированным компонентам, а `Follow`/`SourceChanged` слышат все: второй зарегистрированный боец (окно «меню → новая игра» до `Dispose` старого игрока) раньше получал чужое «вкл/выкл» условной строки, теперь — чужое ЧИСЛО скейл-строки (замер с нового носителя). Штатные дороги безопасны (`Player` снимает `UnregisterSource` до `Detach`). Фикс — копия модификатора на носителя (pull-источник должен знать спрашивающего).
- (знать, авторинг) Взаимный скейл двух строк сходится на одном уровне благодаря щеколде реентерабельности, но числа такой пары — авторская бессмыслица; гард парса запрещает только петли через агрегаты/самопетлю.
- (инцидент) Ревью-проба взаимного скейла на ЧИСТОМ коде до щеколды убила тестовый процесс (StackOverflow, системный диалог «guard page» у владельца) — впредь пробы рекурсии только после защитного фикса.

## Из Т-7a (2026-08-21, коммит `de48fcf2`; сверка ведущего)

- (знать, другой форматтер) `ContextModifierFormatter` отрицательные значения не обрабатывает (шаблоны — целые предложения «Increases … by {value}» → отрицательный ролл напечатал бы «by -25%»); данных таких нет.
- (знать) `FormatDescriptorRange` (таблица пулов крафта) на отрицательном диапазоне напечатает «-30 – -20»; данных таких нет.

## Из батча Т-5/Т-6/Т-8 (2026-08-21, коммит `0b23b354`; accept with minors → возврат закрыт)

- (знать, Godot-ветка без пина) Гард `_ExitTree` «не запоминать вид, отфреймленный по пустому документу» тестами не достаётся (состояние живёт в инстанцированной сцене) — проверяется чек-листом владельца (пункт «открыть колесо без дерева → закрыть → открыть с деревом → фрейм на всё дерево»).
- (знать, предсуществующее) В Battle-песочнице сессионный сброс не зовётся ни для одного участника (нет меню) — память вида там живёт весь процесс.
- (nit, две точки входа в один шов) `PassiveNodeLines` → `ConditionalLineText.Join` напрямую, `EquipItemLines` → через фасад `Localization.WithCondition`; обе в одну реализацию (Core-класс с опциональным провайдером не может опираться на статик-фасад) — знать, не чинить.
- (лок, Т-9) `ru.po`: `Modifier_Conditional` и все 39 `Condition_*` — ключи с пустым msgstr (конвенция; под ru клауза напечатается по-английски через фолбэк).

## Из Т-3/Т-4 (2026-08-21, коммит `0a6b499d`; accept with minors → возврат закрыт)

- (nit, знать) `SocketRingGeometry` — eps-сравнения промежутков (`SameAngleRadians`) нетранзитивны: тройка промежутков, попарно различающихся меньше eps, но крайние — больше, разрешится по-разному при разном порядке обхода; на поставляемом дереве минимальная разница промежутков 6.6° — недостижимо. Если чинить — сравнивать с зафиксированным максимумом, а не перезаписывать `widest` победителем тай-брейка.
- (nit, тесты) Угловая арифметика (`Apart`/`Degrees`) — две независимые копии в `PassiveSocketRingTests` и `Bench` в `PassiveWheelPipClickTests` (намеренно независимы от геометрии, но одна копия на два файла была бы честнее).
- (знать) `WheelSocketLayer._nodes` (NodeGeometry) используется только в null-guard `Rebuild` — размеры читает геометрия кольца (предсуществующее).
- (лок, Т-9) `ru.po`: ни один `UI_PassiveTree_*` не переведён (новые ключи Т-4 легли в ту же яму осознанно).

## Из С-1 плейсхолдер-фикса карточек (2026-08-21, коммит `ffbd6db0`; принят сверкой ведущего)

- ~~**(хвост класса, 4 записи)** Фабричные записи, чей эффект назван В КОДЕ, ассемблеру описаний невидимы~~ — ЗАКРЫТО С-1b `0cf29ef1` (`AugmentFactory.Laying`, резолвер `IAugmentLaidEffects`).
- ~~(лок/механика) `Augment_Reduce_Cooldown*` обещают долю, запись несёт ходы~~ — ЗАКРЫТО С-1b `0cf29ef1` (тексты под `{cooldownTurns|turn|turns}`); там же Primal «+{damageMultiplier:%}» (механика ShareGained = +35%) и Burning без клаузы стаков (канон 999 = без предела).
- (nit, тест-инфра) `AugmentDescriptionTests.Cards(minter, catalog, record)` — параметр `catalog` не используется.
- (nit, тест-пин) `AugmentDescriptionTests:110,113` — канон-пин прибит к числу `Effect_Healing_Fury.healAmount` 15%/55% — ребаланс покраснит (та же цена, что у соседа `TheNumbersOnACardAreTheCanonsAndMoveWithIt`).
- (знать, propertyRefs) `propertyRefs` (duration → Duration хозяйки) читаются в момент каста, описание собирается раньше: строка, назвавшая такой ключ, напечатает канонные ходы, а не декорированные хозяйкины. Сегодня ни одна не называет — вреда нет.
- (открытая развилка дизайна, не баг подстановки) Дизайн-док («Гниющие раны», «Разложение») описывает эти аугменты как «стак эффекта с УВЕЛИЧЕННОЙ на 10–45% эффективностью», а .po описывает сам эффект — развилка «эффективность на эффект» из прохода аугментов (§4f) всё ещё не решена.

- ~~(хвост класса, 4 записи)~~ и ~~(лок/механика, Reduce_Cooldown*)~~ → **ПРИНЯТО владельцем 2026-08-21** («Фикс описания семейства Fury принимаю»; «откуда доля неизвестно, механика обещает целые ходы — править исходя из этого») → задача С-1b (plan.md, карточка #197).

## Из Т-1b (2026-08-21, коммит `2605b986`; принят, minors сюда по бюджету окна)

- (minor) `PassiveWheelCanvas.cs:438-447` — гуард `_sockets != null` в OnLeftClick мёртв (окно создаёт курьера безусловно), а док-абзац про «композицию без курьера» описывает механизм, которого нет (песочницу держит отсутствие документа); проверка задвоена с `_sockets?.Pick`. Снять одну из двух + переписать абзац по факту. 
- (minor) `PassiveWheelPipClickTests.cs:158-170` дублирует фикстуру `PassiveSocketRingTests.cs:146-152` (лестница радиусов, геометрия, зум/пан) — общий хелпер на оба класса.
- **(nit, но реальный сирота-путь — предсуществующее Т-1, теперь достижим и с колеса)** `AugmentSeating.Offer:94-117` — между await запроса кандидатов и `ShowPopup` окно может умереть (ClosePicker уже отработал) → пикер всплывёт над миром сиротой. Гард живости после await.
- (nit) `Bench.Pips()` стенда — вторая сборка пипа против `WheelSocketLayer.Pip` (вынужденная: слой — Godot-нода); при расширении слоя выносить сборку в чистый статик у SocketRingGeometry.
- (знать, UX — владельцу глянуть в прогоне) Пикер с пипа сосуществует с тултипом узла под курсором (разные типы попапов — по политике норма); MouseExited гасит тултип при уходе к списку. (2026-08-21, коммит `c0f037c9`; rework закрыт возвратом)

- (владельцу, .tscn) `AugmentCell.tscn` DormantMark сине-серый (modulate 0.38/0.48/0.64), а строка спячки в карточке — красная (`TextPalette.Debuff`): один факт двумя цветами; значок показывается только для Dormant, карточка говорит и за Partly.
- (знать, поведение шире задачи) Гуард «пока драг в воздухе — тултипов нет» теперь ОБЩИЙ в `HoverTooltipHandle.OpenAfterDelay` — накрыл и сумку/торговлю/HUD; в Godot-прогоне проверить, что перетаскивание предметов больше нигде не поднимает карточек.
- (знать) Бейдж плитки трея в композиции без каталога аугментов печатает «0» — доревизионное, не Т-2.
- (nit, слабый тест) `TheBagAndTheSocketShowTheSameAugmentTheSameWay` — держит согласие перегрузок на константах, не канал минтера (ревьюер: оставить как есть).

## Из Т-17 (2026-08-20, коммит `53c8e8cd`; rework закрыт возвратом)

- **(контракт канала, знать при авторинге Т-15/Т-16)** Строка УЗЛА побеждает строку КЛАССА целиком (ничего не наследует — как Tint/Body): персональная иконка socket-узлу требует повторить в его строке и `Hides Augment Mark`. Задокументировано в xml-доке поля и покрыто тестом; полное пополевое наследование — другая модель резолвера, заводить только при реальной боли художника.
- (знать, авторинг) У художника два файла на «вид узла»: геометрия/радиус/HasView — `PassiveWheelStyle.tres` (её читает пикинг), художественный слой — `PassiveNodeVisuals.tres`; per-узловой РАЗМЕР каналом не выражается. `Tint` открывается прозрачным чёрным — не поднятая альфа = «не задано».
- (nit) `Effect` у узла, рисуемого массой (класс без HasView), молча ничего не делает — в xml-доке, без репорта.

## Из А-1c-2 (2026-08-20, коммит `da3e654f`; принят)

- **(развилка владельца, OnEdge)** `OnEdgeEffect` (0.03 в коде): эффект никто не кладёт, он вне канона и реестра; число 3% есть в дизайн-доке ПАССИВНЫХ способностей («На грани»), в списке временных эффектов его нет. Варианты: (А) эффект в канон+реестр (прецедент Life_Giving_Shade); (Б) пассивка, число данными; (В — дефолт ведущего) оставить с этой пометкой и решить при первом кладущем.
- (той же природы) `Effect_Poison_Coating` — вне канона и реестра EffectProvider (числа от способности, 0.45 из json способности); пейринг-гард дыру не видит (нет ни строки, ни фабрики).
- (nit, док) `DamageOverTurnEffect.NoCeilingOfItsOwn` — док обещает «above every canonical ceiling», фактически 999 РАВНО всем отгруженным потолкам; если канонный потолок поднимут выше 999, константа молча начнёт резать ниже канона — дописать условие в док. Семь продакшн-мест пишут литерал 999 вместо константы (вне скоупа А-1c-2).
- (знать, семантика) `PoisonOnHitRider` трактует декорированный `PoisonPotency == 0` как «ключа нет» → канон 0.35; будущий аугмент «яд не тикает» получит канон вместо нуля.
- (знать) `Upgrades.cs:137` `GetValueOrDefault("poisonPotency", 0.7f)` — не дрейф (запись несёт 0.7, дрейф-гард А-1c-1 держит), но число в коде.

## Из А-1c-1 (2026-08-20, коммит `1b1f1a4a`; принят, возврат закрыт)

- (nit, тест-инфра) Пятая копия идиомы «walk-up от AppContext.BaseDirectory до исходников» в тестах (AugmentFitTests, EffectValueShapeRosterTests, AbilityUnlockTests, CombatWindowClosureTests, AbilityDefaultDriftTests) — просится общий хелпер рядом с `SharedData`.
- (знать) `AugmentParameterTableTests.EveryTranslatedRecordFallsBack…` пинит те же 40 фолбэков ParameterAugments против НАМЕРЕННО литеральной таблицы (снимок доколлапсного поведения); новый дрейф-гард — вторая половина «код против записи»; при мутации краснеют оба — это корректно, не дубль.

## Из А-1b-3 (2026-08-20, коммит `be63ade1`; принят, minor в бэклог по бюджету окна)

- **(minor, пять строк теста)** `StageAndAugmentNumbersTests.cs:279-296` — `maxStacks` Leach единственное из девяти чисел без доказанной ДОРОГИ: пин держит значение, но доктор-запись двигает только `duration`; вернуть литерал `maxStacks: 1` в код — тест останется зелёным. Фикс: в доктор-словарь `["maxStacks"] = 3` + ассерт на `moved.MaxStacks`.
- (nit) `Armageddon.cs:105` — опечатка `missingHpStep: 0` в записи молча съест бонус (гард корректен, но молчалив против конвенции «опечатка в данных = Tracker»); добавить `Tracker.TrackError` рядом с пропуском.
- (знать, класс) Три числа аугментов (`damageMultiplier`, `poisonPotency`, `duration` Leach) «заморожены по копии»: `AugmentInstance.Values` роллится на минте и сейвится — балансная правка записи не достанет копии в сейвах игрока (только новые). Для abilityProperties шести остальных переездов этого нет.
- (знать) `BerserkFury.cs:90` — запись с `minContinueChance > maxContinueChance` уронит каст громким `ArgumentException` из Clamp (осознанно). Пол серии 0.05 поведенчески почти недостижим (ярость жжёт 5% максимума за удар) — пин пола доказывает чтение ключа, не отгруженное значение.
- (nit, лок/имена) id `Augment_Ice_Block_Consume_Stun_Deal_Double_Damage` говорит «Double» при множителе в данных (переименование ломает сейвы — при следующей сейв-ломке); описание записи в .po не печатает `{damageMultiplier}` (→ Т-9).
- (знать) Перевод `Augment_Leach_On_Crit` на `behaviour`+`effectId` в будущем упрётся в ворота Unbuildable (у `Effect_Crit_Leech` нет канон-строки; `duration`/`maxStacks` в записи станут «дублированием канона»).

## Из ревью А-1b-2 (2026-08-20, коммит `7a07bdb6`; принят, minor в бэклог по бюджету окна)

- **(minor, одна строка)** `CombatRulesProvider.cs:56-67` — дубликаты строк одной стадии в секции `multicast` не отсеиваются: две записи `{stage: 2}` = два независимых броска (шанс 1−(1−p)²), единственная МОЛЧАЛИВАЯ порча секции. Фикс: перед `stages.Add` проверка занятой стадии → Tracker + скип, как у прочих битых строк.
- (nit) Отсутствующая секция `multicast` репортится Tracker'ом, соседи (arena/effects/exhaustion) молчат — частичный CombatRules.json для эксперимента-оверрайда будет краснеть на каждой загрузке; развод «нет секции»/«битая» не окупается, жить осознанно.
- (nit, дизайн-заметка) `chance: 0` НЕ выключает стадию (сравнение `<=`: розыгрыш ровно 0.0 проходит) — выключение стадии = удаление строки, не ноль.
- (nit) Три копии факта «стадия 1 не роллится»: `FirstRolledStage = 2` (провайдер) + `BaseStage = 1` ×2 (MulticastActivation, MulticastAbility) — свести в `MulticastRules` при касании.
- (nit, знать) Ветка «провайдер В композиции и каст берёт числа у него» пином не покрыта (пин ролла идёт явным перегрузом; тот же пробел у соседа ResolveExtensionBudget) — если бы ResolveRules не дотягивался до провайдера, ни один тест не показал бы: дефолт и файл совпадают числом.
- (nit) `StubCombatRules.cs` мёртв (ноль потребителей с `d9278c89`) — удалить или начать использовать.

## Из расследования uid-инцидента (2026-08-20; ремонт применён — 36 сайдкаров восстановлены значениями из сцен, битых ссылок 51 → 15)

- ~~(развилка №1)~~ ЗАКРЫТО решением Todd 2026-08-20: `*.uid` снят из `.gitignore`, 572 сайдкара закоммичены (`29db64a0`) — свежий клон больше не мина.
- ~~(развилка №2)~~ РЕШЕНО Todd 2026-08-20: песочницу Crafting оставляем как есть — два её битых сайдкара (`InventorySlot.tscn:3`, `InventoryWindow.tscn:3`) не чиним до решения судьбы песочницы (её main.tscn сломан независимо и давно).
- ~~(развилка №3)~~ ЗАКРЫТО: `export.ps1` переведён на относительную ссылку (mklink /D под Developer Mode, честный отказ без него, нерекурсивная замена старого абсолютного junction; сам junction на диске пересоздан) — коммит по логу 2026-08-20.
- (задача-кандидат, из «замечено» исполнителя) В репозитории НЕТ скрипта, создающего остальные девять симлинков — они заведены руками: свежий клон/worktree их не получит, корректность целей ничем не проверяется. Напрашивается общий скрипт-восстановитель ссылок (образец логики теперь в export.ps1).
- (знать) `Export/` держится вне Godot-сканера редактора через `.gdignore`, который создаётся только запуском export.ps1 — удалённый вручную вернётся не сразу.
- (предшествующая гниль, 15 битых ссылок — не инцидент) Легаси-сцены с мёртвыми и uid, и путями: `Battle/Scenes/Main.tscn:3,5,6`, `Battle/Scenes/MainWorld.tscn:3-6` (не используются, main_scene = Internal/Main.tscn); `Crafting/main.tscn:3,4` (это MAIN_SCENE песочницы Crafting — сломан давно); `Crafting/Internal/Resources.tscn:3,4`; `Main/MainMenu.tscn:3` и `Main/UI/View/Inventory.tscn:3` (обе не используются). Кандидаты на снос при чистке сцен.
- (знать, класс) ~33 ссылки, где uid РАЗРЕШАЕТСЯ, но в другой файл, чем говорит путь (дубли ассетов/скриптов между проектами; uid перебивает путь). Настоящий казус: `Battle/Source/UIElements/SaveLoadWindow.tscn:3` держит uid `Main/UI/SaveLoadWindow.cs`, а не одноимённого файла рядом.
- (инструменты) Скрипт-сверка uid лежит в скретчпаде сессии (`uid_audit.sh` + `rollback.tsv`) — при следующем инциденте пересобрать по описанию из этого бэклога: собрать все `uid://` из ext_resource → все объявленные (сайдкары+`.import`+заголовки) → разность.

- (той же природы, при первом касании) `AttackChanceCalculationBuff` (`Effect_Attack_Chance_Calculation_Buff`) — вариант-замена КР по-прежнему вне канона/реестра: ни потолка, ни силы, ни гарда; никто не кладёт (шва-присвоения нет ни в одной записи), но дыра в канон-машине та же, что была у крит-бафа.
- (nit, док шва) `CriticalCalculation.PrimaryBuffFactory` — с `field ??=` присвоение null молча возвращает дефолт (раньше NRE на касте); xml-док описывает шов как инициализатор — дописать оговорку. Сеттер по-прежнему публичный без присваивающих.
- (лок, Т-9) en.po:1027/2478, ru.po:1367 — описание КР жёстко пишет «extends the buff by 1 turn»: цифра стала данными (`{AdditionalDurationAmount}` — бесплатный плейсхолдер), текст теперь опровержим правкой json.
- (знать) `RegisterDefault(Duration, 1)` в КР — мёртвая лгущая строка из аудита, уйдёт проходом А-1c (а-дрейф ×11).

## Из Х-3 (2026-08-20, коммит `6c29f029`; accept)

- **(лок, в Т-9)** `Effect_Seal_Of_Oblivion`: `_Description` (en.po:1184) «passive skills are disabled» (= док), а `_Tooltip` (en.po:2673, ru.po:2025) «нельзя лечить» — тултип не от этой печати и ни от одной из пяти; один из текстов переписать.
- (лок, Т-9) У 5 из 10 «сильных» эффектов нет русского имени: `Effect_Burning_Fury` — пустой msgstr, `Effect_Primal_Fury`/`Effect_Healing_Fury`/`Effect_Evade_First_Death`/`Effect_Heal_Reduction` — записей в ru.po нет вовсе.
- (знать) `Dispel` в продакшене по-прежнему без вызывающих — сила эффектов пока ни на что не влияет; фундамент под предметы/способности с развеиванием.
- (nit) `EffectCanonTests.s_designList` не несёт колонку «Сила» — второй независимой копии доковской силы нет (пин держит только `EffectPowerTests`); расширять кортеж — отдельный заход.

## Из Х-2 (2026-08-20, коммит `f1ad6c31`; accept with minors → возврат закрыт)

- **(задача-кандидат, тест-инфра)** `CombatRandomScope`/`CastRandomScope` (`src/Testing/BattleSystemTests/CombatRandomScope.cs:18,23`) сажают генератор теста в СТАТИКИ процесса (`CombatRandom.Source`/`CastRandom.Source`) — при параллельных классах MSTest чужой бой получает spy соседа (23 красных при сдвиге расписания). Сейчас погашено `[assembly: DoNotParallelize]` (`src/Testing/TestExecution.cs`, цена ~0с); настоящая починка — сиденье ролла в контексте боя, не в статике; после неё файл удалить. Примечание: заодно защитило тест транзитивности `DotPotencyFromCanonTests` (мутирует общий реестр).
- (знать, рантайм) `Decline` завершённого повторяемого квеста прогоняет `onDecline`-действия каждый раз — `GiveItem` там ограничен только аудитом данных, рантайм-реестр уникумов живёт в `TurnIn`.
- (nit) `UniqueItemQuery.IsUnique(null)` бросит из `GetValueOrDefault` вопреки доку «False for an id no catalog claims» (сегодня недостижимо — id инициализированы).
- (nit) Tracker пишется только на выдаче уникума; пропуск на повторе молчит — «квест не дал меч» разбирается по отсутствию строки.
- (nit) `ForgedRewardId = "Ornament_Tier_1"` прибивает мутационный тест к поставляемому id (падёт громко при переименовании).
- (nit, вкусовщина) Два `||` в правиле уникума против списка видов (прецедент `ItemMinter._kinds`).
- (окружение, названо исполнителем) `SharedData/Items/Items.json` (секции quest/common) не соответствует DTO `ItemDataList` — `Coal`, похоже, не грузится вовсе (TODO в `Main/Services/ItemDataProvider.cs:51`); безвредно, пока Coal только в HasItem/TakeItem. `Main/Services/ItemDataProvider.cs:62` — `CopyItem` на неизвестный id бросает `ArgumentNullException` (текст «Item not found», неверный тип) прямо в `TurnIn` при битом id награды — аудит ловит на данных, рантайм бросает.

## Из инцидента 2026-08-20 (владелец прочитал null как баг)

- (док, одна строка) `DamageOverTurnEffect.s_pools`: `null`-компонент у Poison чуть не был «починен» на `DamageType.Poison` (обнулило бы пул тика от обычных ударов → по реш. 15 яд перестал бы ложиться). Переписать док-строку словаря явно: «Poison кормится ВСЕЙ суммой удара — решение владельца 2026-08-18 (п.6 и E-1 п.7), не пропуск»; ср. мину Ф0-1 про шесть тестов на бестиповом пуле.

## Из ревью А-1a (2026-08-20, коммит `406f11de`; accept with minors)

- (minor, лок — в Т-9 или владельцу) `en.po:2487`/`ru.po:1379` — описание Банки яда обещает полный `{Damage} + скейлы` КАЖДЫЙ ход, реальный тик = ×0.35 канона (расхождение было и при 0.7, фикс его усилил до ×2.86); тултип самого эффекта показывает правду — игрок видит оба числа рядом. Минимум — переформулировать «a share of …»; полный вариант — отдать долю в `DescriptionValues` способности тем же чтением канона.
- (minor, садовый проход) Третья копия формы «Dictionary(Ordinal) → RecordProperties → CreateEffect» (`DamageOverTurnEffect.FromCanon`, `AbilityProvider.FuryFromCanon` :410/:413, инлайны реестра) + литерал `"duration"` теперь в трёх написаниях — свести extension `CreateEffect(this IEffectProvider, string id, params (string,float)[])` в Core + общие ключи.
- (nit) `DotPotencyFromCanonTests.cs:143-150` — имя кейса говорит «канон не несёт строки», меряет ветку «реестр не знает фабрики»; переименовать в духе `AStatusTheRegistryBuildsNothingFor…`.
- (знать) Отказ `FromCanon` СТРОЖЕ соседа `FuryFromCanon` (тот глотает и отказ реестра через `??`) — осознанно по DoD «громкий отказ»; фраза отчёта исполнителя «в точности ответ песочницы из FuryFromCanon» неточна в тексте, не в коде.
- (знать, тест-инфра) Тест транзиивности `DotPotencyFromCanonTests` мутирует общий скомпонованный реестр с восстановлением в TestInitialize/TestCleanup — при включении параллелизма MSTest его придётся изолировать (в проекте `[assembly: Parallelize]` нет).

## Из техбатча (2026-08-19, коммит `e90a1308`; accept с доработкой)

- ~~(БАГ) `AugmentPcMultiStackOnHit`: мёртвая подписка, легендарка не делает ничего~~ — ЗАКРЫТО Б-189 `0a80424e` (#189): класс снят, семантика «стак за каждого ЖИВОГО врага поля в момент удара» — в `PoisonCoatingEffect` через `DelegateAugment`-флаг; сторона счёта и добивание запинены.
- **(minor, пинцет ведомости кусает себя)** Сверки «декларация ⊆ ведомость» обе читают одну рукописную строку, а не тело фабрики: мутация «фабрика двигает другой ключ при прежней декларации» оставляет весь свод зелёным. Нужен зонд «применили запись → какие ключи реально сдвинулись».
- (вопрос дисциплины) 8–11 записей с `fitsAnyAbility` не вправе описываться как принадлежащие названной способности (2 из 5 замечаний ревью — ровно эта ошибка): завести гард «док не называет способность рядом с fitsAnyAbility-записью» или оставить дисциплиной ревью?
- (nit) Сцепка мифика привязана к типу `CritCalculationBuff`, а не к шву `PrimaryBuffFactory` (шов без потребителя) — появится replace-аугмент, мифик потеряет хозяина. Плюс несцепленность держится на том, что `CritCalculationBuff` не переопределяет `IsStronger` (иначе переналожение сняло бы мифик) — допущение нигде не закреплено.

## Из фикса дюпа сумки (2026-08-19, коммит `5314d092`; rework → закрыт)

- (инвариант мешка) `SortBag` сливает стаки под первый item — записи остальных инстанций остаются в реестре `_itemInstances` без слота (предсуществующее; аугментов не касается — MaxStackSize 1). Обещание «реестр отвечает о том, что сумка реально несёт» глобально не выполнено.
- (вопрос архитектуры, при первом касании) Мешок держит две правды: реестр (`GetItem`) и слоты (`GetContents`) — они снова сходятся, но могут разойтись; кандидат — поднять бухгалтерию мешка в Godot-свободный Core-класс, чтобы дорога Main наконец попала под тесты (Main вне графа тестов — пины дюпа ходят по тестовому двойнику).
- (nit, контракт) `IInventory.ItemAmountChanges` — первый аргумент не описан; `Crafting/Internal/Inventory/InventoryWindow.cs:63` читает его как instanceId (фактически item id).
- (владельцу, ассеты) 97 иконок аугментов отсутствуют в `Data/Shared/Assets/Icons/` — 5635 варнингов за день, ~90% объёма log.txt; иконки в работе владельца — при задержке можно приглушить варнинг до одного на id.

## Из разбора лут-симуляции (2026-08-19, коммит `497fbe41`; rework → закрыт)

- (класс «порядок словаря → взвешенный выбор», вне лут-дропа) `TraderService.cs:189/197` (выбор блюпринта из `_blueprints.Values`) и `:199/:206` (полосы редкости); крафт: `EquipItemPoolExtensions.cs:26-29` (`UsedRequiredResources.Keys` → весовой выбор в `ItemUpgrader:77`), `CreateEquipItemRequestHandler.cs:48`; `ConvertAugmentsRequestHandler.cs:93`. Латентно (порядок вставки стабилен), закрывать сортировкой при первом касании.
- (шире, сим vs игра) `FileSystemDataSource` сортирует файлы по пути, `GodotDataSource` обходит подкаталоги раньше файлов — многофайловые каталоги могут склеиваться в РАЗНОМ порядке у симуляции и игры → весовые полосы отчёта не обязаны совпадать с игровыми. Кандидат: выровнять порядок в самих источниках.
- (знать) Два skipped в обычном прогоне = перегон отчёта за опт-ином + давний Inconclusive аудита локализации; не искать поломку.

## Из довер-проверки Ф3 (2026-08-19, коммит `4303c365`; accept)

- (nit, тест) Связка «пассивка ↔ форма вызова CreateReaction» не покрыта: перестановка аргументов внутри CounterAttackPassiveSkill не уронит ничего (пассивки гейтятся нативным Context.Rnd — в тест-хосте не гоняются); пины воспроизводят точные формы вызовов.
- (условие срабатывания) `AbilityTrace` едет без потребителей (реш. 25) — включить чтение «своего» в день, когда появится запись с явным «свой» в дизайн-строке.
- (nit) Смешанная гранулярность в ActivityOf: MovesOf — по id записи, RidersOf — по инстанции копии; закон «две копии — один вердикт» держится, асимметрию назвать в xml-доке.
- (знать, сборки) «0 предупреждений» инкрементальной сборки — артефакт; полная `--no-incremental` даёт ~60 предсуществующих (HEAD 59 → волна 58). Счёт предупреждений мерить только полной сборкой.
- (знать, счётчик серии) `AttackSeriesWindow.OwnerAttacks` считает ответный удар владельца атакой окна (гейт обрыва серии) — осознанно другой счёт, нежели род импакта; при жалобе на обрыв серии от контратаки смотреть сюда.

## Из Ф2 (2026-08-18, коммит `b0cb2bd2`; accept)

- (nit, грант) `Effect_Charge` теперь достижим как грант предмета (фабрика в реестре, `onDetonate: null`) — метка без нагрузки схлопнется вхолостую: молчаливо-бесполезный грант (ср. правило payload-обязателен у скилл-фабрик). 
- (nit, ростер форм) Слепые пятна сканера (сегодня не задеты): вложенный вызов в аргументе прочтётся как Plain; форма-переменная невидима; `EnumerateFiles` нерекурсивен — подпапка в Effects/ станет невидимой; чтения `.Authored` мимо множителя ростер не сторожит (одно легальное — цена Ярости, запинено).
- (nit, идиома) Подстановка дефолта `== 0f ? X : value` (Curse/SlownessSeal/OnEdge/DoT/PoisonCoating) молча переписывает законно-авторский ноль в дефолт и прячет дефолт из сигнатуры.
- (nit) `SlownessSeal` floor избыточен практически (`StartCooldown` усекает через `(int)`), расхождение видно лишь при сложении дробных вкладов.

## Из ревью тег-задачи и Ф0-4 (2026-08-18, коммиты `ce53e8ac`/`bd2d2c56`; оба accept)

- **(владельцу, на глаза)** `s_knowinglyFree` вырос 3→5: пять способностей получают эффективность от `Add_Effectiveness_Reduce_Stacks` БЕЗ платы срезом стаков (односторонний подарок); в журнале решений санкционирована только плохая сделка «Продления оглушения» — этот класс не озвучен.
- (minor, страж/ulp) `StageGuardLayer` теперь возвращает пересобранную пропорцию `ScaledTo(allowed)` — сумма может разойтись с `allowed` на ulp, а порог перехода стадии строится тем же выражением; практически недостижимо (источников Blight нет, допуски тестов 0.001), лечится точным суммированием последнего слоя.
- (minor, тест) Проба `s_hostDeclaredFamily` захардкожена под `AppliedDurations` — второй участник-семья получит молча неверный ответ; просится `id → Func<IAbility,int>` по образцу `s_openedKeys`. У `s_hostDeclaredFamily`/`s_selfContained` нет обратного гарда «участник действительно такой».
- (nit) `(type & BypassingTypes) != 0` уносит весь компонент при будущей составной маске; ветка `Unruled` недостижима и не покрыта; ссылки на строки в CombatLayers промахнулись на 1 (плюс §2.4 `ApplySuppression:80-120` протух ранее); док `EffectGenera` «это все рода» против накладываемых shield/barrier (прикрыто тем, что носители несут и buff).

## Из ревью Ф0-2/Ф0-3 (2026-08-18; оба accept)

- **(владельцу, симуляция — решение: разбор ПОСЛЕ цикла эффектов) Monte-Carlo лута недетерминирован при фиксированном сиде**: перегенерированный `LootSimulationReport.md` при том же сиде 20260710 разошёлся со старым в босс-сценариях (Baseline_Boss 406.03→405.61 и др.) при нетронутом лут-пайплайне; Baseline_Regular совпал до цифры. Подозрение — порядок/параллелизм сценариев обвязки. Обесценивает правило «баланс лута только с перегоном отчёта» — требует отдельного разбора.
- (Ф2, сторона Dispel) `ChargeEffect` (класс нейтрален, но кладётся только на врага) и семья Ярости (само-размен) — решить сторону баф/дебаф поимённо в проходе «сила поимённо»; четыре Ярости должны решить одинаково.
- (nit) `ResolveAttackOutcome` конвертирует `context.Rnd.Randf` в Func — аллокация делегата на каждую атаку (копеечная, горячий путь — знать).
- (nit) Ф0-2: `Dispel` пока без вызывающих в продакшене (фундамент — потребители появятся предметами/способностями позже); void-возврат — предмет вида «лечит за каждый снятый» потребует возврата снятого. Дефолт `Power` в самом IEffect — Weak: реализация мимо базового Effect молча слаба (сегодня базовый — единственный).
- (nit) Дефолт енама `AttackResults` = Evaded бессмыслен после возврата вердикта значением — при случае None/Succeed.
- (знать, RNG-стрим) Осознанные сдвиги при сравнении прогонов: ChainAttack не жжёт бросок на уклонении; нулевой шанс не проходит нигде (крит/побег/стадии босса/реакции/контратака/дар богини).

## Из ревью Ф0-1 «пул DoT по родам» (2026-08-18, коммит `b47d741d`; accept)

- (nit, гард) `AbilityProvider.Behaviours.Extra()` отвергает `poolFromWholeHit` по ключу `BehaviourField.ImpactKind`, а не по фактическому потреблению флага — новое поведение с ImpactKind, флаг игнорирующее, пройдёт гард молча.
- (nit) `DamageSnapshot.s_empty` — изменяемый Dictionary в роли общего пустого часового; `FrozenDictionary` или сравнение `_components is null` выразительнее.
- (nit, тест) `TheStackKeepsTheCasterItWasLaidBy` почти тавтологичен (хранимое поле никто не пересчитывает) — заморозку пула держит слабо; упадёт только при рефакторинге в вычисляемое свойство.
- (nit, тесты) Ядовитые колл-сайты в адаптированных тестах обёрнуты `Of(Physical, X)` — верно, пока пул яда бестиповый; станет типизированным — шесть тестов молча начнут мерить другое.
- (владельцу, баланс-проход) Кровь теперь урезается конверсиями `PhysicalToCold/Lightning`/`AttackSacredConversion` (тик от Physical-компонента, который конверсия съедает) — на конверт-билдах тик крови теряет большую часть; обнулений нет. Плюс числа Burning/Bleeding-пассивок под новую базу пулов никто не пересматривал.

## Из ревью ревизии айди/привязок (2026-08-18, коммит `2afe82f4`; accept)

- ~~(minor, к волне D — контракт райдеров) Снятие аугмента не отписывает райдер~~ ЗАКРЫТО 2026-08-19 (Ф3): `Detach()` в контракте `IImpactRider`/`IActivationRider`, единственная дорога снятия — `Ability.RemoveImpactRider`/`RemoveActivationRider`, отписаны `BuffAfterAttacksImpactRider` и найденный по дороге второй течец `TransferPoisonOnDeathRider` (ждал смерти каждой задетой цели и держал замыкание, пока она не умрёт). Пин — `RebuildingTheBuildLeavesNoOrphanedListener`.
- (nit, тест) `TheTallyCarriesFromOneCastToTheNext` не поймает возврат сброса по `impact.Source.CastId`: `series.Execute` не зовётся, `CastId` весь прогон `""`. Дострожить при случае.
- (знать, следствие решения «счёт через бой») С N-й успешной атаки бафф пары перекладывается КАЖДОЙ последующей атакой: длительность обновляется, стаки добираются до `maxStacks: 3`, и так до конца боя — заметно сильнее прежней покастовой выдачи. Осознано владельцем при выборе боевого счёта; ребаланс — его балансным проходом.

## Из Godot-прогона Todd (2026-08-18, логи)

- (шум в логах, косметика) На завершении каждого боя тройка WRN «Handler not found» (PlaybackSpeedChangedEvent ×1, TurnStartEvent ×2): `BattleEventBus` в конце боя сносит реестр целиком (`_registry.Clear()`, BattleEventBus.cs:18), после чего подписчики (BattleDirector.Teardown и др.) штатно зовут Unsubscribe по уже пустому реестру — `EventRegistry.Remove` ворнит. Не утечка — симптом двойной уборки; идёт с 2026-07-12, сотни записей. Лечится либо отказом от массового Clear, либо тишиной Remove после Clear.

## Из волны CL (2026-08-15, верификация CL-5)

- (nit, float) Интерполированная ступень хранится в сейве сырой (`Augment_Multicast` Rare = 0.45000002) — функционально идентично (гарды с толерансом), видно только если тултип напечатает сырое число; лечится округлением интерполированной ступени при минте. Авторские лестницы не затронуты (не интерполируются).

## Из волны CL (2026-08-14, верификация CL-4)

- (ведомости) Четыре инертные НАТИВНЫЕ посадки fury-записей (2 записи × Покров/Арес по тегу `health`) не значатся ни в одной литеральной ведомости: `s_reach` — только общие ключи, дар-гард — только дарёная дорога; запись приватного ключа, путешествующая тегом, не залеждена в обе стороны. Завести ведомость класса или расширить существующую (волна E).
- (UI/лок, владельцу отмечено) «Регенерация» — имя и аугмента, и эффекта; развести при желании одной строкой msgstr.
- (nit ревью) `AbilityAugmentReduceParameter`: две доли на одном параметре — соперники, не сложение (Priority.Base) — при росте фабрики помнить.

## Из волны CL (2026-08-14, верификация CL-3a)

- (nit, §4f) Проза «вне канона 21 id» — фактически 22 в списке, и свип находит ещё непокрытые (в т.ч. `Effect_Additional_Hit_Chance_Buff`, `Effect_Attack_Chance_Calculation_Buff`); машинные гарды точны, врёт только перечисление — пересчитать или пометить «не исчерпывающий».
- (nit) Ростер форм: «ровно в одном списке» обеспечивается косвенно (HashSet дедупит; класс в двух списках валится другим свипом) — косметика имени.
- (предсуществующее) `SilentFuryPassive` строит `SilenceSeal` конструкторными дефолтами мимо канона и реестра.
- (владельцу, при случае) Не-урон расхождения доков (Армагеддон hpCost 0.5 vs 30%; порог казни 15 vs 42; яд Покрытия 0.45/5 vs канон 0.35/4; стаки Покрова 4 vs 5; урон Банки, которого нет в доке) + опечатка дока «1115%» (прочитано 115%) + Цепная молния подбита при статусе «Не реализовано».

## Из сверки каталога с дизайн-доком (2026-08-11, файл «Аугменты — вне дизайн-дока.md»)

- (волна E / лок-проход) **Имена msgstr пусты почти у всех записей каталога** — заполнены только у пяти; в игре показывается ключ. Названия в Obsidian-файле — рабочие, при заполнении .po брать оттуда.
- (волна E) **Полный дубль**: `Augment_Buff_Duration` ≡ `Augment_Increased_Buff_Duration` (оба Т1, [buff, duration], +1 общий `Duration`) — слить или развести по величине/тиру (та же болезнь, что пара More_Attack_Damage/Damage_Multiplier).
- (лок) `Augment_Fury_Duration`: описание в .po обещает «+длительность Ярости», код — Subtract (запись-цена, решение владельца) — текст переписать.
- (лок) `Augment_Increasing_Scales` судится тегом [damage] по всей книге, а описание говорит «Скейлы яда» (наследие Банки) — на чужих посадках вводит в заблуждение.
- (id, при случае с сейв-ломкой) Опечатка `Augment_Reduce_Execution_Trahsold` → Threshold (G-5).
- (дизайн-док, владельцу) «Эффективность поглощения» дословно повторяет «Лечение при получении удара» (копипаст?); две соседние позиции Т1 названы одинаково «Перезарядка + Стоимость».
- (волна E, обратная сторона — список не составлялся) ~14 позиций дока без записей каталога: агрегаты длительности/эффективности эффектов, «Объем щита», «Заряды», «Кровавая серия атак», «Мифический расчет» и др.

## Из волны C (2026-08-10, верификация C-2b)

- **(Godot-прогон владельца, глянуть)** `Effect_Instant_Restore` (восстановление Двойного удара) не заведён в .po: эффект мгновенный (вошёл-вылечил-вышел), но публикует `EffectAppliedEvent`/`EffectRemoved` — если HUD/боевой лог реагируют, игрок может увидеть безымянную иконку на кадр.
- (nit) `InstantRestoreEffect.Apply` проверяет только `Target == null` против шаблона `!IsApplied || Target == null` остального свода — отступление верное (отвергнутый стак обязан вернуть ресурс), но без строки комментария будет «починено» при следующем чтении.
- (nit) `PorcupineBuffEffect` зовёт `Effective(healOnHitPercent)` дважды за ответный удар (гард и сумма) — конструктор чистый, вреда нет, но форма та же, что вычищена из райдера.
- (nit) `DeferredEffectActivationRider`: `if (onTarget == null) return;` внутри foreach выходит из всего цикла, читается как continue — защитимо (null = отказ фабрики), но глазу спорно.
- (минтер, предложение исполнителя C-2b — решить при случае) Не роллить свойства, накрытые `propertyRefs`: бросок не жжётся, сейв не носит мёртвое число; одна проверка в `AugmentMinter.Rolled`.

## Из волны C (2026-08-10, верификация C-0)

- (волна C) Гард `HoldsAScalableFigure` видит композит СЛУЧАЙНО: `AresBlessingEffect.Copy()` ссылается на параметры первичного конструктора, потому компилятор захватывает их полями `EffectValue`; композит, чей `Copy()` их не трогает, окажется невидим. Спускаться в `CompositeParameterChangeEffect.Changes` явно.
- (волна C/E, при первом владении) Четыре эффекта вне обеих семей всё ещё считают руками: `Weakness` (`1 - value`), `SorceryGiftEffect` (`1 - value`), `CloudedMindEffect` (`1 + value`), `OverloadChargeEffect` (`1 + multiplier`). Ни один сегодня не кладётся владельцем `Effectiveness` — режим отказа тишина, не переворот знака; переводить на `EffectValueShape` при первом владении их способностей.
- (nit) `Effect.Effectiveness` с `protected set` шире, чем нужно единственному потомку — явный `StampFrom(context)` был бы точнее открытого сеттера.
- (nit) Второй проход гарда требует ХОТЯ БЫ ОДНО масштабируемое число — частично немасштабированная нагрузка проходит; ужесточение — вопрос волны E.

## Из волны B (2026-08-10)

- (волна E, из ревью B-3) `Porcupine.DamageReturn` ↔ `IceAegis.ReflectPercent` — единственное найденное совпадение понятий («доля полученного урона обратно»); сегодня недостижимо (`Augment_More_Damage_Return` прибита к Дикобразу, тег `armor` носит только он). Кандидат в общий ключ при первой записи.
- (из верификации B-1, одна строка дока) Гард `AugmentSharedKeyReachTests` ходит по `_parameterAugments`; четыре рукописных класса на общих ключах в таблице не значатся (`PcUpgradeIncreaseDuration`, `PcUpgradeAdditionalPoisonDuration`, `PeUpgradeTotalDamageMultiplier` — прибиты abilityId, охват тривиально 1/0; `AugmentPoisonOnHit` одалживает, не двигает). Строка в доке таблицы превратит пропуск в решение.
- (волна C) Инвариант многоходовых записей гарда намеренно не видит `CostValue` (регистрируют все — «работает» всегда): записи, БЕРУЩИЕ ману на инертных посадках (`Augment_Extend_Stun_Add_Cost` — 11 посадок со счётом без товара, `Augment_More_Stacks_More_Cost` — 9), описаны прозой в комментарии, но не утверждены. Существование инварианта не означает, что этот класс покрыт.
- (волна C, каталог) `Augment_More_Attack_Damage` и `Augment_Damage_Multiplier` — идентичные списки досягаемости (2 рабочих / 8 инертных) на одном ключе с разной величиной: дубль, а не выбор — слить или развести по величине/тиру.
- (волна E, теги) `Augment_Extend_Stun_Add_Cost` не достаёт Армагеддон — единственный из 4 владельцев `StunDuration` вне обоих списков (теги не пересекаются); дотянуть тегом способности или записи. `Ability_Static_Armor` инертен на `DamageMultiplier`-записях: тег `damage` есть, множителя не объявляет (его урон — детонация).
- **(волна C, долг)** Тройка длительности на Ядовитом покрытии: именная `Ability_Pc_Augment_Increase_Duration` стала строго избыточным дублем общей `Augment_Buff_Duration` (одна идентичность, равная величина — ничья по порядку посадки). По правилу G-5 — слить/снять именную при генерализации записей.
- (к волне C) TrackNotFound при посадке декоратора на незарегистрированный ключ: развести «опечатка» и «штатная инертность» (с общими ключами инертных посадок стало 5–9 на запись).
- (nit) `FreezeDuration` (DeepFreeze+IceAegis) — двойник без записи; объявить общим при первой записи «длительность заморозки». `SecondDamage`-тройка Армагеддон/DoubleStrike — совпадение написаний без дизайн-строки.
- (nit, B-cap) Дубль дефолта 3 в DTO и EffectRules (конвенция дома, на балансном проходе); `ExtensionBudget` не на `IEffect`; возврат `Extend` никем не читается.

## Из волны A (2026-08-09/10)

- **(вопрос дизайна → волна D-2)** Два ОДИНАКОВЫХ подателя на одной способности (через орнамент): сегодня райдер дедупится по id поведения — вторая копия не ставит второго райдера (2 копии ≠ 2 стака за импакт), снятие любой уносит общий райдер; ЧИСЛА при этом не уезжают (счётчик одалживаний). Пин `TwoCopiesOfOneApplierRideAsOne` фиксирует текущее; менять ли на пер-копийные райдеры — решение соперничества волны D.

- (к волне C) Порядок `InstallUpgrades` детерминирован тирами (усилитель t1 садится раньше подателя t3) → `AddDecorator` репортит `TrackNotFound` на каждый `Bind()` легального билда; отличать «спящий декоратор» от опечатки в ключе, либо регистрации раньше декораций.
- (к волне D, UI) Карточка аугмента печатает `{poisonDuration}` из записи (3), а способность с усилителем кладёт 4 — плейсхолдеры должны читать декорированное значение.
- (nit) `TryRegister`/`Unregister` не поднимают `ParameterChanged` при смене `Keys` — заметно станет, когда одолженный ключ попадёт в текст описания.
- (развести в плане D-1) `ExtendPoisonOnHitRider`: «чей яд продлевать» — след способности (D-1), но ЧИСЛО продления одалживается механизмом A-3 уже сейчас — не дать D-1 проглотить готовую часть.
- (из ревью A-2) `SplashRandomTargetRider` сам бьёт цель без импакта — вопрос контракта райдеров; `IpDamageRandomTargetStrategy` без пина (нужен живой аттак-пайплайн); отписка не в finally (предсуществующее).
- (волна D) Слой эффектов не отчитывается импактами: `StaticArmorEffect` (детонация), `PoisonCoatingEffect` (яд на атаках), `IceAegisEffect` (эффект на атакующего) — эффект не держит инстанцию способности, срабатывает в чужие ходы. `PorcupineBuffEffect` инстанцию ДЕРЖИТ — его причина другая.
- (мёртвое) `DarkShroud/DefaultExecutionStrategy` — NotImplementedException, ноль ссылок.

## Из ревью P-08 (линейка испытаний, 2026-08-09; rework закрыт — оставшееся сюда)

- **(владельцу, баланс)** Разрыв лестницы Elit lvl 8 → Unique lvl 25: третья ступень «чуть плотнее медведь», четвёртая «другая игра» (2800 HP + 800 барьера, Mastermind, 5 способностей). Промежуточная ступень (Ghoul/Skeleton_Warrior) или просадка уровня через authored — если это не сознательная «стена».
- (знать, к месту ноды) Трупы зверей-целей встают нежитью по таймеру (у Wolf/Direwolf/Bear нет lifecycle-секции), Undead враждебна людям; ревенант враждебен сразу — `Trial_Grounds` не вплотную к деревне; `Npc_Human_Merchant` цикл нежити не переживёт.
- (к P-05, структурно) Ферма целей закрыта данными (Cooldown 24h), но структурной защиты нет: идемпотентный спавн или деспавн цели при отказе — код.
- (nit) `tier: 1..4` в записях линейки инертен (читает только QuestOfferRollCondition); ширина баннера-комментария ≠ конвенции файла.
- (знать) `Kill_Count:<npcId>` считает киллы записи где угодно (retroactive выключен — только после старта стадии); сегодня теоретика — целей нет в ростерах точек.

## Из ревью P-23 (сейв диких живых, 2026-08-09; rework — major/minors закрываются возвратом)

- (nit) `Restore`: `GetAllModifierIds()` аллоцирует List на каждую alive-запись + линейный Contains — HashSet один раз.
- (nit) `"modifiers": []` в каждой записи файла — `DefaultValueHandling.Ignore`.
- (владельцу, при случае) Alive-запись возвращает «того же, но не совсем»: витал не пишется (раненая цель встаёт целой), способности перекатываются, InstanceId новый (личная репутация к цели забыта). Пока — задокументированная политика; поле health добавляется дёшево, если станет заметно.
- (знать) Дикий живой, умерший и восставший нежитью, пишется как `risen` и теряет квестовые модификаторы (тела перекатывают — принятое решение).
- (знать) DebugConsole-спавн диким не помечается — отладочные NPC не персистятся (как и было).

## Из ревью P-06 (очки дерева за квесты, 2026-08-09; accept with minors — №1/№2 возвратом, №3 сюда)

- **(minor, решение владельца)** Выдача очков при открытом окне мастерства/колеса не обновляет счётчик до следующего клика (у бюджета дерева нет события; то же давно верно для левелапа в бою). Дешёвая правка: `PassiveTreeService.SetTotalPoints` при фактическом изменении поднимает `AllocationChanged` — тогда и левелап начнёт освежать окна. Сценарий «колесо открыто во время сдачи квеста» — насколько реален? В бою окно дерева не открывается в принципе, при сдаче квеста тоже. 
- (nit) `MasterySaveParticipant`: `CurrentLevel - BonusLevel` при живом `EarnedLevel`.
- (nit) `amount: 99999999999` проходит гейт Integer и падает OverflowException (спасает try/catch парсера; прямой вызов фабрики бросит).
- (nit) Битая запись награды = квест едет БЕЗ награды (конвенция словаря действий; «громко» только в логе).
- (важно для P-08/P-09) Повторяемый квест выплатит очки снова, а с P-09 и орнамент — кодом не держится ничем. Держит это ДЕФОЛТ ТИПА: `bool Repeatable` в `QuestsData`, и поля `repeatable` не пишет ни один квест в `Quests.json` (проверено: 0 вхождений) — то есть в данных флага не видно вовсе, и опереться на его чтение при ревью нельзя. Гарантия «выдаётся один раз» существует ровно до первой записи `"repeatable": true` в линейке.
- (nit) Стенд `PassiveRespecTests` принимает `AddBonusPoints(<=0)` без отказа — контракт стендом не отражён.

## Из ревью P-05 (SpawnNpcAction, 2026-08-09; accept with minors — все minor закрыты возвратом)

- (nit) Дубль `PointId` в реестре разрешается первым попавшимся (задокументировано); `GetAllModifierIds()` строит List на каждый Execute (O(n·m), на этих размерах неважно).
- (nit, знать) Спавн синхронный — оба соседа мутируют дерево отложенно (`CallDeferred`); сегодня до Execute не доходит ни один батловый колбэк, но первый же счётчик убийств из батлового колбэка превратит это в AddChild посреди боя.
- (следствие, знать) Цель испытания на Villager-цикле не умирает окончательно → держит слот `ReserveOutsideLimit` до сброса сессии.

## Из ревью P-21 (щеколда каталогов, 2026-08-09; accept)

- (minor) `ShippedCatalogTests` — фильтр `IsLiteral` молча теряет константу при переводе в `static readonly` (тернарник `GetRawConstantValue()/GetValue(null)` принимает обе формы).
- (minor) Доккоммент переобещает «checked where the game reads it from» — гуард меряет только общий корень, локальный оверрайд каталога даст ложный красный (самообъясняющийся). Дописать фразу.
- (nit) `IsInitOnly: false` в паттерне избыточен; первый тест-класс в корне Testing — при втором гуарде завести папку; `SharedData.Root()` без кэша (36 обходов ФС).
- (вне задачи) Обратный гуард «папка в SharedData без имени в DataCatalog» не покрыт (сегодня это легальные Assets/Localization).

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

## Из ревью сейв-кластера (полка/земля, 2026-08-25)

- (реальное окно, решить с владельцем) Сейв в раздачу лута после боя молча теряет неразлитые предметы: `BattleContext.RunBattleAsync` гасит `IsFighting` в `finally` ДО публикации game-bus `BattleEndEvent`, весь асинхронный цикл раздачи (~0.25–0.45 с × N дропов) идёт при разрешённом `CanSave`, а предметы в этот момент в локальном `pending` — ни в `_itemsOnGround`, ни в кэше. Входы: SaveLoadWindow, DebugConsole save. Лечится гейтом «раздача идёт» в CanSave или флашем pending при капчуре — трогает контракт SaveGameService, потому не в minors.
- (minor) `EnsureFreshState` в TraderService: ветка `|| NextRestockMinutes == double.MinValue` мертва (`NowMinutes >= MinValue` истинно всегда) — наследство; поведение верное (стокуется по общему условию), ветку можно снять при следующем касании.
- (minor) `NowMinutes` при `clock == null` = 0: сейв в композиции без часов и загрузка с часами (и наоборот) неверно датируют дедлайны рестока — проблема сборки, не секций.
- (nit) `TraderOfferSaveData.Item` вкладывает `InventoryItemSaveData` без `[JsonProperty]` — в файле смешение "offerId" и "Amount"/"Equip"; нормализация отклонена ведущим (косметика ценой касания формата сумки).
- (инфо) `RegisterProjectSaveSections` резолвит `IGroundItemStore` в `CreateProvider()` — `LootOrchestrator` теперь строится в статическом инициализаторе `GameServiceProvider.Instance`, не лениво; вреда нет (ctor только подписывается на шины, подписка раньше), принято.
- (владельцу) Висячий untracked `src/LootGeneration/Source/Assets/ItemOnGround.cs.uid` (0 байт): сам `.cs` живёт уровнем выше со своим uid — файл ждёт перескана/удаления в редакторе Godot.

## Из ревью #211 (эффективность-стат, 2026-08-26)

- (владельцу, баланс) Курьёз Жертвы: `SacrificeChargeEffect` классифицирован как Debuff (IsHarmful, едино с диспелом — вражеский диспел снимает заряд) → ручка «+X% эффективности дебафов» усиливает игроку ЕГО СОБСТВЕННОЕ усиление от Жертвы, а баф-ручка на неё не действует. Механика последовательна, но игрок читает строку как «сила проклятий» — решить при балансе, жить с курьёзом или разводить род для ручек от рода для диспела (цена: два определения рода).
- (minor, фитиль) Бонус-стек клонируется ПОСЛЕ пайплайна: `DamagePerTick` клона уже с ручкой, а поле `Effectiveness` перештампуется каст-значением без ручки (бонус-стек пайплайн пропускает). Сегодня безвредно — единственный источник бонус-стеков BurningStacks = DoT, а DoT после установки тика Effectiveness не читает. Станет багом, когда бонус-стеки появятся у не-DoT эффекта.
- (инфо) `IEffect.Genus` — default interface member: читается только через интерфейс, наследник не может переопределить род без реимплементации. Все читатели сегодня интерфейсные; свойство конструкции, не долг.

## Из ревью волны конверсий (2026-08-27)

- (minor, виталы) Текущая мана при Агностике (MaxMana=0) остаётся «повисшей»: `Player.OnParameterChanged` на смену максимума только публикует событие, текущее не клампится вниз. Тратиться не может (стоимость форсирована в Health), деления на ноль нет, но HUD и читатели текущей маны видят пул, который кейстоун объявил исчезнувшим. Предсуществующий контракт виталов, не волна; поправить при касании виталов — кламп текущего к новому максимуму или явное правило.
- (tech-debt) `IDamageComponent` (`src/Core/Entity/Components/IDamageComponent.cs`) мёртв целиком после сноса `CalculateForBase`: ноль реализаций, ноль потребителей, единственное упоминание — само объявление. Снести при следующем касании каталога компонентов.
