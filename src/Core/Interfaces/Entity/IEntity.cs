namespace Core.Interfaces.Entity
{
    using Enums;
    using Items;
    using Components;
    using Interfaces;

    public interface IEntity : IIdentifiable, IDisplayable, IFightable
    {
        IEffectsManager Effects { get; }
        IModifierManager Modifiers { get; }

        IEntityGroup? Group { get; set; }

        StatusEffects StatusEffects { get; set; }

        bool CanMove { get; set; }

        void AddItemToInventory(IItem item);
    }
}
