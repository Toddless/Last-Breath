namespace Core.Interfaces.Crafting
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;

    public interface ICraftingMastery
    {
        int BonusLevel { get; }
        int CurrentExp { get; }
        int CurrentLevel { get; }

        event Action<int>? BonusLevelChange;
        event Action<int>? ExpirienceChange;

        void AddExpirience(int bonusAmount);
        int ExpToNextLevelRemain();
        float GetFinalResourceMultiplier(float resourceBonus = 0);
        float GetFinalSkillChance(float skillBonus = 0);
        float GetFinalValueMultiplier(float multiplierBonus = 0);
        Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = 0);
        Rarity RollRarity(float rarityBonus = 0);
    }
}
