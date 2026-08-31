namespace SharedUi
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
    /// <para>
    /// It is the shared <see cref="IPickerPopup"/>: the socket screens pick an augment out of the bag
    /// the same way crafting picks a piece, and one list of candidates at the cursor is one scene
    /// rather than two that drift apart.
    /// </para>
    /// </summary>
    public partial class ResourcePickerPopup : Control, IPickerPopup
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://dupapy2hnba7p";
        private const float CursorOffset = 16f;

        [Export] private PanelContainer? _panel;
        [Export] private Label? _title;
        [Export] private ItemList? _list;
        [Export] private Button? _cancel;

        private readonly List<PickerEntry> _entries = [];
        private Action<string>? _onPicked;
        private HoverTooltipHandle? _preview;

        public PopupLifetime Lifetime => PopupLifetime.Pinned;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public override void _Ready()
        {
            _cancel?.Pressed += Close;
            if (_list != null)
            {
                _list.ItemSelected += OnItemSelected;
                // Entries carrying a Preview open their own popup (the full item card) instead of the
                // engine tooltip. ItemList rows are not controls, so the picker reads the hovered row
                // off the mouse itself and the shared handle owns the delay and the lifetime.
                _preview = HoverTooltip.Follow(this, ShowPreview);
                _list.GuiInput += OnListGuiInput;
                _list.MouseExited += () => _preview?.Target(null);
            }

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
            _entries.Clear();
            _preview?.Cancel();
            _list?.Clear();
            foreach (var entry in entries)
            {
                _entries.Add(entry);
                if (_list == null) continue;
                int index = _list.AddItem(entry.Label, entry.Icon);
                // The engine tooltip only where no Preview popup takes the hover instead. A Preview
                // row disables it OUTRIGHT — ItemList otherwise floats the row's own label as a
                // native tooltip right next to the full item card.
                if (entry.Preview != null) _list.SetItemTooltipEnabled(index, false);
                else if (!string.IsNullOrEmpty(entry.Tooltip)) _list.SetItemTooltip(index, entry.Tooltip);
                if (entry.LabelColor is { } color) _list.SetItemCustomFgColor(index, color);
            }
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnItemSelected(long index)
        {
            if (index < 0 || index >= _entries.Count) return;
            var callback = _onPicked;
            string picked = _entries[(int)index].Id;
            Close();
            callback?.Invoke(picked);
        }

        /// <summary>Retargets the preview handle to the row under the cursor (rows without a Preview
        /// count as nothing hovered — their engine tooltip does the talking).</summary>
        private void OnListGuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseMotion motion || _list == null || _preview == null) return;
            int index = _list.GetItemAtPosition(motion.Position, exact: true);
            bool hasPreview = index >= 0 && index < _entries.Count && _entries[index].Preview != null;
            _preview.Target(hasPreview ? index : (object?)null);
        }

        private IPopup? ShowPreview(object? key) =>
            key is int index && index >= 0 && index < _entries.Count ? _entries[index].Preview?.Invoke() : null;

        private void Place()
        {
            if (_panel == null) return;
            UiPlacement.PlaceClamped(_panel, GetGlobalMousePosition(), new Vector2(CursorOffset, CursorOffset));
            _panel.Visible = true;
        }
    }
}
