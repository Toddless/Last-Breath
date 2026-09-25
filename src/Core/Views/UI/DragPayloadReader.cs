namespace Core.Views.UI
{
    using Godot;
    using Godot.Collections;
    using Inventory;

    /// <summary>
    /// Reading the one key every drag source in the game puts into its payload: which copy of an item
    /// is being carried. A bag slot, an equipment slot and an augment tray tile all write it, so
    /// whoever accepts a drop reads it the same way and an augment travels from any of them without a
    /// second protocol.
    /// </summary>
    public static class DragPayloadReader
    {
        /// <summary>True when the payload names an item instance. False for anything else being
        /// dragged, which is exactly what a drop target needs in order to refuse quietly.</summary>
        public static bool TryReadInstance(Variant data, out string instanceId)
        {
            instanceId = string.Empty;
            if (data.VariantType != Variant.Type.Dictionary) return false;

            Dictionary payload = data.AsGodotDictionary();
            if (!payload.ContainsKey(DragPayload.Instance)) return false;

            instanceId = payload[DragPayload.Instance].AsString();
            return !string.IsNullOrEmpty(instanceId);
        }
    }
}
