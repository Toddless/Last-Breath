namespace Core.Interfaces.Entity
{
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Components;

    public interface IEntity : IIdentifiable, IDisplayable, IFightable
    {
        IEffectsManager Effects { get; }
        IModifierManager Modifiers { get; }

        IEntityGroup? Group { get; set; }

        bool CanMove { get; set; }

        event Action<IEntity>? Dead;

        void AddItemToInventory(IItem item);
    }
}
