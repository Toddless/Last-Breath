namespace Core.Entity
{
    using Context;
    using Interfaces;

    public interface INpcModifier : IIdentifiable, IDisplayable, IWeightable
    {
        float BaseDifficultyMultiplier { get; }
        float DifficultyMultiplier { get; }
        bool IsUnique { get; }
        string NpcBuffId { get; }

        void Attach(IFightable to);
        void Detach(IFightable from);
        void ApplyModifier(IModifierApplyingContext context);
        void ScaleUp(IScaleModifier modifier);
        void ScaleDown(IScaleModifier modifier);

        INpcModifier Copy();
    }
}
