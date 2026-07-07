namespace Core.Battle.Abilities
{
    using Entity;

    public struct EffectApplyingContext
    {
        public IFightable Caster { get; init; }
        public IFightable Target { get; set; }
        public float Damage { get; set; }
        public bool IsCritical { get; set; }
        public string Source { get; init; }
    }
}
