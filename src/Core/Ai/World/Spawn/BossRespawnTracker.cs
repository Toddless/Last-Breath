namespace Core.Ai.World.Spawn
{
    using Enums;
    using Narrative.Facts;

    /// <summary>
    /// Counting half of the FactionDeaths boss category: combat deaths of the boss's faction feed
    /// the counter; a living boss FREEZES it (deaths while he walks are not banked). The counter
    /// lives in WorldFacts, so it persists with the narrative save for free. The final-death fact
    /// is deliberately ignored by this mode — only Single bosses gate on it.
    /// </summary>
    public class BossRespawnTracker(IWorldFactsService facts, string bossId, Fractions faction, int deathsPerRespawn)
    {
        /// <summary>Kept by the owning spawn point: true while its boss instance is alive.</summary>
        public bool BossAlive { get; set; }

        public bool IsRespawnDue => !BossAlive && facts.GetCount(FactKeys.BossRespawnDeaths(bossId)) >= deathsPerRespawn;

        /// <summary>A combat death somewhere in the world; summons never feed the counter.</summary>
        public void OnDeath(Fractions deadFaction, bool isSummon)
        {
            if (BossAlive || isSummon || deadFaction != faction) return;
            facts.Add(FactKeys.BossRespawnDeaths(bossId));
        }

        /// <summary>The respawn happened: the bank starts over.</summary>
        public void ConsumeRespawn() => facts.SetCount(FactKeys.BossRespawnDeaths(bossId), 0);
    }
}
