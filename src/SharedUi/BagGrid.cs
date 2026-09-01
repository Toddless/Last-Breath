namespace SharedUi
{
    using System;
    using Core.Inventory;
    using Godot;

    /// <summary>
    /// The borrowed bag view: a scrollable grid the inventory service lends its slot nodes into.
    /// Attach/Detach mirror the service's lending contract; the filter only dims non-matching
    /// slots (the grid IS the physical bag), and a detach always hands the slots back untinted —
    /// the nodes outlive this view.
    /// </summary>
    public partial class BagGrid : ScrollContainer
    {
        private static readonly Color s_filteredOut = new(1f, 1f, 1f, 0.28f);

        [Export] private GridContainer? _grid;
        [Export] private int _columns = 1;

        private ISlotLender? _lender;

        public override void _Ready() => _grid?.Columns = _columns;

        /// <summary>Borrows the service-owned slot nodes into this grid.</summary>
        public void Attach(ISlotLender lender)
        {
            _lender = lender;
            if (_grid != null) lender.AttachSlots(_grid);
        }

        /// <summary>Returns the slots to the service, tints reset for the next borrower.</summary>
        public void Detach()
        {
            ApplyFilter(null);
            _lender?.DetachSlots();
            _lender = null;
        }

        /// <summary>Dims the slots the predicate rejects; null clears the dimming.</summary>
        public void ApplyFilter(Func<IInventorySlot, bool>? filteredOut)
        {
            if (_grid == null) return;
            foreach (var child in _grid.GetChildren())
                if (child is Control control and IInventorySlot slot)
                    control.Modulate = filteredOut?.Invoke(slot) == true ? s_filteredOut : Colors.White;
        }
    }
}
