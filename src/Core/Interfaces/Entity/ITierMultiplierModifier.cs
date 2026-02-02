namespace Core.Interfaces.Entity
{
    public interface ITierMultiplierModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
