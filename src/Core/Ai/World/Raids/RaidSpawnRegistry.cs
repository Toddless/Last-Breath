namespace Core.Ai.World.Raids
{
    using System.Collections.Generic;

    /// <summary>Spawn points register on ready and unregister on exit; the raid service scans the list.</summary>
    public interface IRaidSpawnRegistry
    {
        IReadOnlyList<IRaidSpawnSite> All { get; }

        void Register(IRaidSpawnSite site);
        void Unregister(IRaidSpawnSite site);
    }

    public class RaidSpawnRegistry : IRaidSpawnRegistry
    {
        private readonly List<IRaidSpawnSite> _sites = [];

        public IReadOnlyList<IRaidSpawnSite> All => _sites;

        public void Register(IRaidSpawnSite site)
        {
            if (!_sites.Contains(site)) _sites.Add(site);
        }

        public void Unregister(IRaidSpawnSite site) => _sites.Remove(site);
    }
}
