namespace LootGeneration.Source.NpcModifiers
{
    using System;
    using System.Linq;
    using Core.Interfaces.Entity;
    using Core.Data.NpcModifiersData;
    using System.Collections.Generic;

    public class NpcModifiersFactory : INpcModifiersFactory
    {
        private readonly Dictionary<string, Func<List<NpcModifierData>, List<INpcModifier>>> _factories;

        public NpcModifiersFactory()
        {
            _factories = new Dictionary<string, Func<List<NpcModifierData>, List<INpcModifier>>>
            {
                ["scale"] = data =>
                    data.OfType<ScaleModifierData>()
                        .Select(scale => new ScaleModifier(scale.Id, scale.Weight, scale.Difficulty, scale.Scale, scale.IsUnique, scale.NpcBuffId))
                        .Cast<INpcModifier>().ToList(),
                ["tierUpgrade"] = data =>
                    data.OfType<TierUpgradeData>()
                        .Select(upgrade => new TierUpgradeModifier(upgrade.Id, upgrade.Weight, upgrade.Difficulty, upgrade.IsUnique, upgrade.NpcBuffId, upgrade.UpgradeMultiplier,
                            upgrade.UpgradeBy))
                        .Cast<INpcModifier>().ToList(),
                ["guaranteedItems"] = data =>
                    data.OfType<GuaranteedItemsData>()
                        .Select(guaranteed =>
                            new GuaranteedItemsModifier(guaranteed.Id, guaranteed.Weight, guaranteed.Difficulty, guaranteed.IsUnique, guaranteed.NpcBuffId, guaranteed.Items))
                        .Cast<INpcModifier>().ToList(),
                ["tierMultiplier"] = data =>
                    data.OfType<TierMultiplierData>()
                        .Select(tier => new TierMultiplierModifier(tier.Id, tier.Weight, tier.Difficulty, tier.IsUnique, tier.NpcBuffId, tier.Multiplier, tier.AffectedTiers))
                        .Cast<INpcModifier>().ToList(),
                ["itemEffects"] = data =>
                    data.OfType<ItemEffectData>()
                        .Select(item => new ItemEffectsModifier(item.Id, item.Weight, item.Difficulty, item.IsUnique, item.NpcBuffId, item.EffectId))
                        .Cast<INpcModifier>().ToList(),
                ["minRarity"] = data =>
                    data.OfType<MinRarityModifierData>()
                        .Select(rarity => new MinRarityModifier(rarity.Id, rarity.Weight, rarity.Difficulty, rarity.IsUnique, rarity.NpcBuffId, rarity.Rarity))
                        .Cast<INpcModifier>().ToList(),
                ["rarityUpgrade"] = data =>
                    data.OfType<RarityUpgradeModifierData>()
                        .Select(rarity => new RarityUpgradeModifier(rarity.Id, rarity.Weight, rarity.Difficulty, rarity.IsUnique, rarity.NpcBuffId, rarity.AffectedRarity,
                            rarity.Multiplier))
                        .Cast<INpcModifier>().ToList(),
            };
        }

        public List<INpcModifier> CreateNpcModifiers(Dictionary<string, List<NpcModifierData>> data)
        {
            var npcMods = new List<INpcModifier>();
            foreach (KeyValuePair<string, List<NpcModifierData>> valuePair in data)
            {
                if (_factories.TryGetValue(valuePair.Key, out var creator))
                    npcMods.AddRange(creator(valuePair.Value));
            }

            return npcMods;
        }
    }
}
