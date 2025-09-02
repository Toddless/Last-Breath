namespace Core.Interfaces.Components
{
    public interface IDamageStrategy
    {
        float GetDamage();
        float GetBaseCriticalChance();
        float GetBaseCriticalDamage();
        float GetBaseExtraHitChance();
    }
}
