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

        public const int Mastery = 10;

        /// <summary>Inventory + equipment: the parameter modifier sources.</summary>
        public const int Items = 20;

        /// <summary>Quest log: after Items so restored states sit on the settled inventory
        /// (objectives are re-derived from facts + inventory, never stored).</summary>
        public const int Quests = 25;

        /// <summary>Ability book, learned abilities and chosen upgrades.</summary>
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
