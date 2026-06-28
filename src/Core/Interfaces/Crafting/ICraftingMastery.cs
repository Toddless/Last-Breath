namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Enums;

    public interface ICraftingMastery : IMastery
    {
        float GetCurrentResourceMultiplier(float resourceBonus = 0);
        float GetCurrentSkillChance(float skillBonus = 0);
        float GetCurrentValueMultiplier(float multiplierBonus = 0);
        Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = 0);
        Rarity RollRarity(float rarityBonus = 0);
        float GetCurrentMinRange();
        float GetCurrentMaxRange();
        float GetRandomValueRange();
    }
}
