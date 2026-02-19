namespace Core.Interfaces.Entity
{
    public interface ITierUpgradeModifier : INpcModifier
    {
        float BaseMultiplier { get; }
        float CurrentMultiplier { get; }
        int UpgradeBy { get; }
    }
}
