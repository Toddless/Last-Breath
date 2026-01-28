namespace Core.Interfaces
{
    using System.Collections.Generic;

    public interface IModifierApplyingContext
    {
        float TierMultiplier { get; set; }
        float RarityMultiplier { get; set; }
        float TierUpgradeChance { get; set; }

        List<string> GuaranteedItems { get; set; }
        List<string> AdditionalItems { get; set; }
        List<string> AdditionalItemSkills { get; set; }
        List<string> AdditionalItemModifiers { get; set; }
    }
}
