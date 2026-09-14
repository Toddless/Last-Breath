namespace LastBreath.World.Interactions.UI
{
    using System.Linq;
    using Core.Localization;
    using Core.World.Interactions;
    using Godot;

    public partial class InteractionMenuWindow : InteractionWindow
    {
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
            ClearRows();
            GetNode<Label>("Frame/Content/Title").Text = Localization.Localize("UI_Interaction_Interact");
            foreach (var action in actions)
            {
                var row = GD.Load<PackedScene>("res://World/Interactions/UI/InteractionActionRow.tscn").Instantiate<Button>();
                row.Text = Localization.Localize(action.LabelKey);
                row.Disabled = !action.Enabled;
                row.TooltipText = action.ReasonKey == null ? "" : Localization.Localize(action.ReasonKey);
                row.Pressed += () => _ = Messages.SendRequest<ExecuteInteractionRequest, InteractionResult>(new(Target.Handle, action.Id));
                Rows.AddChild(row);
            }
            for (int i = 0; i < Rows.GetChildCount(); i++)
                if (Rows.GetChild(i) is Button { Disabled: false } button) { button.GrabFocus(); break; }
        }
        public override void _Process(double delta)
        {
            base._Process(delta);
            _elapsed += delta;
            if (_elapsed < 0.1) return;
            _elapsed = 0;
            if (!IsQueuedForDeletion()) Refresh();
        }
    }
}
