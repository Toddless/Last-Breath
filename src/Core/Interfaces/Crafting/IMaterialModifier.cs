namespace Core.Interfaces.Crafting
{
    using Core.Enums;

    public interface IMaterialModifier
    {
        ModifierType ModifierType { get; set; }
        Parameter Parameter { get; set; }
        float Value { get; set; }
    }
}
