namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Shared ability detail window. Renders a pure view DTO (no domain object) and turns an upgrade
    /// pick into an <see cref="ApplyAbilityUpgradeRequest"/>, re-rendering from the returned view.
    /// The tree opens it via GetOrOpenWindow and calls <see cref="Show"/>; the first Show may arrive
    /// before the node is in the tree (deferred add), so rendering also runs from <see cref="_Ready"/>.
    /// </summary>
    public partial class AbilityUpgradeWindow : Control, IWindow
    {
        private const string UID = "uid://qkc76uh24l18";

        [Export] private AbilityUpgrades? _abilityUpgrades;
        [Export] private Label? _abilityName, _cost, _cooldown;
        [Export] private TextureRect? _abilityIcon;
        [Export] private RichTextLabel? _abilityDescription;
        [Export] private Button? _close;

        private IGameMessageBus? _messageBus;
        private AbilityUpgradeView? _view;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _abilityUpgrades?.AbilityUpgradeSelected += OnUpgradeSelectedAsync;
            _close?.Pressed += QueueFree;
            if (_view != null) Render();
        }

        public void InjectServices(IGameServiceProvider provider) => _messageBus = provider.GetService<IGameMessageBus>();

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
            _cost?.Text = $"Cost: {_view.Cost}";
            _cooldown?.Text = $"Cooldown: {_view.Cooldown}";
            _abilityDescription?.Text = _view.Description;
            _abilityUpgrades?.SetOptions(_view.Options);
        }

        private async void OnUpgradeSelectedAsync(string upgradeInstanceId, int tier)
        {
            if (_messageBus == null || _view == null) return;
            _view = await _messageBus.SendRequest<ApplyAbilityUpgradeRequest, AbilityUpgradeView>(
                new ApplyAbilityUpgradeRequest(_view.AbilityId, upgradeInstanceId, tier));
            Render();
        }
    }
}
