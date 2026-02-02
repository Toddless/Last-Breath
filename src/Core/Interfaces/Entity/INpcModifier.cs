namespace Core.Interfaces.Entity
{
    public interface INpcModifier : IIdentifiable, IDisplayable, IWeightable
    {
        float BaseDifficultyMultiplier { get; }
        float DifficultyMultiplier { get; set; }
        bool IsUnique { get; }
        string NpcBuffId { get; }

        void Attach(IEntity to);
        void Detach(IEntity from);
        void ApplyModifier(IModifierApplyingContext context);
        void ScaleUp(IScaleModifier modifier);
        void ScaleDown(IScaleModifier modifier);

        INpcModifier Copy();
    }
}
