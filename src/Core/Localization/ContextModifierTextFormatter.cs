namespace Core.Localization
{
    using Modifiers;

    public class ContextModifierTextFormatter(ContextModifierFormatter formatter) : ITextFormatter
    {
        public bool CanFormat(object value) => value is ContextModifierEntry;

        public string Format(object value, TextFormat format) => formatter.Format((ContextModifierEntry)value, format);
    }
}
