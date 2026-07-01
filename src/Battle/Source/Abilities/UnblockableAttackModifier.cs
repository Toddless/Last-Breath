namespace Battle.Source.Abilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class UnblockableAttackModifier() : IAttackModifier
    {
        public string Id => "Modifier_Unblockable_Attack";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public void Apply(IAttackContext context) => context.IsUnblockable = true;
    }
}
