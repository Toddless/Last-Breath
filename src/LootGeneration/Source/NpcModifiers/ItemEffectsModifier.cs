namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces.Entity;

    public class ItemEffectsModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        string effectId) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IItemEffectsModifier
    {
        public string EffectId { get; } = effectId;

        public override INpcModifier Copy() => new ItemEffectsModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, EffectId);
    }
}
