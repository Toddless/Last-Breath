namespace Core.Interfaces.Components
{
    using System;
    using Enums;
    using Interfaces;
    using System.Collections.Generic;

    public interface IModifierManager
    {
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> BattleModifiers { get; }
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> PermanentModifiers { get; }
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> TemporaryModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

        IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter);

        void AddBattleModifier(IModifierInstance modifier);
        void AddPermanentModifier(IModifierInstance modifier);
        void AddTemporaryModifier(IModifierInstance modifier);
        void RemoveAllBattleModifiers();
        void RemoveAllTemporaryModifiers();
        void RemoveBattleModifier(IModifierInstance modifier);
        void RemoveBattleModifierBySource(object source);
        void RemovePermanentModifier(IModifierInstance modifier);
        void RemovePermanentModifierBySource(object source);
        void RemoveTemporaryModifier(IModifierInstance modifier);
        void RemoveTemporaryModifierBySource(object source);
        void UpdateBattleModifier(IModifierInstance modifier);
        void UpdatePermanentModifier(IModifierInstance modifier);
        void UpdateTemporaryModifier(IModifierInstance modifier);
    }
}
