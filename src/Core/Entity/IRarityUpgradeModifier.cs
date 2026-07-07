namespace Core.Entity
{
    using Components.NpcModifiers;

    public interface IRarityUpgradeModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
