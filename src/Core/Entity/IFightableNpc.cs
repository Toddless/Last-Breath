namespace Core.Entity
{
    using Ai;

    public interface IFightableNpc : IFightable, INpc
    {
        INpcModifiersComponent NpcModifiers { get; }

        /// <summary>Combat AI archetype. Null = no brain: the legacy basic-attack turn.</summary>
        IBehaviorProfile? Behavior { get; set; }
        float RisingBonus { get; }
        bool IsRisen { get; }
    }
}
