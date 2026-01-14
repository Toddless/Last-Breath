namespace Core.Interfaces.Abilities
{
    public interface ICondition
    {
        string Id { get; }

        bool IsMet(EffectApplyingContext context);
    }
}
