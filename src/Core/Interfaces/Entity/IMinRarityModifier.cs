namespace Core.Interfaces.Entity
{
    using Enums;

    public interface IMinRarityModifier : INpcModifier
    {
        Rarity MinRarity { get; }
    }
}
