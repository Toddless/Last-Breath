namespace LastBreath.World.Interactions.UI
{
    using System;
    using System.Linq;
    using Core;
    using Core.Constants;
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.Views.UI;
    using Core.World.Containers;
    using Core.World.Interactions;
    using Godot;
    using Containers;
    using Inventory;
    using LastBreath.UI;

    /// <summary>The open chest as a grid of cells in the look of bag slots: slot i sits in cell i, over as many cells as the chest's
    /// capacity and never fewer than its slots.</summary>
    public partial class ChestContentsWindow : InteractionWindow, IContainerSession
    {
        private const string UID = "uid://dfyvofbp38pam";
        private const string TakeAllKey = "UI_Container_TakeAll";
        private const string MissingCellsFormat = "Chest window '{0}' shows no contents: it needs its cell grid set in the scene";
        private const string MissingTakeAllFormat = "Chest window '{0}' has no take-all button: it needs it set in the scene";

        /// <summary>Grid the cells are laid out in; its column count is set in the scene.</summary>
        [Export] private GridContainer? _cells;
        [Export] private Button? _takeAll;
        private IUiElementsManager _uiElements = null!;

        /// <summary>The chest the bound target belongs to, its nearest chest ancestor; null for a target outside every chest.</summary>
        private ChestComponent? Chest => Target.FindSelfOrAncestor<ChestComponent>();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void InjectServices(IGameServiceProvider provider)
        {
            base.InjectServices(provider);
            _uiElements = provider.GetService<IUiElementsManager>();
        }

        public override void _Ready()
        {
            ReportMissingExports();
            base._Ready();
            _takeAll?.Pressed += () => Take(null);
        }

        public override void Refresh()
        {
            if (Chest is not { } chest) return;
            Title?.Text = Localization.Localize(chest.NameKey);
            _takeAll?.Text = InteractionPresentation.Prompt(Settings.ContainerTakeAll, TakeAllKey);
            _takeAll?.Disabled = chest.Contents.Empty;
            ShowContents(chest);
        }

        /// <summary>Moves the slot, or every slot, of the chest into the bag while the check holds, then redraws the cells; Unavailable
        /// for a target that is not a chest's or a slot the chest does not have.</summary>
        public InteractionResult Transfer(string? slotId, Func<bool> accessible)
        {
            if (Chest is not { } chest || slotId != null && !chest.Contents.Slots.Any(x => x.Id == slotId))
                return InteractionResult.Unavailable;
            var transfer = chest.Transfer(slotId, accessible);
            Refresh();
            return ResultOf(transfer);
        }

        public override void _UnhandledInput(InputEvent e)
        {
            if (!e.IsActionPressed(Settings.ContainerTakeAll) || e.IsEcho()) return;
            GetViewport().SetInputAsHandled();
            Take(null);
        }

        /// <summary>Completed when anything moved, refused otherwise; the container-full reason when capacity kept items in the chest.</summary>
        private static InteractionResult ResultOf(ChestTransfer transfer) =>
            new(transfer.Accepted > 0 ? InteractionOutcome.Completed : InteractionOutcome.Refused,
                transfer.CapacityLimited ? InteractionReasonKeys.ContainerFull : null);

        private void Take(string? slotId)
        {
            _ = Messages.SendRequest<ContainerTransferRequest, InteractionResult>(new(Target.Handle, slotId));
        }

        /// <summary>Lays slot i into cell i and empties the cells past the last slot; the cells already built are reused and only the
        /// missing ones are added.</summary>
        private void ShowContents(ChestComponent chest)
        {
            if (_cells == null) return;
            var slots = chest.Contents.Slots;
            AddMissingCells(_cells, Math.Max(chest.Capacity, slots.Count));
            for (int index = 0; index < _cells.GetChildCount(); index++)
                ShowSlot(_cells.GetChild<InventorySlot>(index), slots.ElementAtOrDefault(index));
        }

        /// <summary>Adds cells to the grid until it holds <paramref name="count"/>.</summary>
        private void AddMissingCells(GridContainer grid, int count)
        {
            for (int index = grid.GetChildCount(); index < count; index++) grid.AddChild(CreateCell(index));
        }

        /// <summary>A cell in the look of a bag slot for the slot laid into it at this index; it neither starts nor takes a drag, since
        /// only the transfer request moves an item out of the chest.</summary>
        private InventorySlot CreateCell(int index)
        {
            var cell = InventorySlot.Initialize().Instantiate<InventorySlot>();
            cell.DragAndDropEnabled = false;
            cell.GetItemInstance = FindItem;
            cell.GetItemIcon = instanceId => FindItem(instanceId)?.Icon;
            cell.ItemInteraction += (_, interaction) => OnCellInteraction(index, interaction);
            HoverTooltip.Attach(cell, () => ShowTooltip(index));
            return cell;
        }

        /// <summary>Shows the slot's item and amount in the cell; an emptied slot, or none at all, leaves the cell empty.</summary>
        private static void ShowSlot(InventorySlot cell, ChestSlot? slot)
        {
            if (slot is { Amount: > 0, Item: { } item }) cell.SetItem(new ItemInstance(item.Id, item.InstanceId, item.MaxStackSize), slot.Amount);
            else cell.ClearSlot();
        }

        /// <summary>The item of the filled slot holding this instance; null when no filled slot does.</summary>
        private IItem? FindItem(string instanceId) =>
            Chest?.Contents.Slots.FirstOrDefault(slot => slot.Amount > 0 && slot.Item?.InstanceId == instanceId)?.Item;

        /// <summary>The slot laid into the cell at this index while it holds an item; null for an empty cell.</summary>
        private ChestSlot? FilledSlotAt(int index) =>
            Chest?.Contents.Slots.ElementAtOrDefault(index) is { Amount: > 0, Item: not null } slot ? slot : null;

        /// <summary>A right-button press on a filled cell takes its slot, whatever modifier is held.</summary>
        private void OnCellInteraction(int index, MouseInteractions interaction)
        {
            if (interaction is not (MouseInteractions.RightClick or MouseInteractions.CtrRmb or MouseInteractions.AltRmb)) return;
            if (FilledSlotAt(index) is { } slot) Take(slot.Id);
        }

        /// <summary>The full item tooltip for the item the cell holds when it opens; an empty cell opens none.</summary>
        private IPopup? ShowTooltip(int index)
        {
            if (FilledSlotAt(index)?.Item is not { } item || _uiElements.ShowPopup(typeof(ItemTooltipPopup)) is not ItemTooltipPopup popup)
                return null;
            popup.ShowItem(item);
            return popup;
        }

        /// <summary>Writes each required scene reference left unset to the log.</summary>
        private void ReportMissingExports()
        {
            if (_cells == null) Report(MissingCellsFormat);
            if (_takeAll == null) Report(MissingTakeAllFormat);
        }

        /// <summary>Writes a setup problem of this window, formatted with its path, to the log.</summary>
        private void Report(string format) => Tracker.TrackError(string.Format(format, GetPath()), this);
    }
}
