namespace Battle.Source.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Data;
    using Core.Data.NpcBuffsData;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads NpcBuffs.json and turns NpcBuffId into parameter modifiers. This is the "some NPCs
    /// are unbeatable with default skills" lever: a modifier attaching to an NPC pulls its buff.
    /// </summary>
    public class NpcBuffProvider : INpcBuffProvider
    {
        private const string DataPath = "res://Data/NpcBuffs/";

        private readonly Dictionary<string, NpcBuffData> _buffs = [];

        /// <summary>Runtime constructor: loads the JSON asynchronously like every provider.</summary>
        public NpcBuffProvider() => _ = LoadDataAsync();

        /// <summary>Test constructor: applies the data directly, no Godot file access involved.</summary>
        public NpcBuffProvider(NpcBuffsData data) => ApplyData(data);

        public IReadOnlyList<IModifierInstance> CreateModifiers(string buffId, string source)
        {
            var buff = _buffs.GetValueOrDefault(buffId);
            if (buff == null) return []; // loot-only buff ids have no parameter side

            return buff.Modifiers
                .Select(entry => ModifiersCreator.CreateModifierInstance(
                    ParseEnum<EntityParameter>(entry.Parameter),
                    ParseEnum<ModifierValueType>(entry.Type),
                    entry.Value,
                    source))
                .ToList();
        }

        private void ApplyData(NpcBuffsData data)
        {
            foreach (var buff in data.Buffs)
                _buffs[buff.Id] = buff;
        }

        private static T ParseEnum<T>(string value) where T : struct, Enum =>
            Enum.TryParse(value, ignoreCase: true, out T result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(T).Name}");

        private async Task LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(DataPath, ParseBuffs);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load NPC buff data", e);
            }
        }

        private Task ParseBuffs(string json)
        {
            var data = JsonConvert.DeserializeObject<NpcBuffsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC buffs");
            ApplyData(data);
            return Task.CompletedTask;
        }
    }
}
