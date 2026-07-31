namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using Entity;
    using Enums;

    /// <summary>
    /// The context half of a tree's contribution as its carrier sees it: something handed to a fighter
    /// and taken back off one. Rebuilding it belongs to whoever owns the allocation and stays off this
    /// surface — a fighter is given the contribution, it does not decide what is in it.
    /// </summary>
    public interface IPassiveTreeContextSource
    {
        /// <summary>The knobs the allocation currently feeds — one modifier stands behind each.</summary>
        IReadOnlyCollection<ContextParameter> Knobs { get; }

        /// <summary>What the modifier standing behind a knob reads at this moment: the taken lines that
        /// count right now, in the unit the pipeline takes them in. Zero for a knob the allocation feeds
        /// nothing to. The one place to ask what an allocation is actually worth to a fight, as opposed to
        /// what it holds on paper.</summary>
        float ValueOf(ContextParameter parameter);

        void Attach(IFightable owner);

        void Detach(IFightable owner);
    }
}
