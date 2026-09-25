namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The effect registry over a canon, put together the way a game puts it together: the catalog reads
    /// the files and the registry asks it for what they say. Built here in one place so that a walk needs
    /// neither half of that arrangement written out again — and so a walk moving a canonical figure moves
    /// the catalog the registry actually answers from.
    /// </summary>
    internal static class EffectProviders
    {
        /// <summary>The registry over the shipped canonical numbers, read through the real parser.</summary>
        internal static EffectProvider FromShippedData()
        {
            var catalog = new EffectCanonCatalog();
            ShippedRowsInto(catalog);
            return new EffectProvider(catalog);
        }

        /// <summary>The shipped canon inside the process-wide composition: the registry is put up over
        /// the catalog the composition already holds — a shared data participant — and the rows are read
        /// into THAT catalog, so a walk moving a figure moves the one the registry answers from.
        /// Composition is idempotent and the rows are read once; a walk that substituted a row puts the
        /// shipped ones back itself.</summary>
        internal static void ComposeShipped()
        {
            GameServiceProvider.Initialize(services => services
                .AddSingleton<IEffectProvider>(sp => new EffectProvider(sp.GetRequiredService<IEffectCanonCatalog>())));

            IEffectCanonCatalog catalog = ComposedCanon();
            if (catalog.Ids.Count == 0) ShippedRowsInto((IGameDataParticipant)catalog);
        }

        /// <summary>The canonical catalog the composed registry reads from.</summary>
        internal static IEffectCanonCatalog ComposedCanon()
        {
            IEffectCanonCatalog? catalog = GameServiceProvider.TryGet<IEffectCanonCatalog>();
            Assert.IsNotNull(catalog, "the composition holds no canonical catalog, so no canon can be put under the registry");
            return catalog;
        }

        /// <summary>The registry over a canon a test wrote — one string per file, read through the same
        /// parser the game runs.</summary>
        internal static EffectProvider FromJson(params string[] json)
        {
            var catalog = new EffectCanonCatalog();
            for (int index = 0; index < json.Length; index++)
                catalog.Apply(DataCatalog.Effects, new GameDataFile($"Effects_{index}.json", json[index]));

            return new EffectProvider(catalog);
        }

        /// <summary>The registry with no canon behind it at all — for walks about the factories alone.</summary>
        internal static EffectProvider WithoutCanon() => new(new EffectCanonCatalog());

        /// <summary>Every shipped canonical file into a catalog, the way the loader hands them over.</summary>
        internal static void ShippedRowsInto(IGameDataParticipant catalog)
        {
            foreach (string file in Directory.EnumerateFiles(
                         SharedData.Catalog(DataCatalog.Effects), "*.json", SearchOption.AllDirectories))
                catalog.Apply(DataCatalog.Effects, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));
        }
    }
}
