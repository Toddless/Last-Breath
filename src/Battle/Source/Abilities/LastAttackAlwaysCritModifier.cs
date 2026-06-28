namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Battle;

    public class LastAttackAlwaysCritModifier() : IAttackModifier
    {
        public string Id => "Modifier_Last_Attack_Always_Crit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public void Apply(IAttackContext context, AttackMetadata metadata) => context.ForceCriticalAttack = metadata.IsLast;
    }
}
