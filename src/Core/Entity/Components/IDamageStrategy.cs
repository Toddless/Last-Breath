namespace Core.Entity.Components
{
    public interface IDamageStrategy
    {
        float GetDamage();
        float GetBaseCriticalChance();
        float GetBaseCriticalDamage();
        float GetBaseExtraHitChance();
    }
}
