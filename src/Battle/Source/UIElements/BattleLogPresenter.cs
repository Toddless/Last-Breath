namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Localization;

    /// <summary>
    /// Turns replay-time battle events into ready battle-log lines (BBCode). The single place that
    /// knows how domain events read as text — the log control only renders entries. Texts are
    /// Log_* templates in .po, colors come from TextPalette; values arrive pre-colored, so
    /// templates render Plain (no double styling). Feeds off the battle bus, so lines appear in
    /// sync with what is SHOWN, not with the resolve.
    /// </summary>
    public class BattleLogPresenter
    {
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
            bus.Subscribe<PlayerFleeResolvedEvent>(OnFleeResolved);
        }

        private void OnFleeResolved(PlayerFleeResolvedEvent evt) =>
            Emit(BattleLogCategory.System, TextPalette.Colorize(
                Localization.Localize(evt.Succeeded ? "Log_Flee_Success" : "Log_Flee_Failed"),
                evt.Succeeded ? TextPalette.System : TextPalette.Muted));

        private void OnTurnStart(TurnStartEvent evt) =>
            Emit(BattleLogCategory.System, TextPalette.Colorize(
                Render("Log_Turn", new() { ["Name"] = Name(evt.StartedTurn) }), TextPalette.System));

        private void OnAbilityActivated(AbilityActivatedEvent evt) =>
            Emit(BattleLogCategory.Ability, Render("Log_AbilityUsed", new()
            {
                ["Caster"] = Name(evt.Caster),
                ["Ability"] = TextPalette.Colorize($"«{evt.Ability.DisplayName}»", TextPalette.AbilityName),
            }));

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            var context = evt.Context;
            string parts = string.Join(" + ", context.DamageComponents
                .Where(component => component.Value >= 1)
                .Select(component => TextPalette.Colorize(
                    $"{component.Value:0} {Localization.Localize($"DamageType_{component.Key}")}",
                    TextPalette.DamageColor(component.Key))));
            if (parts.Length == 0) return;

            string crit = context.IsCrit
                ? " " + TextPalette.Colorize($"[b]{Localization.Localize("Log_Crit")}[/b]", TextPalette.Crit)
                : string.Empty;
            Emit(BattleLogCategory.Damage, Render("Log_DamageDealt", new()
            {
                ["Attacker"] = Name(context.Source),
                ["Target"] = Name(evt.Target),
                ["Damage"] = parts + crit,
            }));
            if (evt.Vitals.IsDead)
                Emit(BattleLogCategory.Damage, TextPalette.Colorize(
                    Render("Log_Death", new() { ["Name"] = Name(evt.Target) }), TextPalette.Death));
        }

        private void OnHealed(EntityHealedEvent evt) =>
            Emit(BattleLogCategory.Heal, TextPalette.Colorize(
                Render("Log_Heal", new() { ["Name"] = Name(evt.Healed), ["Amount"] = $"{evt.Amount:0}" }), TextPalette.Heal));

        private void OnAttackEvaded(AttackEvadedEvent evt) =>
            Emit(BattleLogCategory.AttackResult, TextPalette.Colorize(
                Render("Log_Evade", new() { ["Target"] = Name(evt.Context.Target), ["Attacker"] = Name(evt.Context.Attacker) }), TextPalette.Muted));

        private void OnAttackBlocked(AttackBlockedEvent evt) =>
            Emit(BattleLogCategory.AttackResult, TextPalette.Colorize(
                Render("Log_Block", new() { ["Target"] = Name(evt.Context.Target), ["Attacker"] = Name(evt.Context.Attacker) }), TextPalette.Muted));

        private void OnEffectApplied(EffectAppliedEvent evt)
        {
            string key = evt.Caster.IsSame(evt.Target.InstanceId) ? "Log_EffectApplied" : "Log_EffectAppliedFrom";
            Emit(BattleLogCategory.Effect, Render(key, new()
            {
                ["Target"] = Name(evt.Target),
                ["Effect"] = TextPalette.Colorize($"«{evt.Effect.DisplayName}»", TextPalette.EffectName),
                ["Caster"] = Name(evt.Caster),
            }));
        }

        private void OnTurnSkipped(TurnSkippedEvent evt)
        {
            string cause = Localization.Localize((evt.Cause & StatusEffects.Freeze) != 0 ? "Log_Cause_Frozen" : "Log_Cause_Stunned");
            Emit(BattleLogCategory.Effect, Render("Log_TurnSkipped", new() { ["Name"] = Name(evt.Fighter), ["Cause"] = cause }));
        }

        private void Emit(BattleLogCategory category, string text) => EntryAdded?.Invoke(new BattleLogEntry(category, text));

        /// <summary>Values are pre-colored strings — Plain render keeps the engine from re-styling them.</summary>
        private static string Render(string key, Dictionary<string, object?> values) =>
            Localization.Render(key, values, TextFormat.Plain);

        private static string Name(IFightable fighter)
        {
            if (fighter is IPlayer) return Localization.Localize("Log_Player");
            if (!string.IsNullOrEmpty(fighter.DisplayName)) return fighter.DisplayName;
            return string.IsNullOrEmpty(fighter.Id) ? Localization.Localize("Log_Enemy") : Localization.Localize(fighter.Id);
        }
    }
}
