namespace Core.Interfaces.Abilities
{
    using Enums;
    using System;
    using Battle.Module;
    using Battle.Decorator;
    using System.Collections.Generic;

    public interface IAbility : IIdentifiable, IDisplayable
    {
        float MaxTargets { get; }
        float AvailablePoints { get; set; }
        float Cooldown { get; }
        IAbilityCost Cost { get; }
        List<IEffect> Effects { get; set; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; }

        event Action<AbilityParameter> OnParameterChanged;

        void Activate(AbilityContext context);

        void AddParameterUpgrade<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
            where T : struct, Enum;

        void RemoveParameterUpgrade<T>(string id, T key)
            where T : struct, Enum;
    }
}
