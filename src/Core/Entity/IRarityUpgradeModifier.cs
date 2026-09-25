namespace Core.Entity
{
    using NpcModifiers;

    public interface IRarityUpgradeModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
