namespace Core.Interfaces.Entity
{
    using Enums;

    public interface IRarityUpgradeModifier : INpcModifier
    {
        Rarity MinRarity { get; }
    }
}
