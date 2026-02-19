namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public class GuaranteedItemsModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        List<string> items) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IGuaranteedItemsModifier
    {
        public List<string> Items { get; } = items;

        public override void ApplyModifier(IModifierApplyingContext context) => context.GuaranteedItems.AddRange(Items);

        public override INpcModifier Copy() => new GuaranteedItemsModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, Items);
    }
}
