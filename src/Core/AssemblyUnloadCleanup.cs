namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Loader;
    using System.Threading;

    /// <summary>
    /// Runs registered cleanups the moment the editor starts unloading this assembly's load context.
    /// Statics holding native wrappers, open file handles or engine subscriptions must come apart
    /// before the unload, or the old context stays pinned (".NET: Failed to unload assemblies").
    /// One utility serves every game assembly: the editor loads them all into one collectible
    /// context, so the single hook on this assembly's context fires for all of them.
    /// </summary>
    /// <remarks>
    /// Outside the editor the load context is not collectible and nothing is armed: a running game
    /// or a test host never sees these cleanups. Each cleanup must be idempotent and must not log —
    /// the Tracker's own teardown is one of them.
    /// </remarks>
    public static class AssemblyUnloadCleanup
    {
        private static readonly Lock Gate = new();
        private static readonly List<Action> Cleanups = [];
        private static bool _armed;

        /// <summary>Remembers a cleanup to run on unload; the first one arms the hook.</summary>
        public static void Register(Action cleanup)
        {
            lock (Gate)
            {
                Arm();
                Cleanups.Add(cleanup);
            }
        }

        private static void Arm()
        {
            if (_armed) return;

            _armed = true;
            if (AssemblyLoadContext.GetLoadContext(typeof(AssemblyUnloadCleanup).Assembly) is { IsCollectible: true } context)
            {
                context.Unloading += OnUnloading;
            }
        }

        private static void OnUnloading(AssemblyLoadContext _)
        {
            List<Action> snapshot;
            lock (Gate)
            {
                snapshot = [.. Cleanups];
                Cleanups.Clear();
            }

            foreach (Action cleanup in snapshot)
            {
                try
                {
                    cleanup();
                }
                catch
                {
                    // A failed cleanup must not stop the rest, and mid-unload there is nowhere left to log.
                }
            }
        }
    }
}
