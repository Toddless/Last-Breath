namespace Core.Interfaces
{
    using System;

    public interface IMastery : IIdentifiable, IDisplayable
    {
        int BonusLevel { get; }
        int CurrentExperience { get; }
        int CurrentLevel { get; }

        event Action<int>? BonusLevelChange, ExperienceChange, CurrentLevelChange;

        void AddExperience(int experience);
        void AddBonusLevel();
        void RemoveBonusLevel();
        int ExpToNextLevelRemain();
    }
}
