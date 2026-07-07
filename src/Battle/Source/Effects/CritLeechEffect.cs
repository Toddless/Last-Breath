namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events.GameEvents;

    public class CritLeechEffect(int duration, int maxStacks, float amount)
        : Effect(id: "Effect_Crit_Leech", duration, maxStacks)
    {
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
            attacker.Heal(new HealContext(attacker, attacker) { Amount = evt.Context.FinalDamage * amount, Cause = HealCause.Leech });
        }

        public override IEffect Copy() => new CritLeechEffect(Duration, MaxStacks, amount);
    }
}
