namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Consumes the NpcSpawnRolls catalog: the per-EntityType chance ladders that decide how many of
    /// an NPC's modifier and ability slots actually get filled, plus the rarity ladder that lifts or
    /// lowers all of them at once.
    /// <para>
    /// A type the file does not name (and a file that never arrives) answers 1 for every slot, which
    /// is the pre-catalog behaviour — every slot the ceiling offers is taken. That is the safe
    /// failure: a data folder someone forgot to ship makes NPCs as they were last month, not NPCs
    /// stripped of everything they own. It is the ONLY thing that default covers: a ladder the file does
    /// name has to name all three of its fields or the file is refused (see <see cref="Field"/>), because
    /// a typo in a field name would otherwise buy that same silence for a type nobody meant to exempt.
    /// <see cref="Enums.EntityType.Boss"/> and
    /// <see cref="Enums.EntityType.Archon"/> deliberately name no ability ladder — their slot count is
    /// "every ability of the stance pool", an authored promise rather than a thing to roll against.
    /// </para>
    /// </summary>
    public class NpcSpawnRollsProvider : INpcSpawnRollsProvider, IGameDataParticipant
    {
        private readonly Dictionary<EntityType, SlotLadder> _modifiers = [];
        private readonly Dictionary<EntityType, SlotLadder> _abilities = [];
        private readonly Dictionary<Rarity, float> _rarityMultipliers = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.NpcSpawnRolls];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<SpawnRollsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC spawn roll data");

            // Read the whole file before keeping any of it: a half-applied balance file is the one shape
            // this participant must never end up in, because the types that failed to arrive answer
            // "every slot is certain" and nothing downstream can tell that from an authored ladder.
            var modifiers = ReadLadders("modifiers", data.Modifiers);
            var abilities = ReadLadders("abilities", data.Abilities);
            Dictionary<Rarity, float> rarities = [];
            foreach ((string rarity, float multiplier) in data.RarityMultipliers)
                rarities[EnumParser.ParseEnum<Rarity>(rarity)] = multiplier;

            foreach ((var type, var ladder) in modifiers) _modifiers[type] = ladder;
            foreach ((var type, var ladder) in abilities) _abilities[type] = ladder;
            foreach ((var rarity, float multiplier) in rarities) _rarityMultipliers[rarity] = multiplier;
        }

        public float ModifierSlotChance(EntityType type, Rarity rarity, int slotIndex) =>
            SlotChance(_modifiers, type, rarity, slotIndex);

        public float AbilitySlotChance(EntityType type, Rarity rarity, int slotIndex) =>
            SlotChance(_abilities, type, rarity, slotIndex);

        private static Dictionary<EntityType, SlotLadder> ReadLadders(string section, Dictionary<string, SlotLadderData> source)
        {
            Dictionary<EntityType, SlotLadder> ladders = [];
            foreach ((string type, SlotLadderData ladder) in source)
                ladders[EnumParser.ParseEnum<EntityType>(type)] = new SlotLadder(
                    Field(section, type, "firstSlotChance", ladder.FirstSlotChance),
                    Field(section, type, "nextSlotChance", ladder.NextSlotChance),
                    Field(section, type, "decay", ladder.Decay));
            return ladders;
        }

        /// <summary>A ladder that is written down has to be written down whole. There is no sane default
        /// for a missing field here: the only value that keeps the arithmetic running is 1, and 1 reads as
        /// "this slot is certain" — so a typo in a field name (firstSlotChanse) would hand the type back
        /// the always-maximum spawn this catalog exists to end, and say nothing while doing it.</summary>
        private static float Field(string section, string type, string name, float? value) =>
            value ?? throw new FormatException(
                $"The {section} ladder of '{type}' names no '{name}'. All three of firstSlotChance, " +
                "nextSlotChance and decay are required of every ladder the file names.");

        /// <summary>The whole mechanic in four lines: the first slot has a chance of its own, every later
        /// one starts from the second-slot chance and is worn down by the decay once per step, and the
        /// rarity of the NPC scales the lot. The result is clamped, so a rarity multiplier above one
        /// turns the top of a ladder into a certainty instead of into nonsense.</summary>
        private float SlotChance(Dictionary<EntityType, SlotLadder> ladders, EntityType type, Rarity rarity, int slotIndex)
        {
            if (!ladders.TryGetValue(type, out var ladder)) return 1f;

            float raw = slotIndex <= 0
                ? ladder.FirstSlotChance
                : ladder.NextSlotChance * MathF.Pow(ladder.Decay, slotIndex - 1);
            return Math.Clamp(raw * _rarityMultipliers.GetValueOrDefault(rarity, 1f), 0f, 1f);
        }

        private readonly record struct SlotLadder(float FirstSlotChance, float NextSlotChance, float Decay);

        private record SpawnRollsData
        {
            [JsonProperty("modifiers")] public Dictionary<string, SlotLadderData> Modifiers { get; init; } = [];
            [JsonProperty("abilities")] public Dictionary<string, SlotLadderData> Abilities { get; init; } = [];
            [JsonProperty("rarityMultipliers")] public Dictionary<string, float> RarityMultipliers { get; init; } = [];
        }

        /// <summary>Nullable on purpose: absent is a question the file has to answer (see
        /// <see cref="Field"/>), not a number to guess at.</summary>
        private record SlotLadderData
        {
            [JsonProperty("firstSlotChance")] public float? FirstSlotChance { get; init; }
            [JsonProperty("nextSlotChance")] public float? NextSlotChance { get; init; }
            [JsonProperty("decay")] public float? Decay { get; init; }
        }
    }
}
