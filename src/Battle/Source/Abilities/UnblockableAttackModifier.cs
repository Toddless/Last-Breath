namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Battle;

    public class UnblockableAttackModifier(string id) : IAttackModifier
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public void Apply(IAttackContext context, AttackMetadata metadata) => context.IsUnblockable = true;
    }
}
