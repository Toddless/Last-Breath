namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Abilities;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
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
    /// The one place it touches a service directly instead of the bus is the drag check: the engine
    /// asks whether a drop is allowed synchronously and the request bus answers asynchronously, so the
    /// two cannot be joined. That is a READ — every operation still goes through the bus — and it reads
    /// the very gate the operation will go through, which is the only arrangement in which the cursor
    /// cannot promise something the bus then refuses.
    /// </summary>
    [GlobalClass]
    public partial class AbilitySocketPanel : VBoxContainer, IRequireServices, IAugmentCellHost
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://dk3fp7ag2wr5h";

        /// <summary>Localisation keys of the extraction refusals, by the answer's own name.</summary>
        private const string ExtractRefusalPrefix = "UI_Augment_Extract_";

        /// <summary>The rows as they were built, so a drag can reach their cells without asking the
        /// engine for a child list that still holds the ones queued for deletion.</summary>
        private readonly List<AbilitySocketRow> _rowNodes = [];

        [Export] private VBoxContainer? _rows;
        [Export] private Label? _empty;
        [Export] private Label? _reason;
        [Export] private PackedScene? _rowScene;

        private IGameMessageBus? _bus;
        private IAugmentInstallGate? _gate;
        private IAbilitySocketBoard? _board;
        private IUiElementsManager? _windows;

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
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _bus = provider.GetService<IGameMessageBus>();
            _gate = provider.GetService<IAugmentInstallGate>();
            _windows = provider.GetService<IUiElementsManager>();
            _board = provider.GetService<IAbilitySocketBoard>();
            _board.Changed += OnBoardChanged;
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

        public AugmentInstallResult Judge(string socketAddress, string itemInstanceId) =>
            _gate?.Judge(socketAddress, itemInstanceId) ?? new AugmentInstallResult(AugmentInstallOutcome.NoSuchSocket);

        public void Install(string socketAddress, string itemInstanceId) => SendInstall(socketAddress, itemInstanceId);

        public void Extract(string socketAddress) => SendExtract(socketAddress);

        public void ShowReason(string text)
        {
            if (_reason == null) return;

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
            if (_rows == null || _rowScene == null) return;

            _rowNodes.Clear();
            _rows.QueueFreeChildren();
            foreach (AbilitySocketRowView view in views)
            {
                var row = _rowScene.Instantiate<AbilitySocketRow>();
                _rows.AddChild(row);
                row.SetView(view, this, _windows);
                _rowNodes.Add(row);
            }

            if (_empty != null) _empty.Visible = views.Count == 0;
            ShowReason(string.Empty);
        }

        /// <summary>Marks the slots the copy under the cursor could go into, the way a support-gem panel
        /// does. The answer comes from the same gate the drop will go through.</summary>
        private void PreviewDrag()
        {
            if (_gate == null || GetViewport()?.GuiGetDragData() is not { } data) return;
            if (data.VariantType != Variant.Type.Dictionary) return;

            var payload = data.AsGodotDictionary();
            if (!payload.ContainsKey(DragPayload.Instance)) return;

            string instanceId = payload[DragPayload.Instance].AsString();
            foreach (AbilitySocketRow row in _rowNodes)
                foreach (AugmentCell cell in row.Cells)
                    cell.SetHighlight(cell.WouldAccept(_gate, instanceId));
        }

        private void ClearHighlights()
        {
            foreach (AbilitySocketRow row in _rowNodes)
                foreach (AugmentCell cell in row.Cells)
                    cell.SetHighlight(null);
        }

        /// <summary>The bus is the authority: the preview under the cursor was advisory, and between it
        /// and the drop the bag could be emptied or a node refunded by another window. A refusal is
        /// reported from the ANSWER; a success needs no redraw of its own, because the board says so.</summary>
        private async void SendInstall(string socketAddress, string itemInstanceId)
        {
            try
            {
                if (_bus == null) return;
                var result = await _bus.SendRequest<InstallAugmentRequest, AugmentInstallResult>(
                    new InstallAugmentRequest(socketAddress, itemInstanceId));

                if (!result.Installed) Announce(AugmentRefusalText.KeyFor(result));
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to install an augment", exception, this);
                GD.Print($"Failed to install an augment: {exception.Message}");
            }
        }

        private async void SendExtract(string socketAddress)
        {
            try
            {
                if (_bus == null) return;
                var result = await _bus.SendRequest<ExtractAugmentRequest, AugmentExtractResult>(
                    new ExtractAugmentRequest(socketAddress));

                if (result != AugmentExtractResult.Extracted) Announce($"{ExtractRefusalPrefix}{result}");
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to extract an augment", exception, this);
                GD.Print($"Failed to extract an augment: {exception.Message}");
            }
        }

        /// <summary>A refusal is said twice on purpose: on the panel's own line, where the player is
        /// looking, and as a toast, because the panel may have been closed by the time the bus answers.</summary>
        private void Announce(string localizationKey)
        {
            ShowReason(Localization.Localize(localizationKey));
            _ = _bus?.PublishMessageAsync(new SendNotificationMessageMessage(localizationKey));
        }
    }
}
