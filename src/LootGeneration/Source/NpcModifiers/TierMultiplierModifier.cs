namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces.Entity;

    public class TierMultiplierModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        float multiplier,
        int[] affectedTiers) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), ITierMultiplierModifier
    {
        public float BaseMultiplier { get; } = multiplier;
        public float Multiplier => BaseMultiplier * TotalScale;
        public int[] ChancesAffected { get; } = affectedTiers;

        public override INpcModifier Copy() => new TierMultiplierModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, BaseMultiplier, ChancesAffected);
    }
}
