namespace Core.Interfaces.Components
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using System.Collections.Generic;

    public interface IModifierManager
    {
        IReadOnlyDictionary<Parameter, List<IModifierInstance>> BattleModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IModifierInstance>> PermanentModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IModifierInstance>> TemporaryModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

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
