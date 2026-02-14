namespace LootGeneration.Source.NpcModifiers
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public class MinRarityModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        Rarity rarity) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IMinRarityModifier
    {
        public Rarity MinRarity { get; } = rarity;

        public override void ApplyModifier(IModifierApplyingContext context)
        {
            if (context.AtLeast < MinRarity) context.AtLeast = MinRarity;
        }

        public override INpcModifier Copy() => new MinRarityModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, MinRarity);
    }
}
