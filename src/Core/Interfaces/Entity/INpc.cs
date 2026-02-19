namespace Core.Interfaces.Entity
{
    using Enums;

    public interface INpc : IEntity
    {
        int Level { get; }
        Rarity Rarity { get; }
        EntityType EntityType { get; }
        Fractions Fraction { get; }
        INpcModifiersComponent NpcModifiers { get; }
    }
}
