namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public class TierUpgradeModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        float tierUpgradeChance,
        int upgradeBy)
        : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), ITierUpgradeModifier
    {
        public float BaseMultiplier { get; } = tierUpgradeChance;
        public float CurrentMultiplier => BaseMultiplier * TotalScale;
        public int UpgradeBy { get; } = upgradeBy;

        public override void ApplyModifier(IModifierApplyingContext context)
        {
            if (context.TierUpgradeBy > UpgradeBy) return;
            context.TierUpgradeBy = UpgradeBy;
            context.TierUpgradeChance = CurrentMultiplier;
        }

        public override INpcModifier Copy() => new TierUpgradeModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, BaseMultiplier, UpgradeBy);
    }
}
