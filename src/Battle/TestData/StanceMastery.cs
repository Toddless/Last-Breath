namespace Battle.TestData
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class StanceMastery(IEntity owner) : IStanceMastery
    {
        private readonly IEntity _owner = owner;

        private const float MaxLevel = 50;
        // Влияет на все способности стойки.
        // Увеличивает базовый урон способности, шансы (критический, накладывания эффекта и т.п)
        //

        public string Id { get; } = "Stance_Mastery";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = [];
        public Texture2D? Icon { get; }
        public int BonusLevel { get; } = 0;
        public int CurrentExperience { get; } = 0;
        public int CurrentLevel { get; } = 1;
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public event Action<int>? BonusLevelChange, CurrentLevelChange, ExperienceChange;

        public float ScaleAbilityParameter(float baseValue) => baseValue * GetProgressFactor();


        public bool HasTag(string tag) => throw new NotImplementedException();

        public void AddExperience(int experience) => throw new NotImplementedException();

        public int ExpToNextLevelRemain() => throw new NotImplementedException();

        private float GetProgressFactor() =>
            Mathf.Clamp((CurrentLevel + BonusLevel - 1) / (MaxLevel + BonusLevel - 1), 0f, 1f) + 1;
    }
}
