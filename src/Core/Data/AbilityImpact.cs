namespace Core.Data
{
    using Interfaces.Battle;
    using Interfaces.Entity;

    /// <summary>
    /// One delivery impact of an ability: a hit, a bounce landing, one attack of a series.
    /// Impact riders receive it to apply per-impact effects to the actual target.
    /// </summary>
    public record AbilityImpact(
        IFightable Caster,
        IFightable Target,
        IBattleField Field,
        bool Succeeded = true,
        bool IsCritical = false,
        float Damage = 0);
}
