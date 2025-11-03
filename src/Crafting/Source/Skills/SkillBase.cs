namespace Crafting.TestResources
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Entity;

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
