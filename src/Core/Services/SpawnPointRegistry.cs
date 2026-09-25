namespace Core.Services
{
    using System.Collections.Generic;
    using Entity;

    /// <inheritdoc cref="ISpawnPointRegistry"/>
    public class SpawnPointRegistry : ISpawnPointRegistry
    {
        private readonly List<IPersistentSpawnPoint> _points = [];

        public IReadOnlyList<IPersistentSpawnPoint> All => _points;

        public void Register(IPersistentSpawnPoint point)
        {
            if (!_points.Contains(point)) _points.Add(point);
        }

        public void Unregister(IPersistentSpawnPoint point) => _points.Remove(point);
    }
}
