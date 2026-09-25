namespace Battle.Source.Abilities
{
    using Core.Context;
    using Core.Entity;

    /// <summary>A single shard/projectile that landed: consumed by on-hit riders. Damage is the split the
    /// hit actually dealt, so effects fed by it can take the component of their own kind.</summary>
    public record ProjectileHit(IFightable Target, bool IsCritical, DamageSnapshot Damage);
}
