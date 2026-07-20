namespace Core.Ai.World.SmartPoints
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class SmartPointRegistry : ISmartPointRegistry
    {
        private readonly List<ISmartPoint> _points = [];
        private readonly Dictionary<string, ISmartPoint> _claims = [];

        public void Register(ISmartPoint point)
        {
            if (!_points.Contains(point)) _points.Add(point);
        }

        public void Unregister(ISmartPoint point)
        {
            _points.Remove(point);
            // A vanished node (scene change) must not leave dangling claims behind.
            foreach (string claimant in _claims.Where(pair => ReferenceEquals(pair.Value, point)).Select(pair => pair.Key).ToList())
                _claims.Remove(claimant);
        }

        public ISmartPoint? TryClaim(string tag, string claimantId, Vector2 from)
        {
            if (_claims.TryGetValue(claimantId, out var held) && held.Tag == tag) return held;

            ISmartPoint? nearest = null;
            float best = float.MaxValue;
            foreach (var point in _points)
            {
                if (point.Tag != tag || ClaimCount(point) >= point.Capacity) continue;
                float distance = from.DistanceSquaredTo(point.Position);
                if (distance >= best) continue;
                best = distance;
                nearest = point;
            }

            if (nearest == null) return null;
            _claims[claimantId] = nearest;
            return nearest;
        }

        public void Release(string claimantId) => _claims.Remove(claimantId);

        private int ClaimCount(ISmartPoint point) => _claims.Values.Count(claimed => ReferenceEquals(claimed, point));
    }
}
