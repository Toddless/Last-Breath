namespace Playground.Script.Helpers
{
    public struct AttackResult(float damage, bool isCritical)
    {
        public float Damage { get; } = damage;
        public bool IsCritical { get; } = isCritical;
    }
}
