namespace Utilities
{
    using Godot;
    using Core.Interfaces;

    public static class Lokalizator
    {
        private static ModifierFormatter s_modifierFormatter;

        static Lokalizator()
        {
            s_modifierFormatter = new ModifierFormatter(Lokalize);
        }

        public static string Format<T>(T obj)
        {
            switch (true)
            {
                case var _ when obj is IModifier modifier:
                    return s_modifierFormatter.FormatModifier(modifier);
                default:
                    break;
            }

            return string.Empty;
        }

        public static string FormatItemStats(string prop, float value) => s_modifierFormatter.FormatItemStats(prop, value); 
        public static string Lokalize(string id) => TranslationServer.Translate(id);
        public static string LokalizeDescription(string id) => TranslationServer.Translate(id + "_Description");
    }
}
