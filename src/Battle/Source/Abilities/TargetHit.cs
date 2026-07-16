namespace Battle.Source.Abilities
{
    using Core.Entity;

    /// <summary>A single hit that landed: consumed by on-hit riders.</summary>
    public record TargetHit(IFightable Target, bool IsCritical, float Damage);
}
