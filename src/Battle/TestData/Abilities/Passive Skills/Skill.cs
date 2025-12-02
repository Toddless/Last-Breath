namespace Battle.TestData.Abilities.Passive_Skills
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Entity;

    public abstract class Skill(string id, SkillType type) : ISkill
    {
        protected IEntity? Owner;
        public string Id { get; } = id;
        public Texture2D? Icon { get; }
        public string Description => Localizator.Localize(Id);
        public string DisplayName => Localizator.LocalizeDescription(Id);
        public SkillType Type { get; } = type;

        public abstract void Attach(IEntity owner);

        public abstract void Detach(IEntity owner);

        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
