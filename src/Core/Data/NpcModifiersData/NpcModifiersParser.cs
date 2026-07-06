namespace Core.Data.NpcModifiersData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parses NpcModifiers.json into typed data grouped by modifier kind. Lives in Core so every
    /// project shares one parser (moved out of Utilities.DataParser — it needed no item factory).
    /// </summary>
    public static class NpcModifiersParser
    {
        public static Dictionary<string, List<NpcModifierData>> Parse(string json)
        {
            var data = JsonConvert.DeserializeObject<ModifiersData>(json) ?? throw new InvalidOperationException("Failed to deserialize NPC modifiers");
            var modifiers = new Dictionary<string, List<NpcModifierData>>();
            foreach (var npcModifiers in data.Mods)
            {
                List<NpcModifierData> mods = npcModifiers.Key switch
                {
                    "scale" => Deserialize<ScaleModifierData>(npcModifiers.Modifiers),
                    "tierUpgrade" => Deserialize<TierUpgradeData>(npcModifiers.Modifiers),
                    "guaranteedItems" => Deserialize<GuaranteedItemsData>(npcModifiers.Modifiers),
                    "tierMultiplier" => Deserialize<TierMultiplierData>(npcModifiers.Modifiers),
                    "itemEffects" => Deserialize<ItemEffectData>(npcModifiers.Modifiers),
                    "minRarity" => Deserialize<MinRarityModifierData>(npcModifiers.Modifiers),
                    "rarityUpgrade" => Deserialize<RarityUpgradeModifierData>(npcModifiers.Modifiers),
                    _ => []
                };
                modifiers.Add(npcModifiers.Key, mods);
            }

            return modifiers;
        }

        private static List<NpcModifierData> Deserialize<T>(List<JToken> tokens) where T : NpcModifierData =>
            tokens.Select(token => token.ToObject<T>()).Where(item => item != null).Cast<NpcModifierData>().ToList();
    }
}
