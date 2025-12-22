namespace Battle.Source.PassiveSkills
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public abstract class Skill(string id) : ISkill
    {
        protected IEntity? Owner;
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon { get; }
        public string Description => Localizator.Localize(Id);
        public string DisplayName => Localizator.LocalizeDescription(Id);


        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public abstract void Attach(IEntity owner);
        public abstract void Detach(IEntity owner);
        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
