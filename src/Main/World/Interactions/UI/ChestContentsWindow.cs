namespace LastBreath.World.Interactions.UI
{
    using Core.Localization;
    using Core.World.Interactions;
    using Godot;
    using Containers;

    public partial class ChestContentsWindow : InteractionWindow
    {
        public static PackedScene Initialize() => GD.Load<PackedScene>("res://World/Interactions/UI/ChestContentsWindow.tscn");
        public override void _Ready()
        {
            base._Ready();
            GetNode<Button>("Frame/Content/TakeAll").Pressed += () => Take(null);
        }
        public override void Refresh()
        {
            if (Target?.GetParent() is not ChestComponent chest) return;
            ClearRows();
            GetNode<Label>("Frame/Content/Title").Text = Localization.Localize(chest.NameKey);
            var takeAll = GetNode<Button>("Frame/Content/TakeAll");
            takeAll.Text = $"{InteractionPresentation.Binding(InteractionActions.TakeAll)} · {Localization.Localize("UI_Container_TakeAll")}";
            takeAll.Disabled = chest.Contents.Empty;
            foreach (var slot in chest.Contents.Slots)
            {
                var row = GD.Load<PackedScene>("res://World/Interactions/UI/ChestItemRow.tscn").Instantiate<Control>();
                row.GetNode<TextureRect>("Icon").Texture = slot.Amount > 0 ? slot.Item?.Icon : null;
                row.GetNode<Label>("Name").Text = slot.Amount > 0 ? slot.Item?.DisplayName ?? "" : "—";
                row.GetNode<Label>("Amount").Text = slot.Amount > 0 ? slot.Amount.ToString() : "";
                row.TooltipText = slot.Amount > 0 ? slot.Item?.DisplayName ?? "" : "";
                row.GuiInput += e =>
                {
                    if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } || slot.Amount == 0) return;
                    row.AcceptEvent();
                    Take(slot.Id);
                };
                Rows.AddChild(row);
            }
        }
        private void Take(string? slotId)
        {
            if (Target == null) return;
            _ = Messages.SendRequest<ContainerTransferRequest, InteractionResult>(new(Target.Handle, slotId));
        }
        public override void _UnhandledInput(InputEvent e)
        {
            if (!e.IsActionPressed(InteractionActions.TakeAll) || e.IsEcho()) return;
            GetViewport().SetInputAsHandled();
            Take(null);
        }
    }
}
