namespace Playground.Components
{
    using System.Collections.Generic;
    using System;
    using Playground.Script.Enums;
    using Playground.Script.Abilities.Modifiers;
    using Godot;

    public class ModifierManager
    {
        // all modifiers from equipment, passive abilities etc.
        protected readonly Dictionary<Parameter, List<IModifier>> _permanentModifiers = [];
        // temporary modifiers from abilities, weapon effect etc.
        protected readonly Dictionary<Parameter, List<IModifier>> _temporaryModifiers = [];

        public event Action<Parameter>? ParameterModifiersChanged;

        public IReadOnlyDictionary<Parameter, List<IModifier>> PermanentModifiers => _permanentModifiers;
        public IReadOnlyDictionary<Parameter, List<IModifier>> TemporaryModifiers => _temporaryModifiers;


        public void AddPermanentModifier(IModifier modifier) => AddToCategory(_permanentModifiers, modifier);
        public void AddTemporaryModifier(IModifier modifier) => AddToCategory(_temporaryModifiers, modifier);


        public void RemovePermanentModifier(IModifier modifier) => RemoveFromCategory(_permanentModifiers, modifier);
        public void RemoveTemporaryModifier(IModifier modifier) => RemoveFromCategory(_temporaryModifiers, modifier);

        public void ResetTemporaryModifiers() => _temporaryModifiers.Clear();


        public List<IModifier> GetCombinedModifiers(Parameter parameter)
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
            GD.Print($"Modifier added: {modifier.GetType().Name}. Parameter: {modifier.Parameter}. Value: {modifier.Value}");
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
    }
}
