namespace LastBreath.UI.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;
    using Inventory;

    /// <summary>
    /// The equipment paperdoll: header plus the ten slot panels laid out over the doll silhouette.
    /// The slots know nothing about the inventory — the window hands the checks, actions and the
    /// tooltip in through <see cref="Bind"/> and pushes the equipped pieces down via <see cref="Refresh"/>.
    /// </summary>
    public partial class PaperDoll : VBoxContainer
    {
        [Export] private Label? _header;
        [Export] private Control? _doll;

        private IEnumerable<EquipmentSlot> Slots =>
            _doll?.GetChildren().OfType<EquipmentSlot>() ?? [];

        public override void _Ready() => _header?.Text = Localization.Localize("UI_Inv_Equipment");

        /// <summary>Wires every doll slot to the window's checks and actions.</summary>
        public void Bind(Func<EquipmentPiece, string, bool> canAcceptInstance, Action<EquipmentPiece, string> acceptInstance,
            Action<EquipmentPiece> unequip, Func<EquipmentPiece, IPopup?> tooltip)
        {
            foreach (var slot in Slots)
            {
                var piece = slot.Piece;
                slot.Bind(instanceId => canAcceptInstance(piece, instanceId), instanceId => acceptInstance(piece, instanceId), unequip);
                HoverTooltip.Attach(slot, () => tooltip(piece));
            }
        }

        /// <summary>Redraws every slot from what is currently equipped.</summary>
        public void Refresh(IEquipmentComponent? equipment)
        {
            foreach (var slot in Slots)
                slot.SetEquipped(equipment?.GetEquipped(slot.Piece));
        }
    }
}
