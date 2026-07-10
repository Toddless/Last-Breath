namespace Core.Narrative.Influence
{
    using Interfaces;

    /// <summary>
    /// The world-interaction mastery: how skillfully the player wields whatever the world thinks
    /// of him. Reputation is the standing, Influence is the leverage — both meet in the chance
    /// formulas via the bonus parameter (reputation perks plug in there).
    /// </summary>
    public interface IInfluenceMastery : IMastery
    {
        /// <summary>Chance (0..1) to pass a speech check of the given difficulty (difficulty is
        /// expressed in mastery levels). Rolling is the caller's job; the chance is NOT shown to
        /// the player by design.</summary>
        float GetSpeechCheckChance(int difficulty, float bonus = 0);

        /// <summary>Chance (0..1) that an NPC brings up a quest of the given tier at all.
        /// A failed roll is not forever — the offer re-rolls after a world-clock cooldown.</summary>
        float GetQuestOfferChance(int tier, float bonus = 0);

        /// <summary>Exp for a passed speech check (paid once per option per game).</summary>
        int SpeechCheckExp { get; }

        /// <summary>Exp for the first conversation with an NPC kind — talking to everyone pays.</summary>
        int FirstTalkExp { get; }

        /// <summary>Load-time restore: writes the BASE level (without BonusLevel) and experience
        /// directly, bypassing AddExperience — no level-up notifications fire.</summary>
        void RestoreState(int baseLevel, int experience);
    }
}
