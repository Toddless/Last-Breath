namespace Core.Save
{
    /// <summary>
    /// Canonical restore priorities. The order encodes data dependencies:
    /// modifier sources (items) come before their consumers (parameters), vitals come last
    /// because their setters clamp against Max values that must settle first.
    /// </summary>
    public static class RestoreOrder
    {
        /// <summary>World clock, faction relations — depend on nothing.</summary>
        public const int World = 0;

        /// <summary>Gold wallet — one number, depends on nothing.</summary>
        public const int Wallet = 5;

        public const int Mastery = 10;

        /// <summary>Passive tree allocation: after Mastery, which hands out the points the allocation
        /// spends, and before Items so the tree's modifier source is in place when equipment — the
        /// other source on the same parameters — settles.</summary>
        public const int PassiveTree = 15;

        /// <summary>Inventory + equipment: the parameter modifier sources.</summary>
        public const int Items = 20;

        /// <summary>Trader shelves: after the world clock, whose game-time the restored restock deadlines
        /// are measured against, and beside Items — a shelf holds concrete instances written in the same
        /// shape the bag writes its own.</summary>
        public const int TraderShelf = 22;

        /// <summary>Drops lying on the floor: the third place holding concrete item instances, written in
        /// the shape the bag and the shelf write theirs. Neighbourhood, not dependency — the drops are
        /// rebuilt from item data alone, so no section has to precede this one.</summary>
        public const int GroundItems = 23;

        /// <summary>Quest log: after Items so restored states sit on the settled inventory
        /// (objectives are re-derived from facts + inventory, never stored).</summary>
        public const int Quests = 25;

        /// <summary>Ability book: the per-stance slot layout, the active stance, and the augments and
        /// ornaments seated on the abilities. The learned set itself is not stored — it follows from the
        /// passive-tree allocation restored earlier.</summary>
        public const int Abilities = 30;

        public const int Stance = 40;

        /// <summary>HP/Mana/Barrier — strictly after everything that shapes the maximums.</summary>
        public const int Vitals = 50;

        /// <summary>NPC world deltas (corpses, undead timers).</summary>
        public const int Npc = 60;

        /// <summary>Spawn point population: after the restored bodies claimed their population slots.</summary>
        public const int SpawnPoints = 65;

        public const int PlayerPlacement = 70;
    }
}
