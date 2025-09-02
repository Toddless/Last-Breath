namespace Core.Interfaces.Components
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers;

    public interface IModifierManager
    {
        IReadOnlyDictionary<Parameter, List<IModifier>> BattleModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IModifier>> PermanentModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IModifier>> TemporaryModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

        void AddBattleModifier(IModifier modifier);
        void AddPermanentModifier(IModifier modifier);
        void AddTemporaryModifier(IModifier modifier);
        void RemoveAllBattleModifiers();
        void RemoveAllTemporaryModifiers();
        void RemoveBattleModifier(IModifier modifier);
        void RemoveBattleModifierBySource(object source);
        void RemovePermanentModifier(IModifier modifier);
        void RemovePermanentModifierBySource(object source);
        void RemoveTemporaryModifier(IModifier modifier);
        void RemoveTemporaryModifierBySource(object source);
        void UpdateBattleModifier(IModifier modifier);
        void UpdatePermanentModifier(IModifier modifier);
        void UpdateTemporaryModifier(IModifier modifier);
    }
}
