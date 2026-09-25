namespace LastBreath.World.Interactions.UI
{
    using System;
    using System.Linq;
    using Core.Localization;
    using Core.Views.UI;
    using Core.World.Containers;
    using Core.World.Interactions;
    using Godot;
    using Containers;

    public partial class ChestContentsWindow : InteractionWindow, IContainerSession
    {
        private const string UID = "uid://dfyvofbp38pam";
        private const string TakeAllKey = "UI_Container_TakeAll";
        [Export] private Button? _takeAll;
        /// <summary>The chest the bound target belongs to, its nearest chest ancestor; null for a target outside every chest.</summary>
        private ChestComponent? Chest => Target.FindSelfOrAncestor<ChestComponent>();
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
        public override void _Ready()
        {
            base._Ready();
            _takeAll?.Pressed += () => Take(null);
        }
        public override void Refresh()
        {
            if (Chest is not { } chest || Rows == null) return;
            ClearRows();
            Title?.Text = Localization.Localize(chest.NameKey);
            _takeAll?.Text = $"{InteractionPresentation.Binding(InteractionActions.TakeAll)} · {Localization.Localize(TakeAllKey)}";
            _takeAll?.Disabled = chest.Contents.Empty;
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
        /// <summary>Moves the slot, or every slot, of the chest into the bag while the check holds, then redraws the rows; Unavailable
        /// for a target that is not a chest's or a slot the chest does not have.</summary>
        public InteractionResult Transfer(string? slotId, Func<bool> accessible)
        {
            if (Chest is not { } chest || slotId != null && !chest.Contents.Slots.Any(x => x.Id == slotId))
                return InteractionResult.Unavailable;
            var transfer = chest.Transfer(slotId, accessible);
            Refresh();
            return ResultOf(transfer);
        }
        /// <summary>Completed when anything moved, refused otherwise; the container-full reason when capacity kept items in the chest.</summary>
        private static InteractionResult ResultOf(ChestTransfer transfer) =>
            new(transfer.Accepted > 0 ? InteractionOutcome.Completed : InteractionOutcome.Refused,
                transfer.CapacityLimited ? InteractionReasonKeys.ContainerFull : null);
        private void Take(string? slotId)
        {
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
