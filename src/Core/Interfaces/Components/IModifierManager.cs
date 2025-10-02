namespace Core.Interfaces.Components
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces;

    public interface IModifierManager
    {
        IReadOnlyDictionary<Parameter, List<IItemModifier>> BattleModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IItemModifier>> PermanentModifiers { get; }
        IReadOnlyDictionary<Parameter, List<IItemModifier>> TemporaryModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

        void AddBattleModifier(IItemModifier modifier);
        void AddPermanentModifier(IItemModifier modifier);
        void AddTemporaryModifier(IItemModifier modifier);
        void RemoveAllBattleModifiers();
        void RemoveAllTemporaryModifiers();
        void RemoveBattleModifier(IItemModifier modifier);
        void RemoveBattleModifierBySource(object source);
        void RemovePermanentModifier(IItemModifier modifier);
        void RemovePermanentModifierBySource(object source);
        void RemoveTemporaryModifier(IItemModifier modifier);
        void RemoveTemporaryModifierBySource(object source);
        void UpdateBattleModifier(IItemModifier modifier);
        void UpdatePermanentModifier(IItemModifier modifier);
        void UpdateTemporaryModifier(IItemModifier modifier);
    }
}
