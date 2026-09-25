namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;

    /// <summary>Several parameter lines rolled and granted as ONE modifier ("armor AND evade +25%").
    /// Lives only in pools and grants: items expand it into its parts on add, so the calculation,
    /// upgrade scaling, save and UI layers only ever see plain modifiers.</summary>
    public class CompositeModifier : IModifierInstance
    {
        private readonly List<IModifierInstance> _parts;

        public IReadOnlyList<IModifierInstance> Parts => _parts;

        public float Weight { get; set; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Source { get; }

        // IModifier facade delegates to the first part for display/sorting only.
        // Composites never reach a calculation directly — item add-paths expand them first.
        public EntityParameter EntityParameter => _parts[0].EntityParameter;
        public ModifierValueType ModifierValueType => _parts[0].ModifierValueType;
        public float BaseValue => _parts[0].BaseValue;

        public float Value
        {
            get => _parts[0].Value;
            set { } // scaling applies to the expanded parts, never to the composite itself
        }

        public ModifierScope Scope
        {
            get => _parts[0].Scope;
            set
            {
                foreach (var part in _parts) part.Scope = value;
            }
        }

        public CompositeModifier(float weight, IEnumerable<IModifierInstance> parts, string source)
        {
            Weight = weight;
            Source = source;
            _parts = [.. parts];
            if (_parts.Count == 0) throw new ArgumentException("Composite modifier requires at least one part");
        }

        public IModifierInstance Copy() => new CompositeModifier(Weight, _parts.Select(part => part.Copy()), Source);

        public void ApplyTo(IFightable target)
        {
            foreach (var part in _parts) part.ApplyTo(target);
        }

        public void RemoveFrom(IFightable target)
        {
            foreach (var part in _parts) part.RemoveFrom(target);
        }
    }
}
