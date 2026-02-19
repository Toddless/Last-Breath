namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces.Entity;

    public class RarityUpgradeModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        int[] affectedRarity,
        float multiplier) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IRarityUpgradeModifier
    {
        public float BaseMultiplier { get; } = multiplier;
        public float Multiplier => BaseMultiplier * TotalScale;
        public int[] ChancesAffected { get; set; } = affectedRarity;

        public override INpcModifier Copy() => new RarityUpgradeModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, ChancesAffected, BaseMultiplier);
    }
}
