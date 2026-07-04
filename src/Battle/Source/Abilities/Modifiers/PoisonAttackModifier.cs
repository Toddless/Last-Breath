namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using Effects;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class PoisonAttackModifier : IAttackModifier
    {
        public string Id => "Modifier_Poison_Attack";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);

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
