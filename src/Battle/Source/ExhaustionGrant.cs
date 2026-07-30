namespace Battle.Source
{
    using System;
    using System.Runtime.CompilerServices;
    using Core.Battle;
    using Core.Entity;
    using Core.Events;
    using Core.Modifiers.Context;
    using Core.Services;

    /// <summary>
    /// Wires exhaustion onto a combatant (see <see cref="ExhaustionModifier"/>): the rule is global —
    /// player and NPCs alike — so the single attach point is shared by both entity kinds and both
    /// projects. A sandbox without combat rules, or a zeroed surcharge in data, attaches nothing.
    /// The wiring is symmetric: <see cref="Detach"/> takes back the modifier and all three
    /// subscriptions, and a repeated <see cref="Attach(IFightable)"/> keeps the single set it already
    /// gave instead of doubling the surcharge.
    /// </summary>
    public static class ExhaustionGrant
    {
        /// <summary>
        /// What every combatant received, kept because none of it can be found again from outside:
        /// the modifier handler does not enumerate its lists and a lambda subscription cannot be
        /// matched by shape. The keys are weak, so a combatant freed without a detach cannot hold
        /// its wiring — or itself — alive through this table.
        /// </summary>
        private static readonly ConditionalWeakTable<IFightable, Wiring> s_wiring = new();

        public static void Attach(IFightable owner)
        {
            var provider = GameServiceProvider.Instance.GetService<ICombatRulesProvider>();
            if (provider == null) return;

            Attach(owner, provider.Exhaustion);
        }

        /// <summary>Wiring against explicit rules; the entry point above takes them from the combat
        /// rules catalog.</summary>
        public static void Attach(IFightable owner, ExhaustionRules rules)
        {
            if (rules.CostIncreasePerStack <= 0) return;
            if (s_wiring.TryGetValue(owner, out _)) return;

            var exhaustion = new ExhaustionModifier(rules);
            Action<AbilityActivatedEvent> onActivated = _ => exhaustion.OnAbilityActivated();
            Action<TurnEndEvent> onTurnEnd = _ => exhaustion.DecayTick();
            Action<BattleEndEvent> onBattleEnd = _ => exhaustion.Reset();

            owner.ModifierHandler.Add(exhaustion);
            owner.CombatEvents.Subscribe(onActivated);
            owner.CombatEvents.Subscribe(onTurnEnd);
            owner.CombatEvents.Subscribe(onBattleEnd);
            s_wiring.Add(owner, new Wiring(exhaustion, onActivated, onTurnEnd, onBattleEnd));
        }

        /// <summary>Called from the combatant's destruction: the surcharge leaves the modifier
        /// handler and the three reactions leave the combat bus. A combatant that was never wired
        /// (no rules, zeroed surcharge) detaches to a no-op.</summary>
        public static void Detach(IFightable owner)
        {
            if (!s_wiring.TryGetValue(owner, out Wiring? wiring)) return;

            s_wiring.Remove(owner);
            owner.ModifierHandler.Remove(wiring.Modifier);
            owner.CombatEvents.Unsubscribe(wiring.OnActivated);
            owner.CombatEvents.Unsubscribe(wiring.OnTurnEnd);
            owner.CombatEvents.Unsubscribe(wiring.OnBattleEnd);
        }

        private sealed record Wiring(
            ExhaustionModifier Modifier,
            Action<AbilityActivatedEvent> OnActivated,
            Action<TurnEndEvent> OnTurnEnd,
            Action<BattleEndEvent> OnBattleEnd);
    }
}
