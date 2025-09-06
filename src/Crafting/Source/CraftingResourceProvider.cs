namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.IO;
    using Core.Interfaces.Crafting;
    using Godot;

    public class CraftingResourceProvider
    {
        private readonly string _pathToResources;
        private Dictionary<string, ICraftingResource> _resources = [];

        public static CraftingResourceProvider? Instance { get; private set; }

        public CraftingResourceProvider(string pathToResources)
        {
            _pathToResources = pathToResources;
            Instance = this;
        }

        public ICraftingResource? GetResource(string resourceId)
        {
            if (!_resources.TryGetValue(resourceId, out var res))
                return null;
            return res.Copy<ICraftingResource>(true);
        }

        public Texture2D? GetResourceIcon(string resourceId)
        {
            if (!_resources.TryGetValue(resourceId, out var res))
                return null;
            return res.Icon;
        }

        public string GetResourceName(string resourceId)
        {
            if (!_resources.TryGetValue(resourceId, out var res))
                return string.Empty;

            return res.DisplayName;
        }

        public IReadOnlyList<IMaterialModifier> GetResourceModifiers(string resourceId)
        {
            if(!_resources.TryGetValue(resourceId, out var res))
            {
                return [];
            }
            return res.MaterialType?.Modifiers ?? [];
        }

        public bool IsResource(string resourceId) => _resources.ContainsKey(resourceId);


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
