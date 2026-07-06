namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Utilities;

    /// <summary>
    /// Turns replay-time battle events into ready battle-log lines (BBCode). The single place that
    /// knows how domain events read as text — the log control only renders entries.
    /// Feeds off the battle bus, so lines appear in sync with what is SHOWN, not with the resolve.
    /// </summary>
    public class BattleLogPresenter
    {
        private const string PlayerName = "Игрок";
        private const string UnknownName = "Противник";
        private const string CritMark = " [color=#ffd75e][b]КРИТ[/b][/color]";

        private static readonly Dictionary<DamageType, (string Name, string Color)> s_damageTypes = new()
        {
            [DamageType.Pure] = ("чистый", "#ffd75e"),
            [DamageType.Physical] = ("физ.", "#d0d0d0"),
            [DamageType.Fire] = ("огонь", "#ff7043"),
            [DamageType.Cold] = ("холод", "#6ec6ff"),
            [DamageType.Lightning] = ("молния", "#b39dff"),
            [DamageType.Poison] = ("яд", "#7ccb64"),
            [DamageType.Burning] = ("горение", "#ff9d45"),
            [DamageType.Bleed] = ("кровь", "#e05555"),
        };

        public event Action<BattleLogEntry>? EntryAdded;

        public BattleLogPresenter(IBattleEventBus bus)
        {
            bus.Subscribe<TurnStartEvent>(OnTurnStart);
            bus.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
            bus.Subscribe<DamageTakenEvent>(OnDamageTaken);
            bus.Subscribe<EntityHealedEvent>(OnHealed);
            bus.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
            bus.Subscribe<AttackBlockedEvent>(OnAttackBlocked);
            bus.Subscribe<EffectAppliedEvent>(OnEffectApplied);
            bus.Subscribe<TurnSkippedEvent>(OnTurnSkipped);
        }

        private void OnTurnStart(TurnStartEvent evt) =>
            Emit(BattleLogCategory.System, $"[color=#909090]Ход: {Name(evt.StartedTurn)}[/color]");

        private void OnAbilityActivated(AbilityActivatedEvent evt) =>
            Emit(BattleLogCategory.Ability, $"{Name(evt.Caster)} применяет [color=#8fd4e8]«{evt.Ability.DisplayName}»[/color]");

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            var context = evt.Context;
            string parts = string.Join(" + ", context.DamageComponents
                .Where(component => component.Value >= 1)
                .Select(component => Colored($"{component.Value:0} {TypeName(component.Key)}", TypeColor(component.Key))));
            if (parts.Length == 0) return;

            string crit = context.IsCrit ? CritMark : string.Empty;
            Emit(BattleLogCategory.Damage, $"{Name(context.Source)} наносит {Name(evt.Target)}: {parts}{crit}");
            if (evt.Vitals.IsDead)
                Emit(BattleLogCategory.Damage, $"[color=#e05555]{Name(evt.Target)} погибает[/color]");
        }

        private void OnHealed(EntityHealedEvent evt) =>
            Emit(BattleLogCategory.Heal, $"[color=#7ccb64]{Name(evt.Healed)} восстанавливает {evt.Amount:0} HP[/color]");

        private void OnAttackEvaded(AttackEvadedEvent evt) =>
            Emit(BattleLogCategory.AttackResult, $"[color=#b8b8b8]{Name(evt.Context.Target)} уворачивается от атаки {Name(evt.Context.Attacker)}[/color]");

        private void OnAttackBlocked(AttackBlockedEvent evt) =>
            Emit(BattleLogCategory.AttackResult, $"[color=#b8b8b8]{Name(evt.Context.Target)} блокирует атаку {Name(evt.Context.Attacker)}[/color]");

        private void OnEffectApplied(EffectAppliedEvent evt)
        {
            string source = evt.Caster.IsSame(evt.Target.InstanceId) ? string.Empty : $" от {Name(evt.Caster)}";
            Emit(BattleLogCategory.Effect, $"{Name(evt.Target)}: [color=#c9a0e8]«{evt.Effect.DisplayName}»[/color]{source}");
        }

        private void OnTurnSkipped(TurnSkippedEvent evt)
        {
            string cause = (evt.Cause & StatusEffects.Freeze) != 0 ? "заморожен" : "оглушён";
            Emit(BattleLogCategory.Effect, $"{Name(evt.Fighter)} пропускает ход ({cause})");
        }

        private void Emit(BattleLogCategory category, string text) => EntryAdded?.Invoke(new BattleLogEntry(category, text));

        private static string Name(IFightable fighter)
        {
            if (fighter is IPlayer) return PlayerName;
            if (!string.IsNullOrEmpty(fighter.DisplayName)) return fighter.DisplayName;
            return string.IsNullOrEmpty(fighter.Id) ? UnknownName : Localization.Localize(fighter.Id);
        }

        private static string Colored(string text, string color) => $"[color={color}]{text}[/color]";
        private static string TypeName(DamageType type) => s_damageTypes.GetValueOrDefault(type, (Name: type.ToString(), Color: "#ffffff")).Name;
        private static string TypeColor(DamageType type) => s_damageTypes.GetValueOrDefault(type, (Name: type.ToString(), Color: "#ffffff")).Color;
    }
}
