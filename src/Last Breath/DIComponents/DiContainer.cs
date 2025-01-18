namespace Playground.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Godot;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class DiContainer : Node
    {
        public static List<ClassMetadata>? ClassMetadataCollection
        {
            get; private set;
        }

        public static IServiceProvider? ServiceProvider
        {
            get; set;
        }

        public override void _Ready()
        {
            Configure();
            CollectClassesToInject();
        }

        public static void Configure()
        {
            var provider = new ServiceCollection();

            provider.AddSingleton<IBasedOnRarityLootTable>(service =>
            {
                var instance = new BasedOnRarityLootTable();
                instance.InitializeLootTable();
                instance.ValidateTable();
                return instance;
            });
            provider.AddTransient<RandomNumberGenerator>();

            ServiceProvider = provider.BuildServiceProvider();
        }

        public static void CollectClassesToInject()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            ClassMetadataCollection = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<Inject>() != null)
                .Select(type => new ClassMetadata(type, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Where(field => field.GetCustomAttribute<Inject>() != null).ToList())).ToList();
        }

        public static void InjectDependencies(object instance)
        {
            var getClassToInject = ClassMetadataCollection?.FirstOrDefault(type => type.Type == instance.GetType());
            if (getClassToInject == null) { return; }

            foreach (var field in getClassToInject.Fields)
            {
                var dependency = ServiceProvider?.GetService(field.FieldType);
                if (dependency != null)
                {
                    field.SetValue(instance, dependency);
                }
            }
        }
    }
}
