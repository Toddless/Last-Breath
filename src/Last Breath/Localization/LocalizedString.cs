namespace LastBreath.Localization
{
    using Godot;

    [GlobalClass]
    public partial class LocalizedString : Resource
    {
        [Export]
        public string Key { get; set; } = string.Empty;

        public string Text => TranslationServer.Translate(Key);
    }
}
