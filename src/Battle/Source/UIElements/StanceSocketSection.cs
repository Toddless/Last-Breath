namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Enums;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// One stance's share of the mastery screen: a heading and the socket panel filtered to that
    /// stance. It does nothing else on purpose — the stance lives here so the panel below can stay
    /// stance-blind and be reused by the passive wheel, which shows one ability and no stance at all.
    /// </summary>
    [GlobalClass]
    public partial class StanceSocketSection : VBoxContainer, IRequireServices
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://bh2sn5qe8dc1r";

        [Export] private Label? _header;
        [Export] private AbilitySocketPanel? _panel;
        [Export] private Stance _stance;

        public Stance Stance => _stance;

        public void InjectServices(IGameServiceProvider provider)
        {
            _header?.Text = Localization.Localize(_stance.ToString());
            _panel?.InjectServices(provider);
            _panel?.ShowStance(_stance);
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);
    }
}
