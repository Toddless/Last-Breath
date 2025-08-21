namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core.Interfaces.Crafting;
    using Godot;

    public class CraftingResourceProvider
    {
        private readonly string _pathToResources;
        private Dictionary<string, ICraftingResource> _resources = [];

        public CraftingResourceProvider(string pathToResources)
        {
            _pathToResources = pathToResources;
        }

        public void AddResource(ICraftingResource resource) => _resources.Add(resource.Id, resource);

        // last ref here
        public ICraftingResource? GetResource(string resourceId)
        {
            if (_resources.TryGetValue(resourceId, out var res))
                return res.Copy<ICraftingResource>(true);
            return null;
        }

        public IEnumerable<ICraftingResource> GetAllResources() => [.. _resources.Values];

        public bool HasResource(string id) => _resources.ContainsKey(id);

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
