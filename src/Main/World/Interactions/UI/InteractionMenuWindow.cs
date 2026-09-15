namespace LastBreath.World.Interactions.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Localization;
    using Core.World.Interactions;
    using Godot;

    public partial class InteractionMenuWindow : InteractionWindow
    {
        private const string ActionIdMeta = "interaction_action_id";
        private const double RefreshIntervalSeconds = 0.1;
        private string _signature = "";
        private double _elapsed;
        public static PackedScene Initialize() => GD.Load<PackedScene>("res://World/Interactions/UI/InteractionMenuWindow.tscn");
        public override void Refresh()
        {
            if (Target == null) return;
            var actions = Target.ReadActions();
            string signature = string.Join("|", actions.Select(x => $"{x.Id}:{x.Enabled}:{x.ReasonKey}"));
            if (_signature == signature) return;
            _signature = signature;
            string? focusedActionId = FocusedActionId();
            bool closeFocused = CloseButton.HasFocus();
            ClearRows();
            GetNode<Label>("Frame/Content/Title").Text = Localization.Localize("UI_Interaction_Interact");
            var rows = new List<(InteractionAction Action, Button Row)>(actions.Count);
            foreach (var action in actions)
            {
                var row = GD.Load<PackedScene>("res://World/Interactions/UI/InteractionActionRow.tscn").Instantiate<Button>();
                row.SetMeta(ActionIdMeta, action.Id);
                row.Text = Localization.Localize(action.LabelKey);
                row.Disabled = !action.Enabled;
                row.TooltipText = action.ReasonKey == null ? "" : Localization.Localize(action.ReasonKey);
                row.Pressed += () => _ = Messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(Target.Handle, action.Id));
                Rows.AddChild(row);
                rows.Add((action, row));
            }
            FocusCandidate(rows, focusedActionId, closeFocused).GrabFocus();
        }
        public override void _Process(double delta)
        {
            base._Process(delta);
            _elapsed += delta;
            if (_elapsed < RefreshIntervalSeconds) return;
            _elapsed = 0;
            if (!IsQueuedForDeletion()) Refresh();
        }

        /// <summary>Action id of the row holding keyboard focus; null when the focus is outside the rows.</summary>
        private string? FocusedActionId() =>
            GetViewport()?.GuiGetFocusOwner() is { } owner && owner.GetParent() == Rows && owner.HasMeta(ActionIdMeta)
                ? owner.GetMeta(ActionIdMeta).AsString()
                : null;

        /// <summary>Focus after a rebuild: where the player left it (Close, or the same action while it stays enabled), else the
        /// first enabled action without an explicit-choice policy, else Close. An explicit-choice row is never focused automatically.</summary>
        private Button FocusCandidate(IReadOnlyList<(InteractionAction Action, Button Row)> rows, string? focusedActionId, bool closeFocused)
        {
            if (closeFocused) return CloseButton;
            return rows.Where(x => x.Action.Enabled && x.Action.Id == focusedActionId).Select(x => x.Row).FirstOrDefault()
                ?? rows.Where(x => x.Action.Enabled && !x.Action.ExplicitChoice).Select(x => x.Row).FirstOrDefault()
                ?? CloseButton;
        }
    }
}
