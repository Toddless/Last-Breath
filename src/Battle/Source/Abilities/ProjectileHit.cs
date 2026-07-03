namespace Battle.Source.Abilities
{
    using Core.Interfaces.Entity;

    /// <summary>A single shard/projectile that landed: consumed by on-hit riders.</summary>
    public record ProjectileHit(IFightable Target, bool IsCritical, float Damage);
}
