namespace Playground.Script.Passives.Interfaces
{
    public interface ICanHeal
    {
        void Heal(HealthComponent? health = default);
    }
}
