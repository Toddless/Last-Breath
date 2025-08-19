namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Godot;

    public class ResourceManager
    {
        private readonly string _pathToResources;
        private Dictionary<string, ICraftingResource> _resources = [];

        public ResourceManager(string pathToResources)
        {
            _pathToResources = pathToResources;
        }

        public void AddResource(ICraftingResource resource) => _resources.Add(resource.Id, resource);

        public ICraftingResource? GetResource(string resourceId)
        {
            if (_resources.TryGetValue(resourceId, out var res))
                return res;
            return null;
        }

        public IEnumerable<ICraftingResource> GetAllResources() => [.. _resources.Values];

        public bool HasResource(string id) => _resources.ContainsKey(id);

        public void InitializeResources()
        {
            using var dir = DirAccess.Open(_pathToResources);
            if (dir == null)
            {
                GD.PrintErr($"CraftingResourceManager: Could not open folder '{_pathToResources}'");
                return;
            }

            dir.ListDirBegin();
            try
            {
                string file;
                while ((file = dir.GetNext()) != "")
                {
                    if (dir.CurrentIsDir())
                        continue;

                    if (!file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var fullPath = $"{_pathToResources}/{file}";
                    var loaded = ResourceLoader.Load(fullPath);
                    if (loaded is not ICraftingResource resource)
                        continue;

                    if (_resources.ContainsKey(resource.Id))
                        GD.PrintErr($"CraftingResourceManager: Duplicate resource id '{resource.Id}' skipping {fullPath}");
                    else
                        _resources[resource.Id] = resource;
                }
            }
            finally
            {
                dir.ListDirEnd();
            }
        }
    }
}
