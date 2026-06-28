namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Battle;

    public class UnblockableAttackModifier() : IAttackModifier
    {
        public string Id => "Modifier_Unblockable_Attack";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public void Apply(IAttackContext context, AttackMetadata metadata) => context.IsUnblockable = true;
    }
}
