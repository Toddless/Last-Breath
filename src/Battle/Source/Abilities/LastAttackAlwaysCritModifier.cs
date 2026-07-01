namespace Battle.Source.Abilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class LastAttackAlwaysCritModifier : IAttackModifier
    {
        public string Id => "Modifier_Last_Attack_Always_Crit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public void Apply(IAttackContext context) => context.ForceCriticalAttack = context.IsLast;
    }
}
