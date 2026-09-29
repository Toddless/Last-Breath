namespace LastBreath.World.Interactions.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Localization;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Godot;

    public partial class InteractionMenuWindow : InteractionWindow
    {
        private const string UID = "uid://cxgma3niixgpr";
        private string _signature = "";
        /// <summary>Cached offer the rows were built from; the menu rebuilds once its target keeps another list instance.</summary>
        private IReadOnlyList<InteractionAction>? _shownOffer;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void Refresh()
        {
            if (Rows == null) return;
            var actions = Target.CachedOffer;
            _shownOffer = actions;
            string signature = string.Join("|", actions.Select(x => $"{x.Id}:{x.Enabled}:{x.ReasonKey}"));
            if (_signature == signature) return;
            _signature = signature;
            string? focusedActionId = FocusedActionId();
            bool closeFocused = CloseButton?.HasFocus() == true;
            // The console, the bag filter and this menu share one root window: a rebuild must leave a typing player alone.
            bool typing = Core.World.Spaces.SpatialAccess.HasTextFocus(this);
            ClearRows();
            Title?.Text = Localization.Localize(InteractionPresentation.InteractKey);
            var rows = new List<(InteractionAction Action, Button Row)>(actions.Count);
            foreach (var action in actions)
            {
                var row = InteractionActionRow.Initialize().Instantiate<InteractionActionRow>();
                row.SetAction(action);
                row.ActionPressed += ExecuteAction;
                Rows.AddChild(row);
                rows.Add((action, row));
            }
            if (!typing) FocusCandidate(rows, focusedActionId, closeFocused)?.GrabFocus();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (!IsQueuedForDeletion() && !ReferenceEquals(Target.CachedOffer, _shownOffer)) Refresh();
        }

        /// <summary>Sends the pressed row's action to the bound target.</summary>
        private void ExecuteAction(string actionId) =>
            _ = Messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(Target.Handle, actionId));

        /// <summary>Action id of the row holding keyboard focus on itself or on a control inside it; null when the focus is outside the rows.</summary>
        private string? FocusedActionId() =>
            GetViewport()?.GuiGetFocusOwner()?.FindSelfOrAncestor<InteractionActionRow>() is { } row && row.GetParent() == Rows
                ? row.ActionId
                : null;

        /// <summary>Focus after a rebuild: where the player left it (Close, or the same action while it stays enabled), else the
        /// first enabled action without an explicit-choice policy, else Close. An explicit-choice row is never focused automatically.</summary>
        private Button? FocusCandidate(IReadOnlyList<(InteractionAction Action, Button Row)> rows, string? focusedActionId, bool closeFocused)
        {
            if (closeFocused) return CloseButton;
            return rows.Where(x => x.Action.Enabled && x.Action.Id == focusedActionId).Select(x => x.Row).FirstOrDefault()
                ?? rows.Where(x => x.Action.Enabled && !x.Action.ExplicitChoice).Select(x => x.Row).FirstOrDefault()
                ?? CloseButton;
        }
    }
}
