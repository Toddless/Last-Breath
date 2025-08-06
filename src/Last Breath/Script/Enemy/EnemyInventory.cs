namespace LastBreath.Script.Enemy
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Items;
    using LastBreath.Script.Inventory;

    public class EnemyInventory : Inventory
    {
        public List<IItem> GivePlayerItems() => [.. Slots.Where(x => x.CurrentItem != null).Select(x => x.CurrentItem!)];
    }
}
