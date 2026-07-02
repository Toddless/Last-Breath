namespace Core.Interfaces.Entity
{
    using Components;
    using Items;

    /// <summary>
    /// Base contract for every world entity: player, fightable and peaceful NPCs.
    /// Contains only what any entity has — identity, presentation and world presence.
    /// Combat capabilities live in <see cref="IFightable"/>.
    /// </summary>
    public interface IEntity : IIdentifiable, IDisplayable
    {
        IAnimationsComponent Animations { get; }
        bool CanMove { get; set; }

        void AddItemToInventory(IItem item);
    }
}
