namespace LastBreath.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Contracts.Data;
    using Godot;
    using LastBreath.Script.Items.ItemData;
    using LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;

    public partial class DiContainer : Node
    {
        private static List<ClassMetadata>? ClassMetadataCollection
        {
            get; set;
        }

        private static IServiceProvider? s_serviceProvider;

        public override void _Ready()
        {
            Configure();
            CollectClassesToInject();
        }

        public static T? GetService<T>()
         where T : class
        {
            return s_serviceProvider?.GetService<T>();
        }

        public static void InjectDependencies(object instance)
        {
            var getClassToInject = ClassMetadataCollection?.FirstOrDefault(type => type.Type == instance.GetType());
            if (getClassToInject == null) { return; }

            foreach (var property in getClassToInject.Properties)
            {
                var dependency = s_serviceProvider?.GetService(property.PropertyType);
                if (dependency != null)
                {
                    property.SetValue(instance, dependency);
                }
            }
        }

        private static void Configure()
        {
            var provider = new ServiceCollection();

            provider.AddSingleton<IBasedOnRarityLootTable>(service =>
            {
                var instance = new BasedOnRarityLootTable();
                instance.InitializeLootTable();
                instance.ValidateTable();
                return instance;
            });
            provider.AddSingleton<IItemDataProvider<ItemStats>>(service =>
            {
                var instance = new ItemsStatsProvider();
                instance.LoadData();
                return instance;
            });
            provider.AddSingleton<IItemDataProvider<ItemMediaData>>(service =>
            {
                var instance = new ItemsMediaProvider();
                instance.LoadData();
                return instance;
            });
            provider.AddSingleton<RandomNumberGenerator>();
            s_serviceProvider = provider.BuildServiceProvider();
        }

        private static void CollectClassesToInject()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            ClassMetadataCollection = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<Inject>() != null)
                .Select(type => new ClassMetadata(type, type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Where(property => property.GetCustomAttribute<Inject>() != null).ToList())).ToList();
        }
    }
}
