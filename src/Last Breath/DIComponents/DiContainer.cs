namespace Playground.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Godot;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.QuestSystem;

    public partial class DiContainer : Node
    {
        private static List<ClassMetadata>? ClassMetadataCollection
        {
            get; set;
        }

        public static IServiceProvider? ServiceProvider
        {
            get;
            private set;
        }

        public override void _Ready()
        {
            Configure();
            CollectClassesToInject();
        }

        public static void InjectDependencies(object instance)
        {
            var getClassToInject = ClassMetadataCollection?.FirstOrDefault(type => type.Type == instance.GetType());
            if (getClassToInject == null) { return; }

            foreach (var property in getClassToInject.Properties)
            {
                var dependency = ServiceProvider?.GetService(property.PropertyType);
                if (dependency != null)
                {
                    property.SetValue(instance, dependency);
                }
            }
        }

        public static T? GetService<T>()
            where T : class
        {
            return ServiceProvider?.GetService<T>();
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
            provider.AddSingleton<QuestManager>();
            provider.AddSingleton<RandomNumberGenerator>();
            provider.AddTransient<IConditionsFactory, ConditionsFactory>();
            ServiceProvider = provider.BuildServiceProvider();
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
