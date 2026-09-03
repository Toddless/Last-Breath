namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using Core.Interfaces;
    using Core.Narrative.Influence;
    using Godot;

    /// <summary>
    /// The game's own Influence mastery with one thing taken out of its hands: how the invisible rolls
    /// come out. The chance a check is really worth is still the game's curve — the panel shows that
    /// number — and the switch decides only whether the roll passes, so an author reads both the odds
    /// of a line and the branch he asked to see.
    /// </summary>
    /// <remarks>The mastery's own notifications are not forwarded: nothing in a dry run listens, and a
    /// level typed into the panel is not a level up.</remarks>
    public sealed class SandboxInfluence(SandboxWorldState state, InfluenceMastery inner) : IInfluenceMastery
    {
        /// <summary>What a forced roll is worth. A chance of one always passes and a chance of zero
        /// never does, because the game compares a draw of [0..1) against it.</summary>
        private const float Certain = 1f;

        private const float Impossible = 0f;

        public string Id => inner.Id;

        public string InstanceId => inner.InstanceId;

        public Texture2D? Icon => null;

        /// <summary>The id itself: the wording lives in the .po files the engine loads, and a tool
        /// running outside it has no translation server to ask.</summary>
        public string Description => inner.Id;

        public string DisplayName => inner.Id;

        public int BonusLevel => inner.BonusLevel;

        public int CurrentExperience => inner.CurrentExperience;

        public int CurrentLevel => inner.CurrentLevel;

        public int MaximumLevel => inner.MaximumLevel;

        public int SpeechCheckExp => inner.SpeechCheckExp;

        public int FirstTalkExp => inner.FirstTalkExp;

        public event Action<int>? BonusLevelChange { add { } remove { } }

        public event Action<int>? ExperienceChange { add { } remove { } }

        public event Action<int>? CurrentLevelChange { add { } remove { } }

        /// <summary>What the check is really worth at this level, whatever the switch does to the roll.</summary>
        public float TrueSpeechCheckChance(int difficulty) => inner.GetSpeechCheckChance(difficulty);

        public float GetSpeechCheckChance(int difficulty, float bonus = 0) =>
            Forced() ?? inner.GetSpeechCheckChance(difficulty, bonus);

        public float GetQuestOfferChance(int tier, float bonus = 0) =>
            Forced() ?? inner.GetQuestOfferChance(tier, bonus);

        public void AddExperience(int experience) => inner.AddExperience(experience);

        public void AddBonusLevel() => inner.AddBonusLevel();

        public void RemoveBonusLevel() => inner.RemoveBonusLevel();

        public int ExpToNextLevelRemain() => inner.ExpToNextLevelRemain();

        public int ExpToNextLevelTotal() => inner.ExpToNextLevelTotal();

        public void RestoreState(int baseLevel, int experience) => inner.RestoreState(baseLevel, experience);

        public bool IsSame(string otherId) => inner.IsSame(otherId);

        /// <summary>The chance the switch dictates, or nothing while the rolls are left to chance.</summary>
        private float? Forced() => state.Rolls switch
        {
            SandboxRolls.AlwaysPass => Certain,
            SandboxRolls.AlwaysFail => Impossible,
            _ => null,
        };
    }
}
