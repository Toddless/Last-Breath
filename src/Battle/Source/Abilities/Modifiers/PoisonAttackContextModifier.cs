namespace Battle.Source.Abilities.Modifiers
{
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Effects;

    public class PoisonAttackContextModifier()
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Modifier_Poison_Attack"), IAttackModifier
    {
        public void Apply(IAttackContext context)
        {
            if (context.Result is not AttackResults.Succeed) return;

            var poison = new DamageOverTurnEffect(3, StatusEffects.Poison);
            poison.Apply(new EffectApplyingContext
            {
                Caster = context.Attacker,
                Damage = context.FinalDamage,
                IsCritical = context.IsCritical,
                Source = context.Attacker.InstanceId,
                Target = context.Target
            });
        }
    }
}
