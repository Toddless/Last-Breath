namespace Core.Localization
{
    using System.Linq;
    using Modifiers;

    /// <summary>Registry entry for unmaterialized pool/blueprint lines (recipe previews): parameter and
    /// context descriptors render through their formatters (value spreads through the *_Range templates),
    /// a composite joins its parts into the single line the materialized group will present as.</summary>
    public class ModifierDescriptorTextFormatter(ModifierFormatter modifierFormatter, ContextModifierFormatter contextModifierFormatter) : ITextFormatter
    {
        public bool CanFormat(object value) => value is IModifierDescriptor;

        public string Format(object value, TextFormat format) => FormatDescriptor((IModifierDescriptor)value, format);

        private string FormatDescriptor(IModifierDescriptor descriptor, TextFormat format) => descriptor switch
        {
            ParameterDescriptor parameter => modifierFormatter.FormatDescriptor(parameter, format),
            ContextDescriptor context => contextModifierFormatter.FormatDescriptor(context, format),
            CompositeDescriptor composite => string.Join(", ", composite.Parts.Select(part => FormatDescriptor(part, format))),
            _ => string.Empty,
        };
    }
}
