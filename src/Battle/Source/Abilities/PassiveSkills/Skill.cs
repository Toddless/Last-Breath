namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Godot;
    using Utilities;

    public abstract class Skill(string id) : ISkill
    {
        public string Id { get; } = id;
        public Texture2D? Icon { get; }
        public string Description => Localizator.Localize(Id);
        public string DisplayName => Localizator.LocalizeDescription(Id);

        public abstract void Attach(IEntity owner);

        public abstract void Detach(IEntity owner);

        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
