namespace Crafting.Source.Skills
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Godot;
    using Utilities;

    public abstract partial class SkillBase : Resource, ISkill
    {
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; private set; }
        [Export] public SkillType Type { get; private set; }

        public string Description => GetDescription();
        public string DisplayName => Localizator.Localize(Id);

        public abstract void Attach(IEntity owner);
        public virtual ISkill? Copy() => (ISkill)DuplicateDeep();
        public abstract void Detach();

        protected abstract string GetDescription();
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
