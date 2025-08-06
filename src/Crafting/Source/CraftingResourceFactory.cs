namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core.Enums;
    using Core.Interfaces.CraftingResources;
    using Crafting.Source.CraftingResources;
    using Godot;

    public class CraftingResourceFactory
    {
        private const string DataPath = "Source/CraftingResources";

        private readonly Dictionary<ResourceCategory, ICraftingResource> _craftingResources = [];

        public CraftingResourceFactory()
        {
            LoadResources();
        }

        public ICraftingResource? CreateResource(ResourceCategory type, float quality, int quantity = 1)
        {
            if (!_craftingResources.TryGetValue(type, out var resource))
            {
                GD.PushError("Resource not found: {0}", type);
            }

            var instance = resource?.Copy(true);
            if (instance == null)
            {
                GD.PushError("Fail by copy resource.");
            }
            if (instance != null)
            {
                instance.Quantity = quantity;
            }

            return instance;
        }

        private void LoadResources()
        {
            var userDir = ProjectSettings.GlobalizePath("res://");
            var userDataPath = Path.Combine(userDir, DataPath);

            if (Directory.Exists(userDataPath))
            {
                // TODO: Change it later
                // for now recreate each time (i need new data)
               // Directory.Delete(userDataPath);
                Directory.CreateDirectory(userDataPath);
            }
            foreach (var resourceType in Enum.GetValues<ResourceCategory>())
            {
                var resource = ResourceLoader.Load<CraftingResource>($"res://Source/CraftingResources/{resourceType}.tres");
                _craftingResources[resourceType] = resource;
            }
        }

        private static ResourceQuality DefineResourceQuality(float quality) => quality switch
        {
            < 50 => ResourceQuality.LowGrade,
            < 65 => ResourceQuality.Common,
            < 95 => ResourceQuality.HighClass,
            _ => ResourceQuality.Excellent,
        };
    }
}
