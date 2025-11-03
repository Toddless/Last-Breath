namespace Core.Interfaces.Components
{
    public interface IDefenceComponent
    {
        float Armor { get; }
        float EnergyBarrier { get; }
        float Evade { get; }
        float MaxEvadeChance { get; }
        float MaxReduceDamage { get; }

        float BarrierAbsorbDamage(float amount);
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
    }
}
