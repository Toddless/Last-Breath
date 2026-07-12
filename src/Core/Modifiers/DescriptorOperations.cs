namespace Core.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>Pure transforms over modifier descriptors used by the crafting roll.</summary>
    public static class DescriptorOperations
    {
        /// <summary>Scales a descriptor's value(s) by a quality multiplier, delta-aware for multiplicative lines
        /// (1.25 at x2 quality -> 1.5, not 2.5). Returns a fresh descriptor; composites scale each part.</summary>
        public static IModifierDescriptor Scale(IModifierDescriptor descriptor, float multiplier) => descriptor switch
        {
            ParameterDescriptor parameter => parameter with { Value = ScaledValue(parameter.ValueType, parameter.Value, multiplier) },
            ContextDescriptor context => context with { Value = ScaledValue(context.ValueType, context.Value, multiplier) },
            CompositeDescriptor composite => composite with { Parts = composite.Parts.Select(part => Scale(part, multiplier)).ToList() },
            _ => descriptor,
        };

        /// <summary>Expands composites into their atomic parts — the reroll pool is flat so a reroll is 1-for-1.</summary>
        public static IEnumerable<IModifierDescriptor> Flatten(IEnumerable<IModifierDescriptor> descriptors) =>
            descriptors.SelectMany(descriptor => descriptor is CompositeDescriptor composite ? Flatten(composite.Parts) : [descriptor]);

        /// <summary>Bridges a designed pool modifier (loot/additive pools authored as IModifier) into a descriptor.</summary>
        public static IModifierDescriptor FromModifier(IModifier modifier) => modifier switch
        {
            CompositeModifier composite => new CompositeDescriptor(composite.Parts.Select(FromModifier).ToList()) { Weight = composite.Weight },
            _ => new ParameterDescriptor(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue, modifier.Scope) { Weight = modifier.Weight },
        };

        private static float ScaledValue(ModifierValueType type, float value, float multiplier) =>
            type == ModifierValueType.Multiplicative ? 1f + (value - 1f) * multiplier : value * multiplier;
    }
}
