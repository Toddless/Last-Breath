namespace Core.Results
{
    using Modifiers;

    public record AscensionResult(bool Succeeded, IModifierInstance? GiftedModifier);
}
