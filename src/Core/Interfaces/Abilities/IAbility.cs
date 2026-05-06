namespace Core.Interfaces.Abilities
{
    using Enums;
    using Entity;
    using System;
    using Components.Module;
    using Components.Decorator;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Battle;

    public interface IAbility : IIdentifiable, IDisplayable, ITaggable
    {
        float Cooldown { get; }
        float Damage { get; }
        float WeaponDamageScale { get; }
        float SpellDamageScale { get; }
        int CooldownLeft { get; set; }
        int CostValue { get; }
        bool IsEvadable { get; set; }
        Costs CostType { get; }
        AbilityType AbilityType { get; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; }

        event Action<Enum>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        Task Execute(List<IEntity> targets, IBattleField field);

        void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator) where T : struct, Enum;
        void RemoveParameterDecorator<T>(string id, T key) where T : struct, Enum;
        void SetOwner(IEntity owner);
        bool IsEnoughResource();
        void RemoveOwner();
    }
}
