namespace Core.Inventory
{
    using Godot;

    /// <summary>A bag service that owns its slot nodes for the whole session and lends them into a
    /// window's grid. Borrowers must give the slots back before dying.</summary>
    public interface ISlotLender
    {
        /// <summary>Shows the bag inside the given grid; every borrowed slot view is redrawn.</summary>
        void AttachSlots(GridContainer container);

        /// <summary>Takes the slot nodes back from whatever grid borrowed them.</summary>
        void DetachSlots();
    }
}
