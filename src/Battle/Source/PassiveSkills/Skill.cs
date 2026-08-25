namespace Battle.Source.PassiveSkills
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Localization;
    using Godot;

    public abstract class Skill(string id) : ISkill
    {
        protected IFightable? Owner;
        /// <summary>Named values for the description template ({Chance}, {Threshold}...); null = static text.</summary>
        protected virtual IReadOnlyDictionary<string, object?>? DescriptionValues => null;
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon { get; }
        public string DisplayName => Localization.Localize(Id);

        /// <summary>The catalog's rule text, with the passive's own numbers rendered into it. Virtual for
        /// the one family that has no catalog entry to render into: a passive built from a record's fields
        /// is nothing but its lines, and says so by wording them (<see cref="StatPassiveSkill"/>).</summary>
        public virtual string Description =>
            DescriptionValues is { } values
                ? Localization.RenderDescription(Id, values, TextFormat.Rich)
                : Localization.LocalizeDescription(Id);
        
        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public abstract void Attach(IFightable owner);
        public abstract void Detach(IFightable owner);
        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
