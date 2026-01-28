namespace Core.Interfaces.Entity
{
    public interface INpcModifier : IIdentifiable, IDisplayable, IWeighable
    {
        float DifficultyMultiplier { get; }
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
