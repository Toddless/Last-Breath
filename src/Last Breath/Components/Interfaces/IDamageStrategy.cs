namespace LastBreath.Components.Interfaces
{
    public interface IDamageStrategy
    {
        float GetDamage();
        float GetBaseCriticalChance();
        float GetBaseCriticalDamage();
        float GetBaseExtraHitChance();
    }
}
