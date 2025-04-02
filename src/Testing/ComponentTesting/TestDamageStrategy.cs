namespace PlaygroundTest.ComponentTesting
{
    using Playground.Components.Interfaces;

    public class TestDamageStrategy : IDamageStrategy
    {
        public float GetBaseCriticalChance() => 0.05f;
        public float GetBaseCriticalDamage() => 1.2f;
        public float GetBaseExtraHitChance() => 1;
        public float GetDamage() => 90;
    }
}
