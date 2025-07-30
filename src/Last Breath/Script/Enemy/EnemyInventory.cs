namespace LastBreath.Script.Enemy
{
    using System.Collections.Generic;
    using System.Linq;
    using LastBreath.Script.Inventory;
    using LastBreath.Script.Items;

    public class EnemyInventory : Inventory
    {
        public List<Item> GivePlayerItems() => [.. Slots.Where(x => x.CurrentItem != null).Select(x => x.CurrentItem!)];
    }
}
