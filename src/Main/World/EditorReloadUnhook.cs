namespace LastBreath.World
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Loader;
    using System.Threading;

    /// <summary>A signal subscription that must come apart before this assembly's load context unloads.</summary>
    internal interface IReloadUnhookable
    {
        /// <summary>Drops the subscription if it still stands. Safe to call when already dropped.</summary>
        void Unhook();
    }

    /// <summary>
    /// Runs every registered <see cref="IReloadUnhookable.Unhook"/> the moment the editor starts unloading this
    /// assembly's load context. Godot-mono keeps its own list of managed callables and disconnects each of them
    /// itself during that unload — and when the unload fails (godotengine/godot#78513), the recovery pass walks
    /// the list again, printing "nonexistent connection" for the entries the first pass already removed.
    /// Unhooking here, before the engine touches its list, leaves both passes nothing to disconnect.
    /// </summary>
    /// <remarks>
    /// In a running game the Unloading event never fires; the registry is then just a short list that empties
    /// itself as nodes exit the tree. Entries are weak so the registry never keeps a leaked node alive.
    /// </remarks>
    internal static class EditorReloadUnhook
    {
        private static readonly Lock Gate = new();
        private static readonly List<WeakReference<IReloadUnhookable>> Registry = [];
        private static bool _armed;

        /// <summary>Remembers a live subscription; the first one arms the unload hook.</summary>
        public static void Register(IReloadUnhookable watcher)
        {
            lock (Gate)
            {
                Arm();
                Registry.Add(new WeakReference<IReloadUnhookable>(watcher));
            }
        }

        /// <summary>Forgets a subscription that came apart on its own — its node left the tree.</summary>
        public static void Unregister(IReloadUnhookable watcher)
        {
            lock (Gate)
            {
                Registry.RemoveAll(entry => !entry.TryGetTarget(out IReloadUnhookable? target) || ReferenceEquals(target, watcher));
            }
        }

        private static void Arm()
        {
            if (_armed) return;

            _armed = true;
            // Null names an assembly outside any load context — nothing will ever unload it, no hook needed.
            if (AssemblyLoadContext.GetLoadContext(typeof(EditorReloadUnhook).Assembly) is { } context)
            {
                context.Unloading += _ => UnhookAll();
            }
        }

        private static void UnhookAll()
        {
            List<WeakReference<IReloadUnhookable>> snapshot;
            lock (Gate)
            {
                snapshot = [.. Registry];
                Registry.Clear();
            }

            // A second Unloading pass (the engine retries a failed unload) finds the registry already empty,
            // and each Unhook guards itself besides — running this twice stays harmless.
            foreach (WeakReference<IReloadUnhookable> entry in snapshot)
            {
                if (entry.TryGetTarget(out IReloadUnhookable? watcher)) watcher.Unhook();
            }
        }
    }
}
