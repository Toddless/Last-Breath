namespace Core.Entity.NpcModifiers
{
    using Context;
    using Entity;

    public class ItemEffectsModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        bool isUnique,
        string npcBuffId,
        string effectId) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IItemEffectsModifier
    {
        public string EffectId { get; } = effectId;

        // The signature effect of the kill: equip drops of this NPC roll their bonus grant from the
        // listed ids instead of the whole catalog (payload still comes from the ItemEffects entry).
        public override void ApplyModifier(IModifierApplyingContext context) => context.AdditionalItemEffects.Add(EffectId);

        public override INpcModifier Copy() => new ItemEffectsModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, EffectId);
    }
}
