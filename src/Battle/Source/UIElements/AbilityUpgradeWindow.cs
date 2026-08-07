namespace Battle.Source.UIElements
{
    using Core;
    using Core.Data;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Shared ability detail window. Renders a pure view DTO (no domain object) and nothing else: the
    /// ability's numbers, and the augments its sockets hold. Read only — an ability is upgraded by what
    /// is seated in it, and seating is the socket window's business.
    /// The tree opens it via OpenWindow and calls <see cref="Show"/>; the first Show may arrive
    /// before the node is in the tree (deferred add), so rendering also runs from <see cref="_Ready"/>.
    /// </summary>
    public partial class AbilityUpgradeWindow : Control, IWindow
    {
        private const string UID = "uid://qkc76uh24l18";

        [Export] private AbilityUpgrades? _abilityUpgrades;
        [Export] private Label? _abilityName, _cost, _cooldown;
        [Export] private RichTextLabel? _abilityDescription;
        [Export] private Button? _close;

        private AbilityUpgradeView? _view;


        public override void _Ready()
        {
            _close?.Pressed += QueueFree;
            KeywordLinks.Attach(_abilityDescription);
            if (_view != null) Render();
        }

        /// <summary>Nothing to inject: the window sends no request of its own — the tree that opens it
        /// asks for the view and hands it over.</summary>
        public void InjectServices(IGameServiceProvider provider)
        {
        }

        public void Show(AbilityUpgradeView view)
        {
            _view = view;
            if (IsInsideTree()) Render();
            // Otherwise _Ready renders it once the node enters the tree.
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => _view = null;

        private void Render()
        {
            if (_view == null) return;
            _abilityName?.Text = _view.Name;
            _cost?.Text = _view.Cost;
            _cooldown?.Text = _view.Cooldown;
            _abilityDescription?.Text = _view.Description;
            _abilityUpgrades?.SetWorn(_view.Worn);
        }
    }
}
