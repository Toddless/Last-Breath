namespace Core.Interfaces.Entity
{
    using Enums;
    using Items;
    using System;
    using Components;
    using Interfaces;

    public interface IEntity : IIdentifiable, IDisplayable, IFightable
    {
        IEffectsComponent Effects { get; }
        IModifiersComponent Modifiers { get; }
        IEntityParametersComponent Parameters { get; }
        IPassiveSkillsComponent PassiveSkills { get; }

        IEntityAttribute Dexterity { get; }
        IEntityAttribute Strength { get; }
        IEntityAttribute Intelligence { get; }

        IEntityGroup? Group { get; set; }

        StatusEffects StatusEffects { get; }

        bool CanMove { get; set; }

        float CurrentHealth { get; set; }
        float CurrentBarrier { get; set; }
        float CurrentMana { get; set; }

        event Action<float>? CurrentManaChanged;
        event Action<float>? CurrentBarrierChanged;
        event Action<float>? CurrentHealthChanged;
        event Action<IEntity>? Dead;

        void AddItemToInventory(IItem item);

        float GetDamage();

        void Heal(float amount);
        void ConsumeResource(Costs type, float amount);
        bool TryApplyStatusEffect(StatusEffects statusEffect);
        bool TryRemoveStatusEffect(StatusEffects statusEffect);
    }
}
