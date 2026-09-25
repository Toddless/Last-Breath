namespace Core.Data
{
    using Battle;
    using Battle.Abilities;
    using Context;
    using Entity;

    /// <summary>
    /// One delivery impact of an ability — one actual touch of one target, of one of the five kinds
    /// <see cref="ImpactKind"/> names: an attack of a series, a direct hit, a projectile of a volley, a
    /// jump of a chain or a splash spilled by another impact. Impact riders receive it to apply
    /// per-impact effects to the actual target.
    /// The kind is stamped AT THE DELIVERY and is never corrected further down: nothing below the point
    /// of construction knows which road the touch came by, so whatever is written here is what every
    /// rider and filter downstream will believe.
    /// </summary>
    public record AbilityImpact(
        IFightable Caster,
        IFightable Target,
        IBattleField Field,
        bool Succeeded = true,
        bool IsCritical = false,
        DamageSnapshot Damage = default)
    {
        /// <summary>
        /// The ability instance this impact came out of — the channel between a rider and the cast that
        /// fired it. A rider reaches the ability's own decorated numbers through it, so an augment that
        /// raises a parameter is felt by whatever the rider builds, without either of them knowing the
        /// other exists. Required: an impact whose source could be left out would be an impact whose
        /// riders silently fall back to hardcoded numbers, and nothing would report it.
        /// </summary>
        public required IAbility Source { get; init; }

        /// <summary>
        /// What kind of touch this is (see <see cref="ImpactKind"/>) — attack, hit, projectile, chain
        /// jump or splash. The unit of a rider's work is the IMPACT, and riders differ in which impacts
        /// they were bought for; this is the only thing they have to tell them apart by. Required for the
        /// same reason as <see cref="Source"/>: a forgotten kind is a delivery quietly claiming to be
        /// something it is not.
        /// </summary>
        public required ImpactKind Kind { get; init; }
    }
}
