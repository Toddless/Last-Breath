namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;

    public class CritLeechEffect(int duration, int maxStacks, float amount)
        : Effect(id: "Effect_Crit_Leech", duration, maxStacks)
    {
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(amount)] = amount;
                return values;
            }
        }

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (!evt.Context.IsCritical) return;
            var attacker = evt.Context.Attacker;
            attacker.Heal(new HealContext(attacker, attacker) { Amount = evt.Context.FinalDamage.Total * amount, Cause = RecoveryCause.Leech });
        }

        public override IEffect Copy() => new CritLeechEffect(Duration, MaxStacks, amount);
    }
}
