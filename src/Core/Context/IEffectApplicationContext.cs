namespace Core.Context
{
    using Battle.Abilities;
    using Entity;

    /// <summary>Runs through the caster's pipeline right before an effect instance attaches to the target.
    /// Mutators work on the effect instance directly (duration, per-tick damage of DoTs).</summary>
    public interface IEffectApplicationContext
    {
        IFightable Caster { get; }
        IFightable Target { get; }
        IEffect Effect { get; }

        /// <summary>Extra copies of the effect to apply on top of this one ("+1 burning stack").
        /// Bonus copies are cloned after the pipeline ran and skip it, so mutators apply exactly once per stack.</summary>
        int BonusStacks { get; set; }
    }
}
