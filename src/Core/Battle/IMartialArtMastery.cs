namespace Core.Battle
{
    using Interfaces;

    public interface IMartialArtMastery : IMastery
    {
        /// <summary>Levels the character EARNED by fighting, without the ones equipment grants. What a
        /// price scaled by progress reads: <c>CurrentLevel</c> moves when a ring is put on and taken off
        /// again, and a cost that followed it could be bought at whatever the cheapest gear of the
        /// moment says.</summary>
        int EarnedLevel { get; }

        /// <summary>Passive tree points granted beside the curve — quest rewards. They live here
        /// rather than in the tree because the tree keeps no budget of its own: it is TOLD a total,
        /// so the only place a granted point can survive a save is the mastery that states it.</summary>
        int BonusPoints { get; }

        /// <summary>The whole passive tree budget the character was granted: one point per earned
        /// level plus <see cref="BonusPoints"/>. Exactly the number the tree is told.</summary>
        int TotalPoints { get; }

        /// <summary>Grants tree points beside the curve and states the grown total to the tree at
        /// once. Deliberately not idempotent, like every narrative reward: a quest that must pay
        /// once is held to it by being unrepeatable, not by the grant refusing a second call.</summary>
        void AddBonusPoints(int points);

        /// <summary>Load-time restore: writes the BASE level (without BonusLevel), experience and the
        /// granted point bonus directly, bypassing AddExperience — no level-up notifications fire.
        /// BonusLevel is not touched: it re-accumulates when equipment grants re-attach during load.
        /// The whole granted budget is stated in one call, so a section written before quests paid in
        /// points restores no bonus instead of keeping whatever the session held — which is also why
        /// the bonus has no default: a restore road that forgets it would zero it in silence.</summary>
        void RestoreState(int baseLevel, int experience, int bonusPoints);
    }
}
