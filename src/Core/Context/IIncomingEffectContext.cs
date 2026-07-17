namespace Core.Context
{
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// Target-side view of an effect about to land: the TARGET's pipeline tunes the instance
    /// (boss control resistance shortens stuns) or rejects it outright. Runs after the caster-side
    /// application pipeline, so it sees the final outgoing numbers.
    /// </summary>
    public interface IIncomingEffectContext
    {
        IFightable Caster { get; }
        IFightable Target { get; }
        IEffect Effect { get; }

        /// <summary>True = the target resisted: the effect is not applied at all.</summary>
        bool Rejected { get; set; }
    }
}
