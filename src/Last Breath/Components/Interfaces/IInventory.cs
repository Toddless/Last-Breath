namespace Playground.Components.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.Items;

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
