namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Newtonsoft.Json;

    public class CraftingEffectProvider : ICraftingEffectProvider, IGameDataParticipant
    {
        private readonly List<CraftingEffectOption> _effects = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.ItemEffects];

        public IReadOnlyList<CraftingEffectOption> Effects => _effects;

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<CraftingEffectsData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize item effects file '{file.FileName}'");

            foreach (var entry in data.Effects)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    Tracker.TrackError($"Skipping item effect without an id in '{file.FileName}'");
                    continue;
                }

                if (!EnumParser.TryParseEnum<Core.Enums.GrantKind>(entry.Kind, out var kind))
                {
                    Tracker.TrackError($"Skipping item effect '{entry.Id}': unknown grant kind '{entry.Kind}'");
                    continue;
                }

                _effects.Add(new CraftingEffectOption(kind, entry.Id, entry.Properties) { Weight = entry.Weight });
            }
        }
    }
}
