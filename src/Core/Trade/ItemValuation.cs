namespace Core.Trade
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Data;
    using Items;

    /// <summary>
    /// The single gold-pricing point (Calculations-style): the value of a CONCRETE instance —
    /// the same blueprint prices differently at another rarity, sharpening level or after ascension.
    /// The base comes from the item data (blueprint for equips, the item itself for plain goods);
    /// an unpriced item values to 0 (untradable until the data authors its base).
    /// </summary>
    public interface IItemValuation
    {
        int Value(IItem item);
    }

    /// <param name="blueprints">Where an equip's authored base is written; a composition without it
    /// values no gear.</param>
    /// <param name="augments">What an augment id means. An augment carries no authored base at all —
    /// its price is computed from the tier its record declares — so a composition without the catalog
    /// values none of them. Rarity comes from the copy, like every other item's.</param>
    public class ItemValuation(
        ITradeConfigProvider configProvider,
        IEquipBlueprintProvider? blueprints = null,
        IAbilityAugmentCatalog? augments = null) : IItemValuation
    {
        public int Value(IItem item)
        {
            var config = configProvider.Config;
            float price = Base(item, config);
            if (price <= 0) return 0;

            price *= config.RarityMultipliers.GetValueOrDefault(item.Rarity, 1f);
            if (item is IEquipItem instance)
            {
                price *= 1f + config.UpgradeLevelBonus * instance.UpdateLevel;
                if (instance.IsSealed) price *= config.AscensionMultiplier;
            }

            return (int)MathF.Round(price);
        }

        /// <summary>What the instance is worth before rarity — the one figure every kind of item
        /// answers differently. Rarity is applied above, on whatever comes back, because it means the
        /// same thing whether it was authored on a blueprint or declared by an augment record.</summary>
        private float Base(IItem item, TradeConfig config) => item switch
        {
            IEquipItem equip => blueprints?.GetBlueprint(equip.Id)?.BasePrice ?? 0f,
            IAugmentItem augment => AugmentBase(augment, config),
            _ => item.BasePrice,
        };

        /// <summary>An augment's base, computed rather than authored: tier is the whole of what one
        /// augment is worth over another of the same rarity, and it is already on the record. The copy's
        /// NUMBERS never enter — a blade prices off its blueprint, not off the values its lines rolled —
        /// while the copy's rarity does, above, where every item's rarity is applied.</summary>
        private float AugmentBase(IAugmentItem augment, TradeConfig config) =>
            augments?.Find(augment.Augment.AugmentId) is { } record
                ? config.AugmentBasePrice * MathF.Pow(config.AugmentTierMultiplier, record.Tier)
                : 0f;
    }
}
