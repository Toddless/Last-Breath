namespace Core.Entity
{
    using Enums;

    public interface IMinRarityModifier : INpcModifier
    {
        Rarity MinRarity { get; }
    }
}
