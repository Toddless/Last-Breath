namespace Core.Interfaces.Entity
{
    public interface ITierMultiplierModifier: INpcModifier
    {
        float BaseMultiplier { get; }
        float CurrentMultiplier { get; set; }
    }
}
