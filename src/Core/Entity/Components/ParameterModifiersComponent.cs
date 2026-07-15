namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Events;
    using Modifiers;

    public class ParameterModifiersComponent : IParameterModifiersComponent
    {
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _modifiers = [];
        private readonly List<IParameterModifierSource> _sources = [];

        public IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> EntityModifiers => _modifiers;

        public event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

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

        public IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter) => GetCombinedModifiers(parameter);

        public void AddModifier(IModifierInstance modifier)
        {
            if (!_modifiers.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list))
            {
                list = [];
                _modifiers[modifier.EntityParameter] = list;
            }

            list.Add(modifier);
            RaiseEvent(modifier.EntityParameter);
        }

        public void UpdateModifier(IModifierInstance newModifier)
        {
            if (!_modifiers.TryGetValue(newModifier.EntityParameter, out var list))
            {
                list = [];
                _modifiers[newModifier.EntityParameter] = list;
            }

            var existingModifier = list.FirstOrDefault(x => x.Source == newModifier.Source && x.ModifierValueType == newModifier.ModifierValueType);
            if (existingModifier == null)
            {
                list.Add(newModifier);
            }
            else existingModifier.Value = newModifier.Value;

            RaiseEvent(newModifier.EntityParameter);
        }

        public void UpdateModifiers(IEnumerable<IModifierInstance> modifiers)
        {
            IEnumerable<IModifierInstance> modifierInstances = modifiers.ToList();
            foreach (var modifier in modifierInstances)
            {
                if (_modifiers.TryGetValue(modifier.EntityParameter, out var existing))
                {
                    var existingModifier = existing.FirstOrDefault(x => ReferenceEquals(x.Source, modifier.Source) && x.ModifierValueType == modifier.ModifierValueType);
                    if (existingModifier == null) existing.Add(modifier);
                    else existingModifier.Value = modifier.Value;
                }
                else
                {
                    existing = [];
                    _modifiers[modifier.EntityParameter] = existing;
                    existing.Add(modifier);
                }
            }

            modifierInstances.GroupBy(x => x.EntityParameter).ToList().ForEach(x => RaiseEvent(x.Key));
        }

        public void RemoveModifier(IModifierInstance modifier)
        {
            if (!_modifiers.TryGetValue(modifier.EntityParameter, out List<IModifierInstance>? list)) return;

            list.RemoveAll(x => x.InstanceId == modifier.InstanceId);
            if (list.Count == 0)
            {
                _modifiers.Remove(modifier.EntityParameter);
            }

            RaiseEvent(modifier.EntityParameter);
        }

        public void RefreshParameter(EntityParameter parameter) => RaiseEvent(parameter);

        public void RemoveModifierBySource(string source)
        {
            foreach (var list in _modifiers.Where(list => list.Value.RemoveAll(x => x.Source == source) > 0))
                RaiseEvent(list.Key);
        }

        private List<IModifierInstance> GetCombinedModifiers(EntityParameter parameter)
        {
            var modifiers = new List<IModifierInstance>();
            CollectModifiers(parameter, modifiers);
            // Aggregate parameters (e.g. AllResistance) contribute their modifiers to every family member.
            foreach (var aggregate in AggregateParameters.Aggregates(parameter))
                CollectModifiers(aggregate, modifiers);
            return modifiers;
        }

        private void CollectModifiers(EntityParameter parameter, List<IModifierInstance> into)
        {
            if (_modifiers.TryGetValue(parameter, out var permanent))
                into.AddRange(permanent);
            foreach (var source in _sources)
                into.AddRange(source.GetModifiers(parameter));
        }

        private void OnSourceChanged(IReadOnlyCollection<EntityParameter> parameters)
        {
            foreach (var parameter in parameters) RaiseEvent(parameter);
        }

        // A change on an aggregate parameter refreshes every family member (nothing reads the aggregate itself).
        private void RaiseEvent(EntityParameter parameter)
        {
            if (!AggregateParameters.IsAggregate(parameter))
            {
                RaiseSingle(parameter);
                return;
            }

            foreach (var member in AggregateParameters.Members(parameter))
                RaiseSingle(member);
        }

        private void RaiseSingle(EntityParameter parameter) =>
            ModifiersChanged?.Invoke(this, new ModifiersChangedEventArgs(parameter, GetCombinedModifiers(parameter)));
    }
}
