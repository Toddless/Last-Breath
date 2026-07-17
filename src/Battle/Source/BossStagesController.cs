namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;
    using Effects;

    /// <summary>
    /// Battle-scoped driver of boss stages (the "stages" section of Npc.json): for every attached
    /// staged fighter it watches the owner's DamageTakenEvent. Crossing the stage threshold makes
    /// the boss damage-immune AT ONCE (the "finish your turn" cue, BossStageTransitionStartedEvent);
    /// the transformation executes on the NEXT TurnStartEvent of the battle bus — ALL effects are
    /// stripped (immunity and the player's debuffs alike, by design), the next stage is applied and
    /// vitals are fully restored (BossStageChangedEvent). The stage's on-attack effects ride the
    /// owner's AfterAttackEvent (the entity-level attack pipeline never runs for basic attacks) and
    /// are rewired on every stage switch. The rage threshold grants one-time Increase modifiers
    /// that die with the battle. Pure C# — testable without Godot.
    /// </summary>
    public sealed class BossStagesController : IDisposable
    {
        private const string RageModifierSource = "BossRage";

        private static readonly EntityParameter[] s_rageParameters =
            [EntityParameter.Damage, EntityParameter.CriticalChance, EntityParameter.AdditionalHitChance];

        private readonly IRandomNumberGenerator _rnd;
        private readonly Func<bool> _isBattleActive;
        private readonly IBattleEventBus? _battleEventBus;
        private readonly List<Entry> _entries = [];

        public BossStagesController(IRandomNumberGenerator rnd, Func<bool> isBattleActive, IBattleEventBus? battleEventBus)
        {
            _rnd = rnd;
            _isBattleActive = isBattleActive;
            _battleEventBus = battleEventBus;
            _battleEventBus?.Subscribe<TurnStartEvent>(OnTurnStart);
        }

        /// <summary>Wires the fighter's stages if it has any; everyone else is a cheap no-op.
        /// Safe for latecomers — the arena calls it from TryJoinBattle as well. A risen boss
        /// arrives already standing in its final stage: only its attack effects get wired.</summary>
        public void TryAttach(IFightable fighter)
        {
            if (fighter is not IFightableNpc npc || npc.Stages.Count == 0) return;

            var entry = new Entry(npc, npc.CurrentStageIndex);
            Action<DamageTakenEvent> handler = _ => OnDamageTaken(entry);
            entry.DamageHandler = handler;
            npc.CombatEvents.Subscribe(handler);
            WireAttackEffects(entry);
            _entries.Add(entry);
        }

        public void Dispose()
        {
            _battleEventBus?.Unsubscribe<TurnStartEvent>(OnTurnStart);
            foreach (var entry in _entries)
            {
                if (entry.DamageHandler != null) entry.Owner.CombatEvents.Unsubscribe(entry.DamageHandler);
                UnwireAttackEffects(entry);
                // Rage lasts "until the end of the battle" by design — a surviving boss calms down.
                if (entry.RageTriggered) entry.Owner.ParameterModifiers.RemoveModifierBySource(RageModifierSource);
            }

            _entries.Clear();
        }

        private void OnTurnStart(TurnStartEvent evnt)
        {
            foreach (var entry in _entries)
            {
                if (entry.PendingStage is not { } nextStage) continue;
                if (!entry.Owner.IsAlive) continue;
                ExecuteTransformation(entry, nextStage);
            }
        }

        private void OnDamageTaken(Entry entry)
        {
            if (!_isBattleActive() || !entry.Owner.IsAlive) return;
            TryStartTransition(entry);
            TryTriggerRage(entry);
        }

        /// <summary>One-way and one-shot: once armed the threshold never re-arms — the immunity
        /// keeps health frozen until the transformation, and the final stage has no threshold.</summary>
        private void TryStartTransition(Entry entry)
        {
            if (entry.PendingStage != null) return;
            if (entry.CurrentStage.NextStageAtHealthPercent is not { } threshold) return;
            if (entry.StageIndex + 1 >= entry.Owner.Stages.Count) return;

            var owner = entry.Owner;
            if (owner.CurrentHealth > owner.Parameters.MaxHealth * threshold) return;

            entry.PendingStage = entry.StageIndex + 1;
            var immunity = new DamageImmunityEffect();
            _ = immunity.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = owner.InstanceId });
            owner.CombatEvents.Publish(new BossStageTransitionStartedEvent(owner, entry.PendingStage.Value));
        }

        private void ExecuteTransformation(Entry entry, int nextStage)
        {
            var owner = entry.Owner;
            entry.PendingStage = null;
            // Deliberate order: strip EVERYTHING (immunity and the player's debuffs alike), then
            // the new stage's parameters and abilities, then the full restore against the new maximums.
            owner.Effects.RemoveAllEffects();
            owner.ApplyStage(nextStage);
            entry.StageIndex = nextStage;
            owner.CurrentHealth = owner.Parameters.MaxHealth;
            owner.CurrentMana = owner.Parameters.MaxMana;
            owner.CurrentBarrier = owner.Parameters.MaxBarrier;
            WireAttackEffects(entry);
            owner.CombatEvents.Publish(new BossStageChangedEvent(owner, nextStage));
        }

        private void TryTriggerRage(Entry entry)
        {
            if (entry.RageTriggered || entry.PendingStage != null) return;
            if (entry.CurrentStage is not { RageAtHealthPercent: { } threshold } stage) return;

            var owner = entry.Owner;
            if (owner.CurrentHealth > owner.Parameters.MaxHealth * threshold) return;

            entry.RageTriggered = true;
            foreach (var parameter in s_rageParameters)
                owner.ParameterModifiers.AddModifier(
                    ModifiersCreator.CreateModifierInstance(parameter, ModifierValueType.Increase, stage.RageBonus, RageModifierSource));
        }

        private void WireAttackEffects(Entry entry)
        {
            UnwireAttackEffects(entry);
            if (entry.CurrentStage.AttackEffects.Count == 0) return;

            Action<AfterAttackEvent> handler = evt => OnAfterAttack(entry, evt);
            entry.AttackHandler = handler;
            entry.Owner.CombatEvents.Subscribe(handler);
        }

        private void UnwireAttackEffects(Entry entry)
        {
            if (entry.AttackHandler == null) return;
            entry.Owner.CombatEvents.Unsubscribe(entry.AttackHandler);
            entry.AttackHandler = null;
        }

        // AfterAttackEvent fires on the attacker's bus for every resolved attack (the
        // PoisonCoatingEffect pattern) — extra attacks and counterattacks included, by design.
        private void OnAfterAttack(Entry entry, AfterAttackEvent evt)
        {
            if (!_isBattleActive()) return;
            if (evt.Context.Result != AttackResults.Succeed) return;

            foreach (var config in entry.CurrentStage.AttackEffects)
            {
                if (_rnd.RandFloat() > config.Chance) continue;
                _ = CreateAttackEffect(config).Apply(new EffectApplyingContext
                {
                    Caster = entry.Owner,
                    Target = evt.Context.Target,
                    Source = entry.Owner.InstanceId,
                    Damage = evt.Context.FinalDamage,
                    IsCritical = evt.Context.IsCritical
                });
            }
        }

        private static IEffect CreateAttackEffect(NpcStageAttackEffectConfig config) => config.Effect switch
        {
            StageAttackEffectKind.Poison => new DamageOverTurnEffect(config.Duration, StatusEffects.Poison, config.MaxStacks, config.DamagePercent),
            StageAttackEffectKind.WitheringCurse => new WitheringCurseEffect(config.Duration, config.MaxStacks, config.DamagePercent),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config.Effect, "unmapped stage attack effect")
        };

        private sealed class Entry(IFightableNpc owner, int stageIndex)
        {
            public IFightableNpc Owner { get; } = owner;
            public int StageIndex { get; set; } = stageIndex;
            public NpcStageConfig CurrentStage => Owner.Stages[StageIndex];
            public int? PendingStage { get; set; }
            public bool RageTriggered { get; set; }
            public Action<DamageTakenEvent>? DamageHandler { get; set; }
            public Action<AfterAttackEvent>? AttackHandler { get; set; }
        }
    }
}
