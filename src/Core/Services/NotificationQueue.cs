namespace Core.Services
{
    using System.Collections.Generic;
    using Views.UI;

    /// <summary>
    /// Per-region throttle for notifications: at most N visible at once, the rest wait in FIFO
    /// order. Regions are independent — a location announcement never waits for the system feed.
    /// Pure bookkeeping; the NotificationService owns the popups themselves.
    /// </summary>
    public class NotificationQueue(int maxVisiblePerRegion)
    {
        private readonly Dictionary<OverlayRegion, Queue<string>> _waiting = [];
        private readonly Dictionary<OverlayRegion, int> _visible = [];

        /// <summary>True — a slot was taken, show the notification now; false — parked in the queue.</summary>
        public bool TryTake(string notificationId, OverlayRegion region)
        {
            int visible = _visible.GetValueOrDefault(region);
            if (visible >= maxVisiblePerRegion)
            {
                Waiting(region).Enqueue(notificationId);
                return false;
            }

            _visible[region] = visible + 1;
            return true;
        }

        /// <summary>A visible notification died; returns the next queued id (its slot is re-taken) or null.</summary>
        public string? Release(OverlayRegion region)
        {
            int visible = _visible.GetValueOrDefault(region);
            if (visible > 0) _visible[region] = visible - 1;

            var waiting = Waiting(region);
            if (waiting.Count == 0) return null;

            _visible[region] = _visible.GetValueOrDefault(region) + 1;
            return waiting.Dequeue();
        }

        /// <summary>Esc cleared the overlay layer: drop everything, queued included.</summary>
        public void Clear()
        {
            _waiting.Clear();
            _visible.Clear();
        }

        private Queue<string> Waiting(OverlayRegion region)
        {
            if (!_waiting.TryGetValue(region, out var queue))
                _waiting[region] = queue = new Queue<string>();
            return queue;
        }
    }
}
