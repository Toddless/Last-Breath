namespace Core.Interfaces.Abilities
{
    using Entity;

    public struct EffectApplyingContext
    {
        public IEntity Caster { get; set; }
        public IEntity Target { get; set; }
        public float Damage { get; set; }
        public bool IsCritical { get; set; }
        public object? Source { get; set; }
    }
}
