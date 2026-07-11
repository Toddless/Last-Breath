namespace LastBreath.Inventory
{
    using System;
    using Core.Enums;
    using Core.Items;
    using Godot;
    using Godot.Collections;

    /// <summary>
    /// One row of the equipment column: a drop target for a matching piece dragged from the bag
    /// and a drag source for unequipping into the bag. The window supplies the checks/actions —
    /// the row itself knows nothing about the inventory.
    /// </summary>
    public partial class EquipmentRow : HBoxContainer
    {
        private EquipmentPiece _piece;
        private IEquipItem? _equipped;
        private Func<string, bool>? _canAcceptInstance;
        private Action<string>? _acceptInstance;

        public void Setup(EquipmentPiece piece, IEquipItem? equipped, Func<string, bool> canAcceptInstance, Action<string> acceptInstance)
        {
            _piece = piece;
            _equipped = equipped;
            _canAcceptInstance = canAcceptInstance;
            _acceptInstance = acceptInstance;
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (_equipped == null) return new Variant();

            var preview = new Label { Text = _equipped.DisplayName };
            SetDragPreview(preview);
            return new Dictionary { ["EquipmentPiece"] = (int)_piece };
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.VariantType != Variant.Type.Dictionary) return false;
            var payload = data.AsGodotDictionary();
            if (!payload.ContainsKey("Instance")) return false;
            return _canAcceptInstance?.Invoke(payload["Instance"].AsString()) ?? false;
        }

        public override void _DropData(Vector2 atPosition, Variant data) =>
            _acceptInstance?.Invoke(data.AsGodotDictionary()["Instance"].AsString());
    }
}
