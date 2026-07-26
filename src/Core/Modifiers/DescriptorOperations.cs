namespace Core.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>Pure transforms over modifier descriptors used by the crafting roll.</summary>
    public static class DescriptorOperations
    {
        /// <summary>Scales a descriptor's value bounds by a quality multiplier — LINEAR for every value type:
        /// flat/increase/multiplicative all store the bonus delta (multi data is 0.15, never 1.15), so one
        /// scale rule fits all. Returns a fresh descriptor; composites scale each part.</summary>
        public static IModifierDescriptor Scale(IModifierDescriptor descriptor, float multiplier) => descriptor switch
        {
            ParameterDescriptor parameter => parameter with { Value = parameter.Value.Scale(multiplier) },
            ContextDescriptor context => context with { Value = context.Value.Scale(multiplier) },
            CompositeDescriptor composite => composite with { Parts = composite.Parts.Select(part => Scale(part, multiplier)).ToList() },
            _ => descriptor,
        };

        /// <summary>Expands composites into their atomic parts (statistics/reports view of a pool).
        /// The root's affix survives on every atom (parts carry none by parse contract).</summary>
        public static IEnumerable<IModifierDescriptor> Flatten(IEnumerable<IModifierDescriptor> descriptors) =>
            descriptors.SelectMany(descriptor => descriptor is CompositeDescriptor composite
                ? Flatten(composite.Parts).Select(part => WithAffix(part, composite.Affix))
                : [descriptor]);

        private static IModifierDescriptor WithAffix(IModifierDescriptor descriptor, AffixKind affix) => affix == AffixKind.None
            ? descriptor
            : descriptor switch
            {
                ParameterDescriptor parameter => parameter with { Affix = affix },
                ContextDescriptor context => context with { Affix = affix },
                _ => descriptor,
            };
    }
}
