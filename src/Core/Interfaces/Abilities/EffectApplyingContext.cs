namespace Core.Interfaces.Abilities
{
    using Entity;

    public struct EffectApplyingContext
    {
        public IEntity Caster { get; init; }
        public IEntity Target { get; set; }
        public float Damage { get; set; }
        public bool IsCritical { get; set; }
        public string Source { get; init; }
    }
}
