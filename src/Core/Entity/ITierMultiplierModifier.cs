namespace Core.Entity
{
    using NpcModifiers;

    public interface ITierMultiplierModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
