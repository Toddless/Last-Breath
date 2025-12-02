namespace Core.Interfaces.Abilities
{
    using Enums;

    public interface IConditionalModifier
    {
        string Id { get; }
        AbilityParameter Parameter { get; }
        (float Value, ModifierType Type)? GetValue(EffectApplyingContext context);
    }
}
