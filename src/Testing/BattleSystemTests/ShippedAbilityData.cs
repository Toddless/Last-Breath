namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.GameData;

    /// <summary>
    /// The shipped ability data as the game reads it: the real source, the real loader, the real
    /// parsers. Two participants read that one catalog — the augment records are Core's, because
    /// every composition needs them, while the abilities and the code that builds an augment are the
    /// battle module's — so a test asking about either takes both out of a single load.
    /// </summary>
    internal static class ShippedAbilityData
    {
        /// <summary>The abilities the shipped files declare, already able to offer augments.</summary>
        internal static AbilityProvider Abilities() => Load().Abilities;

        /// <summary>The augment records the shipped files declare.</summary>
        internal static AbilityAugmentCatalog Augments() => Load().Augments;

        internal static (AbilityProvider Abilities, AbilityAugmentCatalog Augments) Load() => LoadFrom(SharedData.Root());

        /// <summary>The augment records of a catalog a test wrote, read through the same loader the
        /// game runs — the fitting rule reads fields no hand-built record can prove are ever parsed,
        /// and a case that writes its own pair keeps that pair whatever the shipped markup becomes.</summary>
        internal static AbilityAugmentCatalog CatalogOver(string json)
        {
            string root = Directory.CreateTempSubdirectory("augments_").FullName;
            Directory.CreateDirectory(Path.Combine(root, DataCatalog.Abilities));
            File.WriteAllText(Path.Combine(root, DataCatalog.Abilities, "Abilities.json"), json);

            AbilityAugmentCatalog catalog = LoadFrom(root).Augments;
            Directory.Delete(root, recursive: true);
            return catalog;
        }

        /// <summary>The same pair over any data root — for records the shipped files do not declare.</summary>
        internal static (AbilityProvider Abilities, AbilityAugmentCatalog Augments) LoadFrom(string root)
        {
            var augments = new AbilityAugmentCatalog();
            var abilities = new AbilityProvider(augments, static () => new EffectProvider());
            var service = new GameDataService(new FileSystemDataSource(root), [abilities, augments]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return (abilities, augments);
        }
    }
}
