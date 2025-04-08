namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Abilities.Modifiers;

    public class ModifierManager : ObservableProperty
    {
        // all modifiers from equipment, passive abilities etc.
        protected readonly Dictionary<Parameter, List<IModifier>> _permanentModifiers = [];
        // temporary modifiers like buffs and debuffs
        protected readonly Dictionary<Parameter, List<IModifier>> _temporaryModifiers = [];

        public event Action<Parameter>? ParameterModifiersChanged;

        public IReadOnlyDictionary<Parameter, List<IModifier>> PermanentModifiers => _permanentModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifier>> TemporaryModifiers => _temporaryModifiers;
        // adding modifiers
        public void AddPermanentModifier(IModifier modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifier modifier) => AddToCategory(_temporaryModifiers, modifier);
        // removing modifiers
        public void RemovePermanentModifier(IModifier modifier) => RemoveFromCategory(_permanentModifiers, modifier);
        public void RemoveTemporaryModifier(IModifier modifier) => RemoveFromCategory(_temporaryModifiers, modifier);
        // clear temporary modifiers (for example we remove all modifiers after fight ends)
        public void Reset() => _temporaryModifiers.Clear();

        public float CalculateFloatValue(float value, Parameter parameter) => Math.Max(0, CalculateModifiers(GetCombinedModifiers(parameter), value));

        private List<IModifier> GetCombinedModifiers(Parameter parameter)
        {
            var modifiers = new List<IModifier>();
            if (_permanentModifiers.TryGetValue(parameter, out var permanent))
                modifiers.AddRange(permanent);

            if (_temporaryModifiers.TryGetValue(parameter, out var temp))
                modifiers.AddRange(temp);

            return modifiers;
        }

        private void AddToCategory(Dictionary<Parameter, List<IModifier>> category, IModifier modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifier>? list))
            {
                list = [];
                category[modifier.Parameter] = list;
            }
            list.Add(modifier);

            ParameterModifiersChanged?.Invoke(modifier.Parameter);
        }

        private void RemoveFromCategory(Dictionary<Parameter, List<IModifier>> category, IModifier modifier)
        {
            if (!category.TryGetValue(modifier.Parameter, out List<IModifier>? list))
            {
                // log
                return;
            }
            list.Remove(modifier);
            if (list.Count == 0)
            {
                category.Remove(modifier.Parameter);
            }
            ParameterModifiersChanged?.Invoke(modifier.Parameter);
        }

        private float CalculateModifiers(IEnumerable<IModifier> modifiers, float value)
        {
            var factor = 1f;
            foreach (var group in modifiers.GroupBy(m => m.Type).OrderBy(g => g.Key))
            {
                switch (group.Key)
                {
                    case ModifierType.Additive:
                        value = ApplyModifiers(value, group);
                        break;
                    case ModifierType.MultiplicativeSum:
                        value *= factor += group.Sum(x => x.Value);
                        break;
                    case ModifierType.Multiplicative:
                        value = ApplyModifiers(value, group);
                        break;

                }
            }
            return value;
        }

        private float ApplyModifiers(float value, IGrouping<ModifierType, IModifier> modifiers)
        {
            foreach (var modifier in modifiers.OrderBy(x => x.Priority))
            {
                value = modifier.ModifyValue(value);
            }
            return value;
        }
    }
}
