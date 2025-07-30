namespace LastBreath.Components.Interfaces
{
    using System;
    using System.Collections.Generic;
    using LastBreath.Script.Items;

    public interface IInventory
    {
        List<Item?> GiveAllItems();

        void TakeAllItems(List<Item?> items);

        Action? ShowInventory
        {
            get; set;
        }

        Action? HideInventory
        {
            get; set;
        }
    }
}
