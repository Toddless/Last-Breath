namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Overlay picker for a crafting slot (category resource / additive): a pinned popup listing
    /// the owned candidates, placed at the cursor next to the clicked card — the crafting window's
    /// layout never moves. A single click picks and closes; the cancel button, Esc or a click
    /// outside just closes. The panel stays hidden until the deferred placement lands, so it never
    /// flashes at the scene origin.
    /// </summary>
    public partial class ResourcePickerPopup : Control, IPopup
    {
        // Path, not uid: the scene is authored outside the editor, so its uid is minted on the
        // first import — a hardcoded one here would dangle.
        private const string ScenePath = "res://Addons/Crafting/UIElements/Scenes/ResourcePickerPopup.tscn";
        private const float CursorOffset = 16f;

        [Export] private PanelContainer? _panel;
        [Export] private Label? _title;
        [Export] private ItemList? _list;
        [Export] private Button? _cancel;

        private readonly List<string> _ids = [];
        private Action<string>? _onPicked;

        public readonly record struct PickerEntry(string Id, string Label, Texture2D? Icon);

        public PopupLifetime Lifetime => PopupLifetime.Pinned;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public override void _Ready()
        {
            _cancel?.Pressed += Close;
            _list?.ItemSelected += OnItemSelected;
            // ShowPopup hands the instance out before the deferred AddChild lands it in the tree,
            // so the actual placement waits for _Ready (+ one frame for the panel's layout pass).
            Callable.From(Place).CallDeferred();
        }

        /// <summary>Pinned popups close on a click outside the panel.</summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true }) return;
            if (_panel != null && _panel.GetGlobalRect().HasPoint(_panel.GetGlobalMousePosition())) return;
            Close();
        }

        public void Present(string title, IReadOnlyList<PickerEntry> entries, Action<string> onPicked)
        {
            _onPicked = onPicked;
            _title?.Text = title;
            _ids.Clear();
            _list?.Clear();
            foreach (var entry in entries)
            {
                _ids.Add(entry.Id);
                _list?.AddItem(entry.Label, entry.Icon);
            }
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        private void OnItemSelected(long index)
        {
            if (index < 0 || index >= _ids.Count) return;
            var callback = _onPicked;
            string picked = _ids[(int)index];
            Close();
            callback?.Invoke(picked);
        }

        private void Place()
        {
            if (_panel == null) return;
            UiPlacement.PlaceClamped(_panel, GetGlobalMousePosition(), new Vector2(CursorOffset, CursorOffset));
            _panel.Visible = true;
        }
    }
}
