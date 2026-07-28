namespace Battle.Source
{
    using Core.Battle;
    using Core.Entity;
    using Core.Events;
    using Core.Modifiers.Context;
    using Core.Services;

    /// <summary>
    /// Wires exhaustion onto a combatant (see <see cref="ExhaustionModifier"/>): the rule is global —
    /// player and NPCs alike — so the single attach point is shared by both entity kinds and both
    /// projects. A sandbox without combat rules, or a zeroed surcharge in data, attaches nothing.
    /// </summary>
    public static class ExhaustionGrant
    {
        public static void Attach(IFightable owner)
        {
            var provider = GameServiceProvider.Instance.GetService<ICombatRulesProvider>();
            if (provider == null) return;
            var rules = provider.Exhaustion;
            if (rules.CostIncreasePerStack <= 0) return;

            var exhaustion = new ExhaustionModifier(rules);
            owner.ModifierHandler.Add(exhaustion);
            owner.CombatEvents.Subscribe<AbilityActivatedEvent>(_ => exhaustion.OnAbilityActivated());
            owner.CombatEvents.Subscribe<TurnEndEvent>(_ => exhaustion.DecayTick());
            owner.CombatEvents.Subscribe<BattleEndEvent>(_ => exhaustion.Reset());
        }
    }
}
