namespace Battle.Source.Abilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class LeechOnCritAttackModifier(Priority priority, float leechPercent) : IAttackModifier
    {
        public string Id => "Attack_Modifier_Leech_On_Crit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Priority Priority { get; set; }

        public void Apply(IAttackContext context)
        {
            if (!context.IsCritical) return;
            var attacker = context.Attacker;
            attacker.Heal(new HealContext(attacker, attacker) { Amount = context.FinalDamage * leechPercent, Cause = HealCause.Leech });
        }
    }
}
