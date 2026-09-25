namespace Core.Entity.NpcModifiers
{
    using System;
    using Context;
    using Entity;

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

        /// <summary>
        /// Tier upgrades STACK (design: "Повышающие тир — стакаются"), so the steps add up: two of them
        /// on one NPC raise the drop by the sum, not by the better of the two.
        /// <para>The chances are separate rolls in the design, and the pipeline offers one: the honest
        /// composition of independent events is "at least one of them fires", 1 - Π(1 - p). That inflates
        /// no single modifier's own chance (alone it stays exactly its own p) and never reads as a sum.
        /// What a fired stack is worth is the full <see cref="UpgradeBy"/> total — the all-or-nothing shape
        /// the single roll forces. A per-modifier roll needs a chance/step list on the context; that is a
        /// loot-side change and does not belong in this pass.</para>
        /// <para>The clamp is load-bearing, not defensive: several scaling modifiers coexist on one NPC, so
        /// TotalScale reaches 6.1 and a 0.3 chance scales past certainty. Unclamped, two such modifiers
        /// would multiply two NEGATIVE remainders back into a positive product and hand the pair a chance
        /// BELOW either of them — a guaranteed upgrade turning into a rare one.</para>
        /// </summary>
        public override void ApplyModifier(IModifierApplyingContext context)
        {
            context.TierUpgradeBy += UpgradeBy;
            float chance = Math.Clamp(CurrentMultiplier, 0f, 1f);
            context.TierUpgradeChance = 1f - ((1f - context.TierUpgradeChance) * (1f - chance));
        }

        public override INpcModifier Copy() => new TierUpgradeModifier(Id, Weight, BaseDifficultyMultiplier, IsUnique, NpcBuffId, BaseMultiplier, UpgradeBy) { Group = Group, UniqueScope = UniqueScope };
    }
}
