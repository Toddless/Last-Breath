namespace Core.Interfaces.CraftingResources
{
    using Core.Enums;

    public interface IMaterialModifiers
    {
        ModifierType ModifierType { get; set; }
        Parameter Parameter { get; set; }
        float Value { get; set; }
    }
}