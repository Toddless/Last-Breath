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

        /// <summary>Load-time restore: writes the BASE level (without BonusLevel) and experience
        /// directly, bypassing AddExperience — no level-up notifications fire. BonusLevel is not
        /// touched: it re-accumulates when equipment grants re-attach during load.</summary>
        void RestoreState(int baseLevel, int experience);
    }
}
