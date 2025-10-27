namespace LastBreath.Components
{
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;
    using Core.Interfaces.Components;

    public class ModifierManager : IModifierManager
    {
        // all modifiers from equipment, passive abilities etc.
        private readonly Dictionary<Parameter, List<IModifierInstance>> _permanentModifiers = [];
        // temporary modifiers from abilities, weapon effect etc.
        private readonly Dictionary<Parameter, List<IModifierInstance>> _temporaryModifiers = [];
        private readonly Dictionary<Parameter, List<IModifierInstance>> _battleModifiers = [];

        public IReadOnlyDictionary<Parameter, List<IModifierInstance>> PermanentModifiers => _permanentModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifierInstance>> TemporaryModifiers => _temporaryModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifierInstance>> BattleModifiers => _battleModifiers;

        public event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

        public void AddPermanentModifier(IModifierInstance modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifierInstance modifier) => AddToCategory(_temporaryModifiers, modifier);
        public void AddBattleModifier(IModifierInstance modifier) => AddToCategory(_battleModifiers, modifier);

        public void UpdatePermanentModifier(IModifierInstance modifier) => UpdateModifier(_permanentModifiers, modifier);
        public void UpdateTemporaryModifier(IModifierInstance modifier) => UpdateModifier(_temporaryModifiers, modifier);
        public void UpdateBattleModifier(IModifierInstance modifier) => UpdateModifier(_battleModifiers, modifier);

        public void RemovePermanentModifier(IModifierInstance modifier) => RemoveFromCategory(_permanentModifiers, modifier);
        public void RemoveTemporaryModifier(IModifierInstance modifier) => RemoveFromCategory(_temporaryModifiers, modifier);
        public void RemoveBattleModifier(IModifierInstance modifier) => RemoveFromCategory(_battleModifiers, modifier);

        public void RemovePermanentModifierBySource(object source) => RemoveAllFromCategoryBySource(_permanentModifiers, source);
        public void RemoveTemporaryModifierBySource(object source) => RemoveAllFromCategoryBySource(_temporaryModifiers, source);
        public void RemoveBattleModifierBySource(object source) => RemoveAllFromCategoryBySource(_battleModifiers, source);

        public void RemoveAllTemporaryModifiers() => _temporaryModifiers.Clear();
        public void RemoveAllBattleModifiers() => _battleModifiers.Clear();


        private List<IModifierInstance> GetCombinedModifiers(Parameter parameter)
        {
            var modifiers = new List<IModifierInstance>();
            if (_permanentModifiers.TryGetValue(parameter, out var permanent))
                modifiers.AddRange(permanent);
            if (_temporaryModifiers.TryGetValue(parameter, out var temp))
                modifiers.AddRange(temp);
            if (_battleModifiers.TryGetValue(parameter, out var battle))
                modifiers.AddRange(battle);

            return modifiers;
        }

        private void RaiseEvent(Parameter parameter) => ParameterModifiersChanged?.Invoke(this, new ModifiersChangedEventArgs(parameter, GetCombinedModifiers(parameter)));

        private void HandleParameters(object source)
        {
            foreach (var param in GetAffectedParameters(source))
                RaiseEvent(param);
        }

        // adding multiple modifiers via foreach generate to many unnecessary calls
        private void UpdateModifier(Dictionary<Parameter, List<IModifierInstance>> category, IModifierInstance newModifier)
        {
            if (!category.TryGetValue(newModifier.Parameter, out var list))
            {
                Tracker.TrackNotFound($"List for {newModifier.Parameter}", this);
                list = [];
                category[newModifier.Parameter] = list;
            }
            var existingModifier = list.FirstOrDefault(x => x.Source == newModifier.Source && x.ModifierType == newModifier.ModifierType);
            if (existingModifier == null)
            {
                Tracker.TrackNotFound($"Modifier with parameters: Source: {newModifier.Source}, Type: {newModifier.ModifierType}", this);
                list.Add(newModifier);
            }
            else
            {
                // TODO: should i change all properties?
                existingModifier.Value = newModifier.Value;
            }
            RaiseEvent(newModifier.Parameter);
        }

        private void AddToCategory(Dictionary<Parameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifierInstance>? list))
            {
                Tracker.TrackNotFound($"List for: {modifier.Parameter}", this);
                list = [];
                category[modifier.Parameter] = list;
            }

            if (list.Contains(modifier))
            {
                Tracker.TrackError("Trying to add a modifier that already exists in the list", this);
            }

            list.Add(modifier);

            RaiseEvent(modifier.Parameter);
        }

        private void RemoveFromCategory(Dictionary<Parameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifierInstance>? list))
            {
                Tracker.TrackError("Trying to remove a modifier from non-existent list", this);
                return;
            }
            list.Remove(modifier);
            if (list.Count == 0)
            {
                category.Remove(modifier.Parameter);
            }
            RaiseEvent(modifier.Parameter);
        }

        private void RemoveAllFromCategoryBySource(Dictionary<Parameter, List<IModifierInstance>> category, object source)
        {
            foreach (var list in category)
            {
                if (list.Value.RemoveAll(x => x.Source == source) > 0) RaiseEvent(list.Key);
            }
        }

        private IModifier? FindFirstModifierBySource(Dictionary<Parameter, List<IModifierInstance>> collection, object source) => collection.Values.SelectMany(list => list).FirstOrDefault(modifier => modifier.Source == source);
        private List<IModifierInstance> FindAllModifiersBySource(Dictionary<Parameter, List<IModifierInstance>> collection, object source) => [.. collection.Values.SelectMany(list => list).Where(modifier => modifier.Source == source)];

        private IEnumerable<Parameter> GetAffectedParameters(object source)
        {
            return _permanentModifiers
                .Concat(_temporaryModifiers)
                .Where(x => x.Value.Any(m => m.Source == source))
                .Select(x => x.Key)
                .Distinct();
        }
    }
}
