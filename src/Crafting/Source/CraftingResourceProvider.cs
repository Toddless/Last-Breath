namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.IO;
    using Core.Interfaces.Crafting;
    using Godot;
    using Utilities;

    public class CraftingResourceProvider
    {
        private readonly string _pathToResources;
        private Dictionary<string, ICraftingResource> _resources = [];

        public CraftingResourceProvider(string pathToResources)
        {
            _pathToResources = pathToResources;
        }

        public ICraftingResource? GetResource(string resourceId)
        {
            if (!_resources.TryGetValue(resourceId, out var res))
            {
                Logger.LogNotFound(resourceId, this);
                return null;
            }
            return res.Copy<ICraftingResource>(true);
        }

        public IEnumerable<ICraftingResource> GetAllResources() => [.. _resources.Values];

        public void InitializeResources()
        {
            var resourceData = ResourceLoader.ListDirectory(_pathToResources);
            foreach (var res in resourceData)
            {
                var loadedResource = ResourceLoader.Load(Path.Combine(_pathToResources, res));
                if (loadedResource is ICraftingResource craft) _resources.Add(craft.Id, craft);
            }
        }
    }
}
