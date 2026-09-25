namespace Core.Session
{
    using System;
    using System.Collections.Generic;
    using Save;

    /// <inheritdoc cref="ISessionResetService"/>
    public class SessionResetService(LoadScope loadScope) : ISessionResetService
    {
        private readonly List<ISessionResettable> _participants = [];

        public void Register(ISessionResettable participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }

        public void ResetSession()
        {
            // Future anti-save-scam hook: a new session mints its GameId here (see the plan in HANDOFF).
            using var _ = loadScope.Begin();
            foreach (var participant in _participants)
            {
                try
                {
                    participant.ResetSession();
                }
                catch (Exception e)
                {
                    // One broken participant must not leave the rest of the session dirty.
                    Tracker.TrackException($"Session reset failed for {participant.GetType().Name}", e);
                }
            }
        }
    }
}
