namespace Core.Battle.Abilities
{
    /// <summary>
    /// A separate defense layer on top of the barrier: absorbs post-mitigation damage until broken
    /// (see «Боссы.md → Механика → Щит»). "While the shield holds" perks live inside the effect and
    /// die with it. Consumed by TakeDamage between mitigation and the barrier.
    /// </summary>
    public interface IShieldEffect : IEffect
    {
        float Strength { get; }

        /// <summary>Absorbs up to Strength; returns the damage left over. Breaks (removes itself) at zero.</summary>
        float Absorb(float damage);
    }
}
