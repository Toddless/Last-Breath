namespace Core.Entity.NpcModifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.NpcModifiersData;
    using Entity;

    public class NpcModifiersFactory : INpcModifiersFactory
    {
        // The section key is passed to every creator, not just used to pick one: it is the modifier's
        // group, and uniqueness on the bearer is read per group. Losing it here is how "one min-rarity
        // floor at a time" would quietly become "all three at once".
        private readonly Dictionary<string, Func<string, List<NpcModifierData>, List<INpcModifier>>> _factories;

        public NpcModifiersFactory()
        {
            _factories = new Dictionary<string, Func<string, List<NpcModifierData>, List<INpcModifier>>>
            {
                ["scale"] = (group, data) =>
                    data.OfType<ScaleModifierData>()
                        .Select(scale => new ScaleModifier(scale.Id, scale.Weight, scale.Difficulty, scale.Scale, scale.IsUnique, scale.NpcBuffId) { Group = group, UniqueScope = scale.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["tierUpgrade"] = (group, data) =>
                    data.OfType<TierUpgradeData>()
                        .Select(upgrade => new TierUpgradeModifier(upgrade.Id, upgrade.Weight, upgrade.Difficulty, upgrade.IsUnique, upgrade.NpcBuffId, upgrade.TierUpgradeChance,
                            upgrade.UpgradeBy) { Group = group, UniqueScope = upgrade.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["guaranteedItems"] = (group, data) =>
                    data.OfType<GuaranteedItemsData>()
                        .Select(guaranteed =>
                            new GuaranteedItemsModifier(guaranteed.Id, guaranteed.Weight, guaranteed.Difficulty, guaranteed.IsUnique, guaranteed.NpcBuffId, guaranteed.Items) { Group = group, UniqueScope = guaranteed.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["tierMultiplier"] = (group, data) =>
                    data.OfType<TierMultiplierData>()
                        .Select(tier => new TierMultiplierModifier(tier.Id, tier.Weight, tier.Difficulty, tier.IsUnique, tier.NpcBuffId, tier.Multiplier, tier.AffectedTiers) { Group = group, UniqueScope = tier.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["itemEffects"] = (group, data) =>
                    data.OfType<ItemEffectData>()
                        .Select(item => new ItemEffectsModifier(item.Id, item.Weight, item.Difficulty, item.IsUnique, item.NpcBuffId, item.EffectId) { Group = group, UniqueScope = item.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["minRarity"] = (group, data) =>
                    data.OfType<MinRarityModifierData>()
                        .Select(rarity => new MinRarityModifier(rarity.Id, rarity.Weight, rarity.Difficulty, rarity.IsUnique, rarity.NpcBuffId, rarity.Rarity) { Group = group, UniqueScope = rarity.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
                ["rarityUpgrade"] = (group, data) =>
                    data.OfType<RarityUpgradeModifierData>()
                        .Select(rarity => new RarityUpgradeModifier(rarity.Id, rarity.Weight, rarity.Difficulty, rarity.IsUnique, rarity.NpcBuffId, rarity.AffectedRarity,
                            rarity.Multiplier) { Group = group, UniqueScope = rarity.UniqueScope })
                        .Cast<INpcModifier>().ToList(),
            };
        }

        public List<INpcModifier> CreateNpcModifiers(Dictionary<string, List<NpcModifierData>> data)
        {
            var npcMods = new List<INpcModifier>();
            foreach (KeyValuePair<string, List<NpcModifierData>> valuePair in data)
            {
                if (_factories.TryGetValue(valuePair.Key, out var creator))
                    npcMods.AddRange(creator(valuePair.Key, valuePair.Value));
            }

            return npcMods;
        }
    }
}
