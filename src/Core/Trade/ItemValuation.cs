namespace Core.Trade
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Items;

    /// <summary>
    /// The single gold-pricing point (Calculations-style): the value of a CONCRETE instance —
    /// the same blueprint prices differently at another rarity, sharpening level or after ascension.
    /// basePrice comes from the item data (blueprint for equips, the item itself for plain goods);
    /// an unpriced item values to 0 (untradable until the data authors its base).
    /// </summary>
    public interface IItemValuation
    {
        int Value(IItem item);
    }

    public class ItemValuation(ITradeConfigProvider configProvider, IEquipBlueprintProvider? blueprints = null) : IItemValuation
    {
        public int Value(IItem item)
        {
            var config = configProvider.Config;
            float price = item is IEquipItem equip
                ? blueprints?.GetBlueprint(equip.Id)?.BasePrice ?? 0
                : item.BasePrice;
            if (price <= 0) return 0;

            price *= config.RarityMultipliers.GetValueOrDefault(item.Rarity, 1f);
            if (item is IEquipItem instance)
            {
                price *= 1f + config.UpgradeLevelBonus * instance.UpdateLevel;
                if (instance.IsSealed) price *= config.AscensionMultiplier;
            }

            return (int)MathF.Round(price);
        }
    }
}
