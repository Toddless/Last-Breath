namespace LastBreath.Inventory
{
    using System;
    using Core.Constants;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Godot;

    public partial class InventorySlot : Slot, IInventorySlot
    {
        private const string UID = "uid://bqlqfsqoepfhs";

        [Export] protected Label? QuantityLabel;

        public Func<string, IItem?>? GetItemInstance;
        public event Action<IInventorySlot, MouseInteractions>? ItemInteraction;
        public event Action<EquipmentPiece, IInventorySlot>? EquipmentDropped;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true } mb || CurrentItem == null) return;

            var interaction = ToInteraction(mb);
            if (interaction == MouseInteractions.None) return;
            ItemInteraction?.Invoke(this, interaction);
            AcceptEvent();
        }

        protected override void RefreshUi()
        {
            base.RefreshUi();
            QuantityLabel?.Text = Quantity > 1 ? Quantity.ToString() : string.Empty;
            var item = GetItemInstance?.Invoke(CurrentItem?.InstanceId ?? string.Empty);
            Background?.Texture = item is null ? null : ResourceLoader.Load<Texture2D?>(AssetPaths.SlotBackground(item.Rarity));
            UpdateRarityFrame();
        }

        protected override void OnEquipmentDropped(EquipmentPiece piece) => EquipmentDropped?.Invoke(piece, this);

        /// <summary>The slot frame is tinted by the item's rarity; an empty slot stays neutral.</summary>
        private void UpdateRarityFrame()
        {
            var item = CurrentItem == null ? null : GetItemInstance?.Invoke(CurrentItem.InstanceId);
            Frame?.Modulate = item == null ? Colors.White : Color.FromHtml(TextPalette.RarityColor(item.Rarity));
        }

        private static MouseInteractions ToInteraction(InputEventMouseButton mb) => mb switch
        {
            { ButtonIndex: MouseButton.Left, AltPressed: true } => MouseInteractions.AltLmb,
            { ButtonIndex: MouseButton.Right, AltPressed: true } => MouseInteractions.AltRmb,
            { ButtonIndex: MouseButton.Left, CtrlPressed: true } => MouseInteractions.CtrLmb,
            { ButtonIndex: MouseButton.Right, CtrlPressed: true } => MouseInteractions.CtrRmb,
            { ButtonIndex: MouseButton.Right } => MouseInteractions.RightClick,
            { ButtonIndex: MouseButton.Left } => MouseInteractions.LeftClick,
            _ => MouseInteractions.None,
        };
    }
}
