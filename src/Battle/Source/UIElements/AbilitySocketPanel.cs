namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Abilities;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using DragPayload = Core.Inventory.DragPayload;

    /// <summary>
    /// The socket sheet as a self-contained element: a list of ability rows, each with its slots, that
    /// takes a dragged augment in and gives one back on right-click.
    ///
    /// It deliberately knows nothing about stances. Three stance sections are a decision of the mastery
    /// window; the passive wheel wants one ability. Embedding it anywhere is three calls —
    /// <see cref="InjectServices"/>, then <see cref="ShowStance"/> or <see cref="ShowAbility"/> — and it
    /// has no output at all: it asks the board what to draw and redraws itself when the board says
    /// something moved, whoever moved it.
    ///
    /// Everything that MOVES an augment leaves through <see cref="AugmentSeating"/>, the same courier
    /// the wheel uses — the panel owns the picture and the line refusals are printed on, and nothing
    /// about the order an install goes through.
    /// </summary>
    [GlobalClass]
    public partial class AbilitySocketPanel : VBoxContainer, IRequireServices
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://dk3fp7ag2wr5h";

        /// <summary>The rows as they were built, so a drag can reach their cells without asking the
        /// engine for a child list that still holds the ones queued for deletion.</summary>
        private readonly List<AbilitySocketRow> _rowNodes = [];

        [Export] private VBoxContainer? _rows;
        [Export] private Label? _empty;
        [Export] private Label? _reason;
        [Export] private PackedScene? _rowScene;

        private IGameMessageBus? _bus;
        private IAbilitySocketBoard? _board;
        private IUiElementsManager? _windows;
        private AugmentSeating? _seating;

        private Stance? _stance;
        private string? _abilityId;

        /// <summary>A drag is in the air. Any redraw asked for while it is gets postponed: rebuilding
        /// the rows would free the node under the cursor mid-gesture.</summary>
        private bool _dragging;

        private bool _refreshPending;

        /// <summary>The services may arrive before the node is in the tree, and a fill that landed then
        /// would have nothing to draw into. Filling again here costs one read and removes the whole
        /// question of which of the two happens first.</summary>
        public override void _Ready()
        {
            if (_bus != null) Refresh();
        }

        public override void _ExitTree()
        {
            if (_board != null) _board.Changed -= OnBoardChanged;

            // The picker lives in the Overlay layer, which outlives this panel and the window holding
            // it: a list left standing over the world would still be seating augments.
            _seating?.ClosePicker();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _bus = provider.GetService<IGameMessageBus>();
            _windows = provider.GetService<IUiElementsManager>();
            _board = provider.GetService<IAbilitySocketBoard>();
            _board.Changed += OnBoardChanged;
            // The one road to the gates, with the panel's own line to print refusals on. A second copy
            // of that road beside the wheel's would be a second reading of one rule.
            _seating = new AugmentSeating(
                _bus, provider.GetService<IAugmentInstallGate>(), ShowReason, _windows);
            Refresh();
        }

        /// <summary>Show every ability of one stance.</summary>
        public void ShowStance(Stance stance)
        {
            _stance = stance;
            _abilityId = null;
            Refresh();
        }

        /// <summary>Show one ability — what the passive wheel asks for beside the node it has selected.</summary>
        public void ShowAbility(string abilityId)
        {
            _stance = null;
            _abilityId = abilityId;
            Refresh();
        }

        /// <summary>The line refusals are printed on. Guarded against a dead node rather than against a
        /// missing one: every road here is an answer that arrives later than the click that asked for it
        /// — a bus reply, or a pick made in a popup that outlives the window it was opened from — and by
        /// then the panel may have been closed and freed.</summary>
        public void ShowReason(string text)
        {
            if (_reason == null || !IsInstanceValid(_reason)) return;

            _reason.Text = text;
            _reason.Visible = !string.IsNullOrEmpty(text);
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Lights up every cell that would take the copy the moment a drag starts, and clears
        /// the lot when it ends. One pass over the visible cells per drag rather than per mouse move.</summary>
        public override void _Notification(int what)
        {
            switch ((long)what)
            {
                case NotificationDragBegin:
                    _dragging = true;
                    PreviewDrag();
                    break;
                case NotificationDragEnd:
                    _dragging = false;
                    ClearHighlights();
                    ShowReason(string.Empty);
                    if (_refreshPending) Refresh();
                    break;
            }
        }

        private void OnBoardChanged()
        {
            if (_dragging)
            {
                _refreshPending = true;
                return;
            }

            Refresh();
        }

        private async void Refresh()
        {
            try
            {
                _refreshPending = false;
                if (_bus == null || _rows == null) return;

                var views = await _bus.SendRequest<GetAbilitySocketRowsRequest, IReadOnlyList<AbilitySocketRowView>>(
                    new GetAbilitySocketRowsRequest(_stance, _abilityId));

                if (!IsInsideTree()) return;
                Rebuild(views);
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to fill the augment socket panel", exception, this);
                GD.Print($"Failed to fill the augment socket panel: {exception.Message}");
            }
        }

        /// <summary>Rebuilt whole rather than diffed: how many rows there are follows from what the
        /// player has learned, and a row kept from the previous set is a row able to outlive its
        /// ability.</summary>
        private void Rebuild(IReadOnlyList<AbilitySocketRowView> views)
        {
            if (_rows == null || _rowScene == null || _seating == null) return;

            _rowNodes.Clear();
            _rows.QueueFreeChildren();
            foreach (AbilitySocketRowView view in views)
            {
                var row = _rowScene.Instantiate<AbilitySocketRow>();
                _rows.AddChild(row);
                row.SetView(view, _seating, _windows);
                _rowNodes.Add(row);
            }

            if (_empty != null) _empty.Visible = views.Count == 0;
            ShowReason(string.Empty);
        }

        /// <summary>Marks the slots the copy under the cursor could go into, the way a support-gem panel
        /// does. Asked through the courier, like every other question the cells ask: one road to the
        /// gate for the drop, the drag preview and this pass alike.</summary>
        private void PreviewDrag()
        {
            if (_seating == null || GetViewport()?.GuiGetDragData() is not { } data) return;
            if (data.VariantType != Variant.Type.Dictionary) return;

            var payload = data.AsGodotDictionary();
            if (!payload.ContainsKey(DragPayload.Instance)) return;

            string instanceId = payload[DragPayload.Instance].AsString();
            foreach (AbilitySocketRow row in _rowNodes)
                foreach (AugmentCell cell in row.Cells)
                    cell.SetHighlight(cell.WouldAccept(_seating, instanceId));
        }

        private void ClearHighlights()
        {
            foreach (AbilitySocketRow row in _rowNodes)
                foreach (AugmentCell cell in row.Cells)
                    cell.SetHighlight(null);
        }
    }
}
