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
        /// <summary>The empty bag a valueless passive renders through — the text still has keywords in it.</summary>
        private static readonly Dictionary<string, object?> s_noValues = [];

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
        public virtual string Description => Describe(TextFormat.Rich);

        /// <summary>The same sentence in the format the reader can show — the card takes it tinted and
        /// clickable, a plain-text reader takes the words alone. One rendering either way: the format is
        /// the only thing that differs, so the two can never word the same numbers differently.
        /// <para>A passive holding no values still goes through the engine: its text may name a KEYWORD
        /// and nothing else ("{@Lucky}"), and a rule read out unrendered would print that token at the
        /// player. An unknown token is kept verbatim by the engine either way, so nothing is lost by
        /// asking.</para></summary>
        public virtual string Describe(TextFormat format) =>
            Localization.RenderDescription(Id, DescriptionValues ?? s_noValues, format);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public abstract void Attach(IFightable owner);
        public abstract void Detach(IFightable owner);
        public abstract ISkill Copy();
        public abstract bool IsStronger(ISkill skill);
    }
}
