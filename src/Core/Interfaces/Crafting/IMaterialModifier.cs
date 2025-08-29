namespace Core.Interfaces.Crafting
{
    using Core.Enums;

    public interface IMaterialModifier
    {
        ModifierType ModifierType { get; }
        Parameter Parameter { get; }
        float Value { get; }
        float Weight { get; }

        bool IsSame(IMaterialModifier other);
    }
}
