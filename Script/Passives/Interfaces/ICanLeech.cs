namespace Playground.Script.Passives.Interfaces
{
    public interface ICanLeech
    {
        void Leech(AttackComponent? attack = default, HealthComponent? health = default);
    }
}
