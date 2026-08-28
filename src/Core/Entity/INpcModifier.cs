namespace Core.Entity
{
    using Context;
    using Interfaces;

    public interface INpcModifier : IIdentifiable, IDisplayable, IWeightable
    {
        float BaseDifficultyMultiplier { get; }
        float DifficultyMultiplier { get; }

        /// <summary>Everything the scaling modifiers added on top of 1: the multiplier a buff value of
        /// this modifier is worth on the bearer.</summary>
        float TotalScale { get; }
        bool IsUnique { get; }
        string NpcBuffId { get; }

        /// <summary>The catalog section this modifier was born in ("scale", "minRarity", ...).
        /// Empty for a modifier built by hand rather than from the catalog.</summary>
        string Group { get; }

        /// <summary>How far this modifier's uniqueness reaches — the whole <see cref="Group"/> or just its
        /// own <see cref="IIdentifiable.Id"/>. Authored per section in NpcModifiers.json.</summary>
        Enums.NpcUniqueScope UniqueScope { get; }

        void Attach(IFightable to);
        void Detach(IFightable from);
        void ApplyModifier(IModifierApplyingContext context);
        void ScaleUp(IScaleModifier modifier);
        void ScaleDown(IScaleModifier modifier);

        INpcModifier Copy();
    }
}
