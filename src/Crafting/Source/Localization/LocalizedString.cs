namespace Crafting.Source.Localization
{
    using Godot;

    [GlobalClass]
    public partial class LocalizedString : Resource
    {
        /// <summary>
        /// For crafting resource names we using: Name_ResourceType
        /// </summary>
        [Export]
        public string Key { get; set; } = string.Empty;

        public string Text => TranslationServer.Translate(Key);
    }
}
