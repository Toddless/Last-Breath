namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.ScenesHandlers;

    public class ConditionsFactory : IConditionsFactory
    {
        private IBattleContext? battleContext;

        public List<ICondition> SetNewConditions(IBattleContext context)
        {
            battleContext = context;
            return [new Condition(HealthCondition, 15, EffectType.Regeneration | EffectType.Buff),
                    // for example purify effect remove all debuffs
                    new Condition(DebuffCondition, 5, EffectType.Buff | EffectType.Cleans),
                    new Condition(PoisonAppliedCondition,5, EffectType.Poison | EffectType.Regeneration)];
        }

        private bool HealthCondition() => battleContext?.Self?.HealthComponent?.CurrentHealth < battleContext?.Opponent?.HealthComponent?.CurrentHealth;

        private bool DebuffCondition() => battleContext?.Self.EffectManager.Effects.Any(x => x.EffectType == EffectType.Debuff) != null;

        private bool PoisonAppliedCondition() => battleContext?.Self.EffectManager.Effects.Any(x => x.EffectType == EffectType.Poison) != null;
    }
}
