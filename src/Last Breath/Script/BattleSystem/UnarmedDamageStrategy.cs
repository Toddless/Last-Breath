namespace Playground.Script.BattleSystem
{
    using Playground.Components.Interfaces;

    public class UnarmedDamageStrategy : IDamageStrategy
    {
        private const float BaseCriticalChance = 1.05f;
        private const float BaseCriticalDamage = 1.2f;
        private const float BaseExtraHitChance = 1;
        private const float BaseDamage = 90;

        public float GetBaseCriticalChance() => BaseCriticalChance;
        public float GetBaseCriticalDamage() => BaseCriticalDamage;
        public float GetBaseExtraHitChance() => BaseExtraHitChance;
        public float GetDamage() => BaseDamage;
    }
}
