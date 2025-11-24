namespace Battle
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;
    using Core.Interfaces.Components;

    public class ModifierManager : IModifierManager
    {
        // all modifiers from equipment, passive abilities etc.
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _permanentModifiers = [];

        // temporary modifiers from abilities, weapon effect etc.
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _temporaryModifiers = [];
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _battleModifiers = [];

        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> PermanentModifiers => _permanentModifiers;
        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> TemporaryModifiers => _temporaryModifiers;
        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> BattleModifiers => _battleModifiers;

        public event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

        public void AddPermanentModifier(IModifierInstance modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifierInstance modifier) => AddToCategory(_temporaryModifiers, modifier);
        public IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter) => GetCombinedModifiers(parameter);

        public void AddBattleModifier(IModifierInstance modifier) => AddToCategory(_battleModifiers, modifier);

        public void UpdatePermanentModifier(IModifierInstance modifier) => UpdateModifier(_permanentModifiers, modifier);

        public void UpdatePermanentModifiers(IEnumerable<IModifierInstance> modifiers) => UpdateModifiers(_permanentModifiers, modifiers);


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


        private List<IModifierInstance> GetCombinedModifiers(EntityParameter parameter)
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

        private void RaiseEvent(EntityParameter parameter) => ModifiersChanged?.Invoke(this, new ModifiersChangedEventArgs(parameter, GetCombinedModifiers(parameter)));

        private void HandleParameters(object source)
        {
            foreach (var param in GetAffectedParameters(source))
                RaiseEvent(param);
        }

        // adding multiple modifiers via foreach generate to many unnecessary calls
        private void UpdateModifier(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance newModifier)
        {
            if (!category.TryGetValue(newModifier.EntityParameter, out var list))
            {
                // Tracker.TrackNotFound($"List for {newModifier.EntityParameter}", this);
                list = [];
                category[newModifier.EntityParameter] = list;
            }

            var existingModifier = list.FirstOrDefault(x => x.Source == newModifier.Source && x.ModifierType == newModifier.ModifierType);
            if (existingModifier == null)
            {
                //  Tracker.TrackNotFound($"Modifier with parameters: Source: {newModifier.Source}, Type: {newModifier.ModifierType}", this);
                list.Add(newModifier);
            }
            else existingModifier.Value = newModifier.Value;

            RaiseEvent(newModifier.EntityParameter);
        }

        private void UpdateModifiers(Dictionary<EntityParameter, List<IModifierInstance>> category, IEnumerable<IModifierInstance> modifiers)
        {
            IEnumerable<IModifierInstance> modifierInstances = modifiers.ToList();
            foreach (var modifier in modifierInstances)
            {
                if (category.TryGetValue(modifier.EntityParameter, out var existing))
                {
                    var existingModifier = existing.FirstOrDefault(x => ReferenceEquals(x.Source, modifier.Source) && x.ModifierType == modifier.ModifierType);
                    if (existingModifier == null) existing.Add(modifier);
                    else existingModifier.Value = modifier.Value;
                }
                else
                {
                    existing = [];
                    _permanentModifiers[modifier.EntityParameter] = existing;
                    existing.Add(modifier);
                }
            }

            modifierInstances.GroupBy(x => x.EntityParameter).ToList().ForEach(x => RaiseEvent(x.Key));
        }

        private void AddToCategory(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list))
            {
                list = [];
                category[modifier.EntityParameter] = list;
            }

            if (list.Contains(modifier))
            {
                // Tracker.TrackError("Trying to add a modifier that already exists in the list", this);
            }

            list.Add(modifier);

            RaiseEvent(modifier.EntityParameter);
        }

        private void RemoveFromCategory(Dictionary<EntityParameter, List<IModifierInstance>> category, IModifierInstance modifier)
        {
            if (!category.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list))
            {
                //Tracker.TrackError("Trying to remove a modifier from non-existent list", this);
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

        private IModifier? FindFirstModifierBySource(Dictionary<EntityParameter, List<IModifierInstance>> collection, object source) =>
            collection.Values.SelectMany(list => list).FirstOrDefault(modifier => modifier.Source == source);

        private List<IModifierInstance> FindAllModifiersBySource(Dictionary<EntityParameter, List<IModifierInstance>> collection, object source) =>
            [.. collection.Values.SelectMany(list => list).Where(modifier => modifier.Source == source)];

        private IEnumerable<EntityParameter> GetAffectedParameters(object source)
        {
            return _permanentModifiers
                .Concat(_temporaryModifiers)
                .Where(x => x.Value.Any(m => m.Source == source))
                .Select(x => x.Key)
                .Distinct();
        }
    }
}
