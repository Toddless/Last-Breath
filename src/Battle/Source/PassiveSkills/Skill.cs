namespace Battle.Source.PassiveSkills
{
    using System;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Godot;
    using Utilities;

    public abstract class Skill(string id) : ISkill
    {
        protected IFightable? Owner;
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon { get; }
        public string Description => Localization.Localize(Id);
        public string DisplayName => Localization.LocalizeDescription(Id);


        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public abstract void Attach(IFightable owner);
        public abstract void Detach(IFightable owner);
        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
