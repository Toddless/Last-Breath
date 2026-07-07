namespace Crafting.Internal.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Components;
    using Core.Enums;
    using Core.Modifiers;

    internal class ParameterModifiersComponent : IParameterModifiersComponent
    {
        // all modifiers from equipment, passive abilities etc.
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _permanentModifiers = [];
        // temporary modifiers from abilities, weapon effect etc.
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _temporaryModifiers = [];
        // mo
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _battleModifiers = [];
        private readonly List<IParameterModifierSource> _sources = [];

        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> EntityModifiers => _permanentModifiers;
        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> TemporaryModifiers => _temporaryModifiers;
        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> BattleModifiers => _battleModifiers;

        public event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

        public IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter) => [];

        public void RegisterSource(IParameterModifierSource source)
        {
            if (_sources.Contains(source)) return;
            _sources.Add(source);
            source.SourceChanged += OnSourceChanged;
            OnSourceChanged(source.AffectedParameters);
        }

        public void UnregisterSource(IParameterModifierSource source)
        {
            if (!_sources.Remove(source)) return;
            source.SourceChanged -= OnSourceChanged;
            OnSourceChanged(source.AffectedParameters);
        }
        public void AddModifier(IModifierInstance modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifierInstance modifier) => AddToCategory(_temporaryModifiers, modifier);

        public void AddBattleModifier(IModifierInstance modifier) => AddToCategory(_battleModifiers, modifier);

        public void UpdateModifier(IModifierInstance newModifier) => UpdateModifier(_permanentModifiers, newModifier);
        public void UpdateModifiers(IEnumerable<IModifierInstance> modifiers) => throw new NotImplementedException();

        public void UpdateTemporaryModifier(IModifierInstance modifier) => UpdateModifier(_temporaryModifiers, modifier);
        public void UpdateBattleModifier(IModifierInstance modifier) => UpdateModifier(_battleModifiers, modifier);

        public void RemoveModifier(IModifierInstance modifier) => RemoveFromCategory(_permanentModifiers, modifier);
        public void RemoveTemporaryModifier(IModifierInstance modifier) => RemoveFromCategory(_temporaryModifiers, modifier);
        public void RemoveBattleModifier(IModifierInstance modifier) => RemoveFromCategory(_battleModifiers, modifier);

        public void RemoveModifierBySource(string source) => RemoveAllFromCategoryBySource(_permanentModifiers, source);
        public void RemoveTemporaryModifierBySource(object source) => RemoveAllFromCategoryBySource(_temporaryModifiers, source);
        public void RemoveBattleModifierBySource(object source) => RemoveAllFromCategoryBySource(_battleModifiers, source);

        public void RemoveAllTemporaryModifiers() => _temporaryModifiers.Clear();
        public void RemoveAllBattleModifiers() => _battleModifiers.Clear();


        private List<IModifierInstance> GetCombinedModifiers(EntityParameter entityParameter)
        {
            var modifiers = new List<IModifierInstance>();
            if (_permanentModifiers.TryGetValue(entityParameter, out var permanent))
                modifiers.AddRange(permanent);
            if (_temporaryModifiers.TryGetValue(entityParameter, out var temp))
                modifiers.AddRange(temp);
            if (_battleModifiers.TryGetValue(entityParameter, out var battle))
                modifiers.AddRange(battle);
            foreach (var source in _sources)
                modifiers.AddRange(source.GetModifiers(entityParameter));

            return modifiers;
        }

        private void OnSourceChanged(IReadOnlyCollection<EntityParameter> parameters)
        {
            foreach (var parameter in parameters) RaiseEvent(parameter);
        }

        private void RaiseEvent(EntityParameter entityParameter) => ModifiersChanged?.Invoke(this, new ModifiersChangedEventArgs(GetCombinedModifiers(entityParameter), entityParameter));


        // adding multiple modifiers via foreach generate to many unnecessary calls
        private void UpdateModifier(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance newModifier)
        {
            if (!category.TryGetValue(newModifier.EntityParameter, out var list))
            {
                Tracker.TrackNotFound($"List for {newModifier.EntityParameter}", this);
                list = [];
                category[newModifier.EntityParameter] = list;
            }
            var existingModifier = list.FirstOrDefault(x => x.Source == newModifier.Source && x.ModifierValueType == newModifier.ModifierValueType);
            if (existingModifier == null)
            {
                Tracker.TrackNotFound($"Modifier with parameters: Source: {newModifier.Source}, Type: {newModifier.ModifierValueType}", this);
                list.Add(newModifier);
            }
            else
            {
                // TODO: should i change all properties?
                existingModifier.Value = newModifier.Value;
            }
            RaiseEvent(newModifier.EntityParameter);
        }

        private void AddToCategory(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list))
            {
                Tracker.TrackNotFound($"List for: {modifier.EntityParameter}", this);
                list = [];
                category[modifier.EntityParameter] = list;
            }

            if (list.Contains(modifier))
            {
                Tracker.TrackError("Trying to add a modifier that already exists in the list", this);
            }

            list.Add(modifier);

            RaiseEvent(modifier.EntityParameter);
        }

        private void RemoveFromCategory(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list))
            {
                Tracker.TrackError("Trying to remove a modifier from non-existent list", this);
                return;
            }
            list.Remove(modifier);
            if (list.Count == 0)
            {
                category.Remove(modifier.EntityParameter);
            }
            RaiseEvent(modifier.EntityParameter);
        }

        private void RemoveAllFromCategoryBySource(Dictionary<EntityParameter, List<IModifierInstance>> category, object source)
        {
            foreach (var list in category)
            {
                if (list.Value.RemoveAll(x => x.Source == source) > 0) RaiseEvent(list.Key);
            }
        }
    }
}
