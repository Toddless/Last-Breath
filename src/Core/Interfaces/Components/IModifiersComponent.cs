namespace Core.Interfaces.Components
{
    using System;
    using Enums;
    using System.Collections.Generic;
    using Modifiers;

    public interface IModifiersComponent
    {
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> BattleModifiers { get; }
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> PermanentModifiers { get; }
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> TemporaryModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

        IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter);

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
        void UpdatePermanentModifiers(IEnumerable<IModifierInstance> modifiers);
        void UpdateTemporaryModifier(IModifierInstance modifier);
    }
}
