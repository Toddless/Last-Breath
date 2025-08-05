namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using Crafting.Source.CraftingResources;
    using Godot;

    public static class CraftingResourceFactory
    {
        private static readonly Dictionary<ResourceType, ICraftingResource> s_craftingResources = [];

        static CraftingResourceFactory()
        {
            LoadResources();
        }


        public static ICraftingResource? CreateResource(ResourceType type, float quality, int quantity = 1)
        {
            if (!s_craftingResources.TryGetValue(type, out var resource))
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
                instance.Quality = DefineResourceQuality(quality);
            }

            return instance;
        }

        private static void LoadResources()
        {
            foreach (var resourceType in Enum.GetValues<ResourceType>())
            {
                var resource = ResourceLoader.Load<CraftingResource>($"res://Source/CraftingResources/{resourceType}.tres");
                s_craftingResources[resourceType] = resource;
            }
        }

        private static ResourceQuality DefineResourceQuality(float quality) => quality switch
        {
            < 50 => ResourceQuality.LowGrade,
            < 85 => ResourceQuality.Common,
            _ => ResourceQuality.HighClass,
        };
    }
}
