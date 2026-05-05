namespace Battle.Source.Abilities
{
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public interface IAttackModifier : IIdentifiable
    {
        void Apply(IAttackContext context, AttackMetadata metadata);
    }
}
