namespace Core.Entity
{
    using Components.NpcModifiers;

    public interface ITierMultiplierModifier : INpcModifier, IChangeableChances
    {
        float BaseMultiplier { get; }
    }
}
