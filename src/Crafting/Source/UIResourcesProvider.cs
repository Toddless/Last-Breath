namespace Crafting.Source
{
    using Godot;
    using Utilities;
    using System.IO;
    using Core.Interfaces.Data;
    using System.Collections.Generic;

    public class UIResourcesProvider : IUIResourcesProvider
    {
        private const string Folder = "res://Source/UIElements/Styles/";

        private Dictionary<string, Resource> _uiResources = [];

        public UIResourcesProvider()
        {
            LoadResources();
        }

        public Resource? GetResource(string name)
        {
            if (!_uiResources.TryGetValue(name, out var resource))
            {
                Tracker.TrackNotFound($"Resource with name: {name}", this);
                return null;
            }
            return resource.Duplicate(true);
        }

        private void LoadResources()
        {
            var data = ResourceLoader.ListDirectory(Folder);
            foreach (var res in data)
            {
                var path = Path.Combine(Folder, res);
                if (!path.EndsWith(".tres")) continue;
                var loadedData = ResourceLoader.Load(path);
                _uiResources.TryAdd(loadedData.ResourceName, loadedData);
            }
        }
    }
}
