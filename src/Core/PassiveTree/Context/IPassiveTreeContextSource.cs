namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using Entity;
    using Enums;

    /// <summary>
    /// The context half of a tree's contribution as its carrier sees it: something handed to a fighter and
    /// taken back off one. One modifier per KNOB, not per node — a pipeline applies modifiers in sequence
    /// rather than summing them, so many nodes reading "+10%" must arrive as one total rather than a
    /// compounding chain. Rebuilding belongs to whoever owns the allocation and stays off this surface — a
    /// fighter is given the contribution, it does not decide what is in it.
    /// </summary>
    public interface IPassiveTreeContextSource
    {
        /// <summary>The knobs the allocation currently feeds — one modifier stands behind each.</summary>
        IReadOnlyCollection<ContextParameter> Knobs { get; }

        /// <summary>What the modifier behind a knob reads right now: the taken lines that count, in the
        /// pipeline's own unit. Zero for a knob the allocation doesn't feed. The one place to ask what an
        /// allocation is worth to a fight, as opposed to what it holds on paper.</summary>
        float ValueOf(ContextParameter parameter);

        void Attach(IFightable owner);

        void Detach(IFightable owner);
    }
}
