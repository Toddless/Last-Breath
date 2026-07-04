namespace Core.Interfaces
{
    using System;

    public interface IMastery : IIdentifiable, IDisplayable
    {
        int BonusLevel { get; }
        int CurrentExperience { get; }
        int CurrentLevel { get; }
        int MaximumLevel { get; }

        event Action<int>? BonusLevelChange, ExperienceChange, CurrentLevelChange;

        void AddExperience(int experience);
        void AddBonusLevel();
        void RemoveBonusLevel();
        int ExpToNextLevelRemain();

        /// <summary>Full cost of the next level (for progress bars); 0 at the level cap.</summary>
        int ExpToNextLevelTotal();
    }
}
