namespace LootGeneration.Services
{
    using Source;
    using System.Linq;
    using System.Collections.Generic;

    public class ItemEffectProvider : IItemEffectProvider
    {
        private readonly List<string> _itemEffectsIds =
        [
            "Passive_Skill_Chain_Attack",
            "Passive_Skill_Counter_Attack",
            "Passive_Skill_Echo",
            "Passive_Skill_Execute",
            "Passive_Skill_Servant_Hell",
            "Passive_Skill_LuckyCriticalChance",
            "Passive_Skill_Multicast",
            "Passive_Skill_Poisoned_Claws",
            "Passive_Skill_Porcupine",
            "Passive_Skill_Regeneration",
            "Passive_Skill_SoulDevouring",
            "Passive_Skill_Trapped_Beast",
            "Passive_Skill_Vampier",
            "Passive_Skill_Mana_Burn",
            "Passive_Skill_Burning",
            "Passive_Skill_Gift_From_The_Goddess",
            "Passive_Skill_Bleeding",
            "Effect_Unlucky_Critical_Chance",
            "Effect_Regeneration",
            "Effect_Lucky_Crit_Chance",
            "Effect_Life_Giving_Shade_Regeneration",
            "Effect_Execution",
            "Effect_Evade_First_Death",
            "Effect_Curse"
        ];

        public List<string> GetCopyItemsEffects() => _itemEffectsIds.ToList();
    }
}
