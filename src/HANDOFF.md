# LastBreath — контекст для продолжения работы

Godot 4.7 C# (.NET 9, LangVersion preview: primary constructors, `field`). Пошаговая RPG, соло-разработчик Todd, общение на русском.
Проекты: **Main** (LastBreath), **Battle**, **Crafting**, **LootGeneration** — отдельные Godot-приложения; **Core** — общая библиотека (ссылается на Godot SDK и MS DI); **Utilities** (Tracker, Calculations); **Testing**. Общий код шарится симлинками (`*/Source`, Core); `*/Internal` — приватное для проекта.

## Правила работы и формулы
Перенесены в `CLAUDE.md` (авто-подгружается всегда). Там: код-стайл, работа с файлами, формулы урона/крита, описание систем.

## Боевая архитектура: replay-модель (сделано, работает)
Логика резолвится МГНОВЕННО (await'ы анимаций удалены из Attack/TakeDamage/Execute), презентация проигрывает запись.

- **Два временных домена:** per-entity `CombatEvents` (`ICombatEventBus`) = момент резолва, для логики; `IBattleEventBus` (per-battle) = момент показа, для UI. UI-события в battle bus публикует ТОЛЬКО BattleDirector при проигрыше.
- **`BattleTimeline`**: упорядоченный лог; `SubscribeAll` catch-all ДО типизированных (причинный порядок); арена attach'ит бойцов в `PrepareBattleArena`. Незнакомые типы событий директор пропускает — новые combat-события безопасны.
- **Снапшоты:** `VitalsSnapshot` в Damage/Healed/AbilityActivated событиях. Презентация НИКОГДА не читает живое состояние.
- **`BattleDirector`**: очередь битов, аккорды по `CastId`, смерть скипает биты мертвеца, упавший бит не стопорит очередь.
- **Гейт хода:** `WaitForPresentationAsync` после каждого хода + финальный.

## Таргетинг и цикл выбора цели (сделано, работает)
- `AbilityTargetType { Enemy, FewEnemies, Self, Ally, FewAllies }` (JSON `targetType`+`maxTargets`) → `ITargetingStrategy` (Core) + стратегии `SingleTarget/FewTargets/Self/RandomTargets` + `TargetingStrategyFactory`; `Ability.Targeting` (сеттер — апгрейды могут подменять).
- **`TargetSelectionController`** (Battle): фаза выбора бесплатна/отменяема, подсвечивает валидные споты; одиночная цель коммитит по клику, `Few*` — тоггл до N + подтверждение, Self/random — авто. **Единый commit = `AbilityActivationEvent`** (публикует контроллер, исполняет BattleArena через `TakeCommittedTargets`). События: `PlayerSelectingTargetForAbilityEvent` (старт), `AbilityTargetPickedEvent`, `ConfirmSelectionEvent`, `CancelSelectionEvent`, `TargetSelectionResolvedEvent` (сброс кнопки).
- `EntitySpot`: подсветка — плейсхолдер-кольцо в `_Draw` (красный=атака, голубой=цель способности, зелёный=выбран).
- **Модель хода:** способности свободны внутри хода; ход завершает базовая атака (клик по врагу) или конец хода. Кнопки «конец хода без атаки» в турн-лупе пока НЕТ.
- `AbilityButton`: стейты Ready/Charging/SelectingTargets/NotAvailable; кулдаун-число + затемнение, отдельный тинт нехватки ресурса; **Charging** = удержание (0.5с/стадия, стадия на кнопке) для `IChargedAbility`, отпускание → выбор цели.

## Райдер-архитектура способностей (сделано)
Модель: **3 delivery-семейства + общий райдер-контракт**.
- **Райдеры:** `IActivationRider` (раз на каст, экс-PostActivation; в `Ability.ActivationRiders`) и `IImpactRider` + запись `AbilityImpact(Caster,Target,Field,Succeeded,IsCritical,Damage)` (на каждый импакт; `Ability.ImpactRiders`, диспатч `ApplyImpactRiders` — public). В сериях атак райдеры бьют по **каждой owner-атаке окна** (доп. атаки от реакций тоже — задумка «каждый хит серии = атака способности»).
- **Delivery:** (1) AttackSeries — `AttackContext`+scheduler, `AttackModifierPipeline` (пре-мутаторы: Unevadable/FirstCrit/Accuracy...); (2) HitSequence — `HitDelivery/` `SelectedTargetsHits`/`BouncingHits`/`AllEnemiesHits` (JoP, Армагедон); (3) `MulticastAbility<TPlan>` — интеллект, кумулятивные стадии, план живёт один каст (`CastPlan`-волей, `ChainPlan`, `AegisPlan`, `OverloadPlan`, `IceBlockPlan`).
- **Generic-апгрейды:** `AbilityUpgradeImpactRider`, `AbilityUpgradeCastEffect` (deferred-эффект на каст), `AbilityUpgradeCostTypeOverride` (мана→здоровье), `DelegateUpgrade`, per-parameter апгрейды (`SacUpgradeParameter` и т.п.).
- **Enablers:** `AbilityExecutedEvent` (конец Execute — окно Activated→Executed для «следующая способность...»), `CompositeParameterChangeEffect` (+`ParameterChange`), `RawAccuracy` в `IAttackContext`, `IgnoreResistances` в `IDamageContext`+Calculations, `OperationType.Override` (был), `IsEvadable` дефолт true (фикс: серия прерывается уклонением).
- `IChargedAbility` (`MaxStage/MaxAffordableStage/PendingStage`) — канал стадии UI→Execute свойством.

## Способности: ГОТОВЫ ВСЕ ТРИ СТОЙКИ (компилируются; ловкость протестирована, сила/интеллект — нет)
- **Ловкость (7):** SoA, IP, JoP, DarkShroud, CriticalCalculation, PoisonExplosion, PoisonCoating. JoP на HitSequence+Impact-райдерах; IP: L2+L3 теперь композируются (были конфликтующие подмены стратегии).
- **Сила (7):** HeadButt (стан, L3 два выпада), DoubleStrike (пер-ударные числа/дебафы, L2 точность, L3 баф при двух попаданиях / хилы), AresBlessing (композитный HP+реген, L3 через каст-эффекты), BerserkFury (hp-gated серия + Fury, L3 подмена Fury-фабрикой, L2 Override CostType), Sacrifice (заряд АКТИВАЦИЯМИ: 100 HP=+1% чистым, `PureDamageBonusModifier` в окне каста; L3 FreeCast/хил/−25% вход.), Porcupine (возврат урона, L3 Echo через `TemporarySkillEffect`), Armageddon (charged: стадии 5/15/30% maxHP, даунгрейд по HP, ст.3 стан; L3 AoE=AllEnemiesHits+КД).
- **Интеллект (5):** IceShards (эталон-волей), IceBlock (стан+увядание+доп.глыбы; без апгрейдов по спеке), Overload (сжигание маны→урон, ст.4 Pure), ChainLightning (ChainPlan: прыжки/спад/×2 последний; L3 игнор резистов), IceAegis (барьер 300+35×INT; **разрушение барьера отслеживает сам `IceAegisEffect`** — ст.4 Freeze всех; остаток сгорает).
- **Новые эффекты:** DamageBuff, ArmorBuff, IncomingDamageReduction(+Modifier), WitheringCurse (−15% maxHP), Freeze (близнец Stun), SacrificeCharge, FreeCast (+`NoCooldownActivationModifier`), PorcupineBuff, AresBlessing, IceAegis.
- **Багфиксы попутно:** `Copy()` двойная инверсия (Clumsiness/Blind/Weakness — ломала значение при копировании райдерами), `EchoPassiveSkill.OnTurnEnds` (отложенный урон никогда не наносился), SoA применял атак-модификаторы ПОСЛЕ резолва.

## Эффекты в UI (сделано)
`EffectsComponent.GetEffectViews()` + событие `EffectsChanged` → сущности публикуют `EffectsChangedEvent(target, views)` → HUD диффит по Id: одна иконка на эффект, счётчик стаков, длительность (макс среди стаков). `IEffect` из UI убран. Старые `EffectAddedEvent/EffectRemovedEvent` больше не публикуются.

## VFX способностей (сделано, каркас)
Цепочка каста: **поза кастера** (клип `Ability_<Id>` со спрайта бойца) **∥ эффект активации** (`CastClip` на споте кастера, параллельно позе) → **полёт/аура** → **импакт на цели** → **hurt+цифры**. VFX-стадии — данные: `AbilityVisualConfig` (Resource: `AbilityId`, `Delivery` = None/Projectile/Chain/InstantOnTarget/SelfAura, `CastClip`, `TravelClip`, `ImpactClip`, `TravelSpeed`, `Scale`; пустой клип = стадия пропускается) в `AbilityVisualLibrary` (`Internal/_Placeholders/Anim/AbilityVisuals.tres`, назначена арене `_visualLibrary`). Исполнитель — `AbilityVfxPresenter` (арена создаёт динамически, якоря = споты). `Chain` играет хиты последовательно в порядке записи (молния, прыжки банки). Касты без прямого урона (банка = чистый DoT) берут цели VFX из `EffectAppliedEvent` аккорда. Серии атак: атаки несут `SourceAbilityId` (AttackContext → DamageContext) — бит урона играет `ImpactClip` способности вместе с hurt (полёт для атак не используется). Фикс: looped-клипы ждут расчётную длительность, не `animation_finished`.

**План расширения системы проигрывания (следующая итерация визуалов):**
- **Пер-стадийные клипы мультикаста**: уровень активации 4 должен быть существенно «сочнее» уровня 1 — конфиг получает клипы по стадиям (напр. `StageClips`/суффикс `<клип>_Stage3`); стадия уже известна презентации (`AbilityStageActivatedEvent` публикуется, но в таймлайн-аккорд не пробрасывается — прокинуть).
- **Разные анимации от апгрейдов**: выбранный апгрейд меняет вид (глобальный Poison_Explosion, прыгающая банка) — ключ конфига id+вариант либо способность публикует «визуальный вариант» в `AbilityActivatedEvent`.
- **Разные анимации от кастующего**: тот же id способности у разных существ выглядит по-разному — выбор конфига с учётом кастера (переопределения per-entity поверх дефолтного).
- Мелочи: дуга полёта (банка — парабола), смещение точки спавна, звук каста/импакта в конфиге (стык с аудио-планом).

## Battle Log (сделано)
Лента в реплей-времени (battle bus): урон с разбивкой по типам/цветам + крит, смерть, уклонение/блок, лечение, активации, наложенные эффекты (**новое combat-событие `EffectAppliedEvent`** из `Effect.Apply`, только принятые стаки), пропуск хода, маркеры ходов. `BattleLogPresenter` (весь текст/BBCode) + `BattleLog` контрол (буфер 150, фильтры по категориям, сворачивание). Директор дополнен republish-битами `AttackEvaded/AttackBlocked/EffectApplied` (готовые точки для будущих анимаций уклонения/блока). Пассивки в логе — только через их урон; полноценно = ввести `PassiveTriggeredEvent`.

## Dev tools
`Battle/Internal/Tools/`: `NpcSpawnTool` (нода: `_world`=MainWorld, `_count`, `_stats` — список `NpcStatSpec` Resource в инспекторе, хоткей B) + `NpcGenerator` — спавн группы NPC (1 vs N через обычный подход игрока).

## NPC AI (сделано, фазы 1–3)
Архитектура: **HFSM (Stateless, добавлен в Core) + Utility AI**, ядро — чистый C# в `Core/Ai`, Godot — тонкие адаптеры. Данные: `Battle/Data/Npc/Npc.json` (NPC: статы, стойки, world, lifecycle) + `Battle/Data/NpcBehaviors/NpcBehavior.json` (архетипы по стойкам: Dex=агрессивный, Str=защитный, Int=смешанный) → `NpcProvider` (Battle DI) роллит `NpcDefinition` (стойка/уровень/редкость/способности) → `BaseNpc.ApplyDefinition`. Маппинг `EntityType` (enum — источник правды): Regular=Обычный(15), Special=Редкий(25), Elit=Элитный(35), Unique=Специальный(50), Boss(80), Archon(150) — Boss/Archon только вручную.

- **Боевой планировщик** (`Core/Ai`): `UtilityTurnPlanner` — цикл «скоринг→каст→переоценка» до `maxCastsPerTurn`, затем базовая атака (модель хода: способности бесплатны). `CastAbilityAction` берёт цели через `ITargetingStrategy` способности (мимо TargetSelectionController — тот UI игрока), исполняет через `ICombatEnvironment` (реализует BattleArena; ветка не-игрока в `RunBattleAsync`). Considerations: добивание/самосохранение/бюджет маны; `AiIntellect` (Simple/Tactical/Mastermind — явное поле), temperature-шум, charged-стадии = MaxAffordable×Greed. NPC без профиля — старый fallback (атака игрока).
- **Мозг мира** (`Core/Ai/World`): `WorldBrain` — FSM Calm/Suspicious/Alert/Search: активности (Idle/Wander/Patrol — маршрут = Node2D-дети в `NpcSpawnTool._patrolRoute`), шум (`WorldStimulusEvent` в GameEventBus, старт боя публикует), погоня с leash от дома, поиск, grace после боя. `BaseNpc` = `IWorldAgent`: движение прямой линией c MoveAndSlide (шов под NavigationAgent2D), зрение поллингом `IPlayerAccessor` (LoS-рейкаста нет), бой стартует физическим контактом (мозг доводит).
- **Цикл нежити** (`NpcLifecycle`): HP=0 → тело в мире (BattleContext возвращает всех); не-нежить → таймер 1–10 мин (JSON `lifecycle`) → восстаёт нежитью (Fraction→Undead, Increase-модификаторы ∝ таймеру, source `UndeadRising`, зелёный Modulate-плейсхолдер, `NpcFactionChangedEvent`); нежить → анабиоз (Dormant). Клик ЛКМ по телу рядом с игроком (150px) = сжигание → `NpcFinalDeathEvent` → QueueFree. Легаси-NPC без данных при смерти QueueFree (раньше утекали сиротами).
- **Спавн**: `NpcSpawnPoint` (нода: npcIds/maxCount/радиус/группа/respawnDelay; слушает FinalDeath/FactionChanged — сожгли → замена, восстал → восставший дикий, точка спавнит замену) + `NpcPopulationService` (глобальный лимит, дефолт 20; Release по FinalDeath — дикие тоже освобождают слот).
- **Фикс шин**: `BattleEndEvent` публиковался только в game bus — подписчики боевой шины (Player/NPC FSM, AbilityButton, CombatTextPresenter) были мёртвые; второй бой ронял Stateless(«Fight из Fight»). Теперь `BattleContext.RunBattleAsync` публикует и в локальную боевую шину. В Main-проекте аналога нет — при оживлении Main добавить.
- **Тесты**: `Testing/BattleSystemTests` (планировщик, мозг, lifecycle) — чистый C#, Moq/фейки, без Godot-рантайма.

### AI: фракции и NPC vs NPC (сделано)
- **Фракционные отношения**: `RelationLevel` (7 уровней Ненависть…Союзничество; эффекты цен/наград/входа — TODO), `IFactionRelationService` + `FactionRelationService` (Battle/Source/Npc, `Data/Factions/FactionRelations.json`). Матрица НАПРАВЛЕННАЯ (нежить враждебна демонам, демоны нейтральны ко всем; гномы↔эльфы Неприязнь), same-faction=Alliance, unspecified=Neutral. Отношение игрока per-фракция динамическое (`SetPlayerRelation`+событие; дефолт Undead=Hatred). Зрение NPC отдаёт только врагов: `hostileToPlayer` в world-секции (бандиты) ИЛИ стенд фракции ≤ Вражда.
- **NPC vs NPC авторезолв (скирмиши)**: `Core/Ai/World/Skirmish` — `SquadStrength` (уровень/редкость/тип/численность только добавляют шансы), `NpcSkirmish` (3 броска d20+Сила, бросок каждые 30–120с, ничья раунда → сила → монетка), `NpcSkirmishService` (экспансия групп, IsFighting-заморозка, исход) + `NpcWorldRegistry` (NPC-зрение без физики) + **нода `NpcWorldDirector` — ОДНА в сцене мира, тикает сервис** (будущий дом A-Life). Проигравшие умирают по своим lifecycle; живые победители сжигают нежить с шансом 0.6. События `NpcSkirmishRoundResolvedEvent` (хук под анимации атак с паузами 5–10с при игроке рядом — не реализовано) и `NpcSkirmishEndedEvent`. Зрение NPC видит враждебных NPC (матрица, обе стороны) → мозг преследует → контакт 90px → скирмиш.
- **NPC-модификаторы перенесены в Core и подключены к спавну**: реализации (база `NpcModifier` + 8 конкретных, `NpcModifiersFactory`, `NpcModifiersComponent`) теперь в `Core/Components/NpcModifiers` (локализация — напрямую `TranslationServer.Translate`); парсер — `NpcModifiersParser` (Core, выпилен из DataParser/IDataParser); общий `NpcModifierProvider` — в Utilities, регистрируют оба проекта (дубликаты в LootGeneration/Battle удалены). `Npc_Modifiers.json` скопирован в `Battle/Data/NpcModifiers/` (двойное хранение данных с LootGeneration — при изменении синхронизировать!). Спавн: кол-во = тип (Regular 1…Archon 6) + бонус редкости (Epic/Legendary +1, Mythic/Unique +2) в `NpcTypeDefaults.ModifierCount`, weighted-ролл без повторов в `NpcProvider.RollModifiers` → `NpcDefinition.Modifiers` → `NpcModifiers.AddModifiers`. ВАЖНО: модификаторы сейчас влияют только на лут (difficulty/budget/гарантированные предметы); парам-бафы по `NpcBuffId` («некоторые NPC непобедимы») ещё не имеют потребителя — отдельная задача.
### AI: время суток + расписания (сделано)
- **`WorldClock`** (`Core/Ai/World/Time`, чистый): игровые сутки = `realMinutesPerGameDay` реальных минут (дефолт 24 → 1 мин = 1 час), `Day/Hour/Minute/MinuteOfDay/NormalizedTimeOfDay`, фазы `DayPhase` (Night 22–06/Morning 06–10/Day 10–18/Evening 18–22, границы в `Battle/Data/World/WorldClock.json`), события `HourPassed`/`PhaseChanged`. Рантайм — `GameWorldClock` (Battle/Source/World, грузит JSON), DI `IWorldClock`. Тикает `NpcWorldDirector` (он же публикует `WorldHourChangedEvent`/`WorldPhaseChangedEvent` в GameEventBus). Время идёт в бою, останавливается с паузой игры. Сейвов нет — время сессионное.
- **Расписания NPC**: секция `schedule` в world (`[{from:"22:00", to:"06:00", activity, wanderRadius?}]`, слоты через полночь ок, дыры → fallback на базовую `activity`) → `ScheduledActivity` — обычная `IWorldActivity`, переключающая под-активности по `MinuteOfDay` (мозг не изменён; тревога/погоня прерывают расписание как любую активность). Фабрика `WorldActivityFactory` (общая для мозга и слотов). Данные: скелет ночной (день Idle / ночь Wander 400), бандит дневной. Локации слотов («к кузнице») — ждут smart-точек из бэклога, формат расширяем полем `location`.
- **Визуал**: `DayNightTint` (CanvasModulate, лерп к цвету фазы) — добавить ОДНУ ноду в сцену мира рядом с `NpcWorldDirector`.

### AI: бэклог (согласовано 2026-07-06)
- Потребитель `NpcBuffId` — парам-бафы модификаторов («некоторые NPC непобедимы»): провайдер бафов по id + применение при AddModifier.
- Распределение глобального лимита NPC по фракциям (сейчас общий счётчик + пер-поинт).
- Мирный слой: активности быта (костёр, торговая точка, «работа», smart-точки), диалог и торговля (`IPeacefulNpc`), эффекты уровней отношений игрока (вход в поселения, цены ремесленников, награды квестов — по таблице Todd в `RelationLevel`).
- Бегство/капитуляция из боя и бегство мирных от опасности (стимул `Danger` зарезервирован); учесть начисление опыта за бой с бегством.
- Поддержка союзников в бою (хилы/бафы на группу NPC), consideration «цель уже дебафнута».
- Фазы боссов (скрипты по порогам HP поверх планировщика; Boss/Archon вручную).
- A-Life offline-тик (NPC вне экрана) + персистентность состояния мира (ждёт систему сейвов).
- Территории/агро-зоны фракций («чужак в периметре лагеря — агрессия, даже если нейтрален», охрана периметра); частично покрыто leash+спавн-поинтами.
- LoS-рейкаст зрения и NavigationAgent2D (ждут слоёв коллизий/навмеша), walk-клипы, группа-лидер в мире (следование, решения лидера).

## Обсудили, но НЕ сделали
- Кнопка «конец хода» без атаки (модель хода это предполагает, в турн-лупе не реализована).
- Общий AttackSeries-слой: продублированные циклы серий у SoA/IP/HeadButt/DoubleStrike/Berserk не консолидированы (только `AttackModifierPipeline`); Aggregate-триггер райдеров (6/9 ударов SoA) остался стратегиями — выделять при 3-м потребителе.
- Приведение `CastPlan.OnHitRiders` (волей) к общему `IImpactRider`.
- Расширение VFX-конфига (см. план в секции «VFX способностей»): пер-стадийные клипы, вид от апгрейдов, вид от кастующего.
- `PassiveTriggeredEvent` для лога/визуалов срабатывания пассивок.
- Ускорение проигрыша по числу битов; `IUIWindowPositionStorage`→DraggableWindow; `ChangeHud(PlayerHud)` после боя; restore-пайплайн ресурсов; effect-application пайплайн; battle-start хуки; связка MartialArtMastery с окном изучения.
- Нода `AbilityVfxPresenter`, добавленная в `BattleArena.tscn` руками, кодом не используется (арена создаёт свой экземпляр динамически) — удалить ноду или перевести код на неё.

## Требует внимания
- **Синхронизация Main-копий (пользователь):** `Main/DamageContext.cs` (+`IgnoreResistances`), `Main/Player.cs`/`Main/Npc/BaseNpc.cs` (EffectsChanged-репаблик), `Main/Components/EffectsComponent.cs` (GetEffectViews; файл к тому же битый нуль-байтами — ломает сборку Main).
- **Ассеты/локализация:** иконки 11 новых id (`Head_Butt, Double_Strike, Ares_Blessing, Berserk_Fury, Sacrifice, Porcupine, Armageddon, Ice_Block, Overload, Chain_Lightning, Ice_Aegis`) + строки имён/описаний.
- **Придуманные дефолты (в JSON, подкрутить):** горение Армагедона L2 (3 стака/3х/0.7), «уничтожение брони» L3 — временное (3 хода, −100%), Fury-варианты Берсерка (множители), дебафы HeadButt/DoubleStrike, freezeDuration Эгиды (1).
- **`MaxBarrier` клампит грант Эгиды** (база NPC 1000) — решить, должен ли эффект поднимать MaxBarrier.
- Заряд Жертвы в HUD без числа длительности (живёт активациями) — нужно ли отображение зарядов; хил L3 считается от pre-mitigation суммы.
- Fury жжёт HP на ВСЕХ атаках владельца (вкл. базовые) — дизайн эффекта, проверить ощущение.
- Не чинил (пре-существующее): `PorcupinePassiveSkill` — `Source = attacker` (само-атрибуция) и нет фильтра причин (риск пинг-понга двух дикобразов); `AccuracyBuff` передаёт value в Multiply без (1+v) — подозрительно.
- `FewEnemies/FewAllies` таргетинг реализован, но данных-потребителей нет; `Poison_Explosion` = Enemy, «глобальность» апгрейдом (при переводе Pe на HitDelivery).

## Аудио (не начато)
План прежний: шины `Master → Music, SFX, UI`; дакинг компрессором; автолоад `AudioManager` под DI; звук = презентация (триггер из `BattleDirector`: `AbilityActivatedEvent` → `<id>_cast`, `DamageTakenEvent` → `<id>_hit`). Плейсхолдеры: `Battle/Internal/_Placeholders/Audio/` (по 2 WAV на 8 старых способностей + generic), VFX-листы и каст-позы там же.
