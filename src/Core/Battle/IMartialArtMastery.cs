namespace Core.Battle
{
    using Interfaces;

    public interface IMartialArtMastery : IMastery
    {
        /// <summary>Load-time restore: writes the BASE level (without BonusLevel) and experience
        /// directly, bypassing AddExperience — no level-up notifications fire. BonusLevel is not
        /// touched: it re-accumulates when equipment grants re-attach during load.</summary>
        void RestoreState(int baseLevel, int experience);
    }
}
