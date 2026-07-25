namespace Battle.Source.UIElements
{
    using System;
    using Core;
    using Core.Data;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Shared ability detail window. Renders a pure view DTO (no domain object) and turns an upgrade
    /// pick into an <see cref="ApplyAbilityUpgradeRequest"/>, re-rendering from the returned view.
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

        private IGameMessageBus? _messageBus;
        private AbilityUpgradeView? _view;


        public override void _Ready()
        {
            _abilityUpgrades?.AbilityUpgradeSelected += OnUpgradeSelectedAsync;
            _close?.Pressed += QueueFree;
            KeywordLinks.Attach(_abilityDescription);
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
            _cost?.Text = _view.Cost;
            _cooldown?.Text = _view.Cooldown;
            _abilityDescription?.Text = _view.Description;
            _abilityUpgrades?.SetOptions(_view.Options);
        }

        private async void OnUpgradeSelectedAsync(string upgradeInstanceId, int tier)
        {
            try
            {
                if (_messageBus == null || _view == null) return;
                _view = await _messageBus.SendRequest<ApplyAbilityUpgradeRequest, AbilityUpgradeView>(
                    new ApplyAbilityUpgradeRequest(_view.AbilityId, upgradeInstanceId, tier));
                Render();
            }
            catch (Exception e)
            {
                Tracker.TrackError($"Failed to upgrade ability: {e.Message}", this);
                GD.Print($"Failed to upgrade ability: {e.Message}, {e.StackTrace}");
            }
        }
    }
}
