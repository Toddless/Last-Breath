namespace Core.Interfaces
{
    using System;
    using Entity;

    /// <summary>A live predicate over the owner's state (health threshold, barrier presence...).
    /// Attach subscribes to the owner's signals; StateChanged fires on every IsMet flip.</summary>
    public interface ICondition
    {
        bool IsMet { get; }
        event Action<bool> StateChanged;

        void Attach(IFightable owner);
        void Detach();

        /// <summary>Fresh unattached instance: conditions hold owner state, so sharing one between copies is a bug.</summary>
        ICondition Copy();
    }
}
