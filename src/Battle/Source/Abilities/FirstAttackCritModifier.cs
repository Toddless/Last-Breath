namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Battle;

    public class FirstAttackCritModifier(string id, float criticalDamageBonus) : IAttackModifier
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public void Apply(IAttackContext context, AttackMetadata metadata)
        {
            if (metadata.IsFirst) context.RawCriticalDamage += criticalDamageBonus;
        }
    }
}
