namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Modifiers;
    using Utilities;

    public class ModifierManager : IModifierManager
    {
        // all modifiers from equipment, passive abilities etc.
        private readonly Dictionary<Parameter, List<IModifier>> _permanentModifiers = [];
        // temporary modifiers from abilities, weapon effect etc.
        private readonly Dictionary<Parameter, List<IModifier>> _temporaryModifiers = [];
        private readonly Dictionary<Parameter, List<IModifier>> _battleModifiers = [];

        public IReadOnlyDictionary<Parameter, List<IModifier>> PermanentModifiers => _permanentModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifier>> TemporaryModifiers => _temporaryModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifier>> BattleModifiers => _battleModifiers;

        public event EventHandler<IModifiersChangedEventArgs>? ParameterModifiersChanged;

        public void AddPermanentModifier(IModifier modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifier modifier) => AddToCategory(_temporaryModifiers, modifier);
        public void AddBattleModifier(IModifier modifier) => AddToCategory(_battleModifiers, modifier);

        public void UpdatePermanentModifier(IModifier modifier) => UpdateModifier(_permanentModifiers, modifier);
        public void UpdateTemporaryModifier(IModifier modifier) => UpdateModifier(_temporaryModifiers, modifier);
        public void UpdateBattleModifier(IModifier modifier) => UpdateModifier(_battleModifiers, modifier);

        public void RemovePermanentModifier(IModifier modifier) => RemoveFromCategory(_permanentModifiers, modifier);
        public void RemoveTemporaryModifier(IModifier modifier) => RemoveFromCategory(_temporaryModifiers, modifier);
        public void RemoveBattleModifier(IModifier modifier) => RemoveFromCategory(_battleModifiers, modifier);

        public void RemovePermanentModifierBySource(object source) => RemoveAllFromCategoryBySource(_permanentModifiers, source);
        public void RemoveTemporaryModifierBySource(object source) => RemoveAllFromCategoryBySource(_temporaryModifiers, source);
        public void RemoveBattleModifierBySource(object source) => RemoveAllFromCategoryBySource(_battleModifiers, source);

        public void RemoveAllTemporaryModifiers() => _temporaryModifiers.Clear();
        public void RemoveAllBattleModifiers() => _battleModifiers.Clear();


        private List<IModifier> GetCombinedModifiers(Parameter parameter)
        {
            var modifiers = new List<IModifier>();
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
        private void UpdateModifier(Dictionary<Parameter, List<IModifier>> category, IModifier newModifier)
        {
            if (!category.TryGetValue(newModifier.Parameter, out var list))
            {
                Logger.LogNotFound($"List for {newModifier.Parameter}", this);
                list = [];
                category[newModifier.Parameter] = list;
            }
            var existingModifier = list.FirstOrDefault(x => x.Source == newModifier.Source && x.Type == newModifier.Type);
            if (existingModifier == null)
            {
                Logger.LogNotFound($"Modifier with parameters: Source: {newModifier.Source}, Type: {newModifier.Type}", this);
                list.Add(newModifier);
            }
            else
            {
                // TODO: should i change all properties?
                existingModifier.Value = newModifier.Value;
            }
            RaiseEvent(newModifier.Parameter);
        }

        private void AddToCategory(Dictionary<Parameter, List<IModifier>> category, IModifier modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifier>? list))
            {
                Logger.LogNotFound($"List for: {modifier.Parameter}", this);
                list = [];
                category[modifier.Parameter] = list;
            }

            if (list.Contains(modifier))
            {
                Logger.LogError("Trying to add a modifier that already exists in the list", this);
            }

            list.Add(modifier);

            RaiseEvent(modifier.Parameter);
        }

        private void RemoveFromCategory(Dictionary<Parameter, List<IModifier>> category, IModifier modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifier>? list))
            {
                Logger.LogError("Trying to remove a modifier from non-existent list", this);
                return;
            }
            list.Remove(modifier);
            if (list.Count == 0)
            {
                category.Remove(modifier.Parameter);
            }
            RaiseEvent(modifier.Parameter);
        }

        private void RemoveAllFromCategoryBySource(Dictionary<Parameter, List<IModifier>> category, object source)
        {
            foreach (var list in category)
            {
                if (list.Value.RemoveAll(x => x.Source == source) > 0) RaiseEvent(list.Key);
            }
        }

        private IModifier? FindFirstModifierBySource(Dictionary<Parameter, List<IModifier>> collection, object source) => collection.Values.SelectMany(list => list).FirstOrDefault(modifier => modifier.Source == source);
        private List<IModifier> FindAllModifiersBySource(Dictionary<Parameter, List<IModifier>> collection, object source) => [.. collection.Values.SelectMany(list => list).Where(modifier => modifier.Source == source)];

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
