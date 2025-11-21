namespace Core.Interfaces.Crafting
{
    using Enums;
    using System.Collections.Generic;

    public interface ICraftingMastery : IMastery
    {
        float GetResourceMultiplier(float resourceBonus = 0);
        float GetSkillChance(float skillBonus = 0);
        float GetValueMultiplier(float multiplierBonus = 0);
        Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = 0);
        Rarity RollRarity(float rarityBonus = 0);
    }
}
