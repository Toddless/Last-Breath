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
Цепочка каста: **поза кастера** (клип `Ability_<Id>` со спрайта бойца, `PlaceholderFrames.tres`) → **полёт/аура** → **импакт на цели** → **hurt+цифры**. Стадии 2–3 — данные: `AbilityVisualConfig` (Resource: `AbilityId`, `Delivery` = None/Projectile/Chain/InstantOnTarget/SelfAura, `TravelClip`, `ImpactClip`, `TravelSpeed`, `Scale`; пустой клип = стадия пропускается) в `AbilityVisualLibrary` (`Internal/_Placeholders/Anim/AbilityVisuals.tres`, назначена арене `_visualLibrary`). Исполнитель — `AbilityVfxPresenter` (арена создаёт динамически, якоря = споты). `Chain` играет хиты последовательно в порядке записи (молния, прыжки банки). Серии атак конфиги НЕ используют (их хиты без `CastId` — анимируются битами Fight_Attack). Фикс: looped-клипы ждут расчётную длительность, не `animation_finished`.

**План расширения системы проигрывания (следующая итерация визуалов):**
- **Пер-стадийные клипы мультикаста**: уровень активации 4 должен быть существенно «сочнее» уровня 1 — конфиг получает клипы по стадиям (напр. `StageClips`/суффикс `<клип>_Stage3`); стадия уже известна презентации (`AbilityStageActivatedEvent` публикуется, но в таймлайн-аккорд не пробрасывается — прокинуть).
- **Разные анимации от апгрейдов**: выбранный апгрейд меняет вид (глобальный Poison_Explosion, прыгающая банка) — ключ конфига id+вариант либо способность публикует «визуальный вариант» в `AbilityActivatedEvent`.
- **Разные анимации от кастующего**: тот же id способности у разных существ выглядит по-разному — выбор конфига с учётом кастера (переопределения per-entity поверх дефолтного).
- Мелочи: дуга полёта (банка — парабола), смещение точки спавна, звук каста/импакта в конфиге (стык с аудио-планом).

## Battle Log (сделано)
Лента в реплей-времени (battle bus): урон с разбивкой по типам/цветам + крит, смерть, уклонение/блок, лечение, активации, наложенные эффекты (**новое combat-событие `EffectAppliedEvent`** из `Effect.Apply`, только принятые стаки), пропуск хода, маркеры ходов. `BattleLogPresenter` (весь текст/BBCode) + `BattleLog` контрол (буфер 150, фильтры по категориям, сворачивание). Директор дополнен republish-битами `AttackEvaded/AttackBlocked/EffectApplied` (готовые точки для будущих анимаций уклонения/блока). Пассивки в логе — только через их урон; полноценно = ввести `PassiveTriggeredEvent`.

## Dev tools
`Battle/Internal/Tools/`: `NpcSpawnTool` (нода: `_world`=MainWorld, `_count`, `_stats` — список `NpcStatSpec` Resource в инспекторе, хоткей B) + `NpcGenerator` — спавн группы NPC (1 vs N через обычный подход игрока).

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
