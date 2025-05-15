namespace Playground.Script.Enemy
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public class EnemyInventory : Inventory
    {
        public List<Item> GivePlayerItems() => [.. Slots.Where(x => x.CurrentItem != null).Select(x => x.CurrentItem!)];
    }
}
