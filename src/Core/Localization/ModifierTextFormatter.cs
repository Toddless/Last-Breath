namespace Core.Localization
{
    using Modifiers;

    public class ModifierTextFormatter(ModifierFormatter formatter) : ITextFormatter
    {
        public bool CanFormat(object value) => value is IModifier;

        public string Format(object value, TextFormat format) => formatter.Format((IModifier)value, format);
    }
}
