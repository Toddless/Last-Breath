namespace Core.Interfaces.Entity
{
    public interface IRarityUpgradeModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
