namespace Core.World.Locations
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public sealed class LocationSnapshot
    {
        public int Version { get; init; } = 1;
        public double LastSimulatedAt { get; init; }
        public JObject State { get; init; } = new();
        public double ElapsedMinutes(double now) => Math.Max(0, now - LastSimulatedAt);
    }

    /// <summary>Object identity is local to its location; a missing live object stays removed.</summary>
    public interface ILocationStateParticipant
    {
        string ObjectId { get; }
        JToken CaptureLocationState();
        void RestoreLocationState(JToken state);
        void ReconcileElapsed(double gameMinutes);
    }

    public sealed class LocationSaveData
    {
        public Dictionary<string, LocationSnapshot> Locations { get; init; } = [];
    }
}
