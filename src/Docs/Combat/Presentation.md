# Воспроизведение боя, анимации и VFX

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- Воспроизведение боя (replay-модель): логика резолвит ход МГНОВЕННО, события пишутся в BattleTimeline (catch-all с per-entity CombatEvents). **BattleDirector** — единственное место «как событие выглядит»: проигрывает записи «битами» с реальным темпом и в момент показа РЕПУБЛИКУЕТ событие в BattleEventBus — весь UI (бары, цифры, лог) реагирует на показ, не на резолв. Событие без хендлера скипается молча; новый визуал = один хендлер в CreateBeatHandlers. Аккорды: события одного каста группируются по `CastId` (штампуется на DamageContext'ы) и играют единой фразой; смерть режет биты мертвеца, но НЕ сам бит смерти: падение — полноценный бит, держит очередь на длину клипа `Dead` (`DeathBeat.HoldSeconds`), поэтому конец боя не обгоняет анимацию; турн-луп ждёт `WaitUntilIdleAsync` (гейт хода — ждёт ОПУСТОШЕНИЯ очереди, а не только текущего насоса). UI НИКОГДА не читает живое состояние — только снапшоты из событий (VitalsSnapshot).
- Анимации бойцов: `AnimationsComponentBase` (Node с экспортом спрайта, копии в Main/Battle) → две реализации за одним контрактом. Клипы по конвенции имён: `Fight_Attack`/`Fight_Hurt`/`Dead`/`Stunned`/`Idle_*`/`Walk_*`, каст-поза = Id способности. **Выбор реализации — по факту арта, на каждое имя**: настоящая анимация = многокадровый клип (`TweenAnimationRules.IsAuthoredMotion`) → `AnimationsComponent` играет клип; одиночный кадр, пустой или отсутствующий клип → `TweenAnimationsComponent` (создаётся дочерней нодой при первой нужде) анимирует ТВИНАМИ на дочернем спрайте: выпад/отдача/опрокидывание/покачивание/каст-pulse 0.5с + дыхание на фейсингах, `Idle_Right` = `Idle_Left` с FlipH. Тело твины не двигают никогда (им владеет BattleDirector). Правила имён, честные длительности и идемпотентность падения — Godot-free `Core/Entity/Components/TweenAnimation*`. Клип забирает спрайт себе (`Suspend`: твины гасятся, трансформ и зеркало в rest). `PlayAnimationAsync`: зацикленный клип → ждём расчётную длительность (кадры/FPS), НЕ animation_finished (иначе подвес); твин ждёт таймером на свою длину.
- VFX способностей: цепочка каста = поза кастера ∥ CastClip (эффект активации на споте кастера, параллельно позе) → полёт/аура → импакт → hurt. VFX-стадии — данные: `AbilityVisualConfig` (Delivery: None/Projectile/Chain/InstantOnTarget/SelfAura; CastClip/TravelClip/ImpactClip — пустой клип = стадия пропускается) в `AbilityVisualLibrary` (.tres, назначен арене), исполняет `AbilityVfxPresenter` (спавн AnimatedSprite2D, полёт твином между спотами). Chain играет хиты последовательно в порядке записи; касты без прямого урона берут цели VFX из EffectAppliedEvent аккорда. Серии атак: атаки штампуют `SourceAbilityId` на AttackContext→DamageContext — бит урона играет ImpactClip способности (полёт к атакам не применяется). Новая способность = клип в VfxFrames + конфиг в библиотеке, код не трогается.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Battle/Source/Presentation/BattleDirector.cs`, `Core/Entity/Components/TweenAnimationRules.cs`.

## Связанные документы

- [Боевой урон и события](../Combat/CurrentSystem.md)
- [Активные способности](../Abilities/CurrentSystem.md)
- [Окна, HUD и всплывающие элементы](../UI/CurrentSystem.md)
- [Карта документации](../README.md)
