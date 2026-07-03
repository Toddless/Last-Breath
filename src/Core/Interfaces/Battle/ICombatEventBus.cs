namespace Core.Interfaces.Battle
{
    using System;

    public interface ICombatEventBus : IEventBus<ICombatEvent>, IDisposable
    {
        /// <summary>
        /// Catch-all subscription: the handler receives every published event regardless of its type.
        /// Catch-all handlers run BEFORE typed handlers, so recorders (battle timeline) capture
        /// a parent event before its reactions publish nested ones — preserving causal order.
        /// </summary>
        void SubscribeAll(Action<ICombatEvent> handler);

        void UnsubscribeAll(Action<ICombatEvent> handler);
    }
}
