namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Data;
    using Core.Inventory;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The augments the player is carrying, laid out beside the sockets so he can drag one straight in.
    /// A view of the bag and never a second place augments live in: it takes nothing, moves nothing and
    /// redraws itself whenever the bag's contents change.
    /// The real inventory grid cannot be borrowed — its slots belong to the bag service for the whole
    /// session and lending them would take them away from an open inventory window — so this is a
    /// projection, and dragging out of an open inventory window works just as well.
    /// </summary>
    [GlobalClass]
    public partial class AugmentTray : Control, IRequireServices
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://cm0ug8yh3ls6n";

        [Export] private GridContainer? _grid;
        [Export] private Label? _trayEmpty;
        [Export] private PackedScene? _tileScene;

        private IGameMessageBus? _bus;
        private IInventory? _inventory;
        private IUiElementsManager? _windows;

        /// <summary>The services may arrive before the node is in the tree, and a fill that landed then
        /// would have nothing to draw into.</summary>
        public override void _Ready()
        {
            if (_bus != null) Refresh();
        }

        public override void _ExitTree()
        {
            if (_inventory != null) _inventory.ItemAmountChanges -= OnBagChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _bus = provider.GetService<IGameMessageBus>();
            _windows = provider.GetService<IUiElementsManager>();
            _inventory = provider.Optional<IInventory>();
            if (_inventory != null) _inventory.ItemAmountChanges += OnBagChanged;
            Refresh();
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private void OnBagChanged(string itemId, int amount) => Refresh();

        private async void Refresh()
        {
            try
            {
                if (_bus == null || _grid == null || _tileScene == null) return;

                var tiles = await _bus.SendRequest<GetCarriedAugmentsRequest, IReadOnlyList<AugmentTrayTileView>>(
                    new GetCarriedAugmentsRequest());

                if (!IsInsideTree()) return;
                Rebuild(tiles);
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to fill the augment tray", exception, this);
                GD.Print($"Failed to fill the augment tray: {exception.Message}");
            }
        }

        private void Rebuild(IReadOnlyList<AugmentTrayTileView> tiles)
        {
            if (_grid == null || _tileScene == null) return;

            _grid.QueueFreeChildren();
            foreach (AugmentTrayTileView tile in tiles)
            {
                var node = _tileScene.Instantiate<AugmentTrayTile>();
                _grid.AddChild(node);
                node.SetView(tile, _windows);
            }

            if (_trayEmpty != null) _trayEmpty.Visible = tiles.Count == 0;
        }
    }
}
