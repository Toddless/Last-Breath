namespace Core.Interfaces.Entity
{
    using Ai;
    using Enums;

    public interface IFightableNpc : IFightable
    {
        int Level { get; }
        Rarity Rarity { get; }
        EntityType EntityType { get; }
        Fractions Fraction { get; }
        INpcModifiersComponent NpcModifiers { get; }

        /// <summary>Combat AI archetype. Null = no brain: the legacy basic-attack turn.</summary>
        BehaviorProfile? Behavior { get; set; }
    }
}
