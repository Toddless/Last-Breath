namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Interfaces.UI;
    using Godot;

    /// <summary>
    /// The ability learning screen: mastery level with progress, every known ability per stance
    /// (learned / learnable / locked behind a mastery level) and the upgrade selection
    /// (one of three per tier, changeable freely) for learned abilities.
    /// The content is built in code; the scene only hosts the root node — restyle freely later.
    /// </summary>
    [GlobalClass]
    public partial class MartialArtMasteryWindow : Control, IWindow
    {
        private const string UID = "uid://ds0wq0f8ha2x5";

        [Export] private StanceTree? _dexTree, _strTree, _intTree;
        [Export] private Label? _levelLabel;
        [Export] private Label? _experienceLabel;
        [Export] private TextureProgressBar? _experienceBar;

        private string? _selectedAbilityId;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public void InjectServices(IGameServiceProvider provider)
        {
            _dexTree?.InjectServices(provider);
            _strTree?.InjectServices(provider);
            _intTree?.InjectServices(provider);
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
