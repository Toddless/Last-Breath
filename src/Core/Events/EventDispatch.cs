namespace Core.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Isolated event dispatch shared by every bus (game/battle/combat): a throwing subscriber is
    /// logged and skipped so it can't abort the rest of the notification or the replay beat loop.
    /// The snapshot (<c>ToList</c>) keeps publishing re-entrancy-safe — a handler may (un)subscribe
    /// while the event is being delivered.
    /// DEBUG builds rethrow after logging so a handler bug is loud during development (the Godot editor
    /// runs Debug); exported (Release) builds stay resilient. NOTE: no Godot calls here on purpose —
    /// the game bus dispatches inside the headless loot/reputation simulations, where any GD.* call is a
    /// fatal 0xC0000005 that try/catch cannot contain, so the "loud" path is a managed rethrow.
    /// </summary>
    public static class EventDispatch
    {
        public static void Dispatch<T>(IEnumerable<Action<T>> handlers, T evnt, object? source)
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    handler(evnt);
                }
                catch (Exception e)
                {
                    Tracker.TrackException($"Event handler failed for {typeof(T).Name}", e, source);
#if DEBUG
                    throw;
#endif
                }
            }
        }
    }
}
