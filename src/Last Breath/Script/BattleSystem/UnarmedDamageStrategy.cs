namespace LastBreath.Script.BattleSystem
{
    using Core.Interfaces.Components;

    public class UnarmedDamageStrategy : IDamageStrategy
    {
        private const float BaseCriticalChance = 0.05f;
        private const float BaseCriticalDamage = 1.2f;
        private const float BaseExtraHitChance = 0f;
        private const float BaseDamage = 90;

        public float GetBaseCriticalChance() => BaseCriticalChance;
        public float GetBaseCriticalDamage() => BaseCriticalDamage;
        public float GetBaseExtraHitChance() => BaseExtraHitChance;
        public float GetDamage() => BaseDamage;
    }
}
