namespace Core.Items.Grants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Battle.Skills;
    using Entity;

    public class PassiveSkillGrant(
        string id,
        string skillId,
        IReadOnlyDictionary<string, float> properties,
        Func<ISkillProvider?> providerAccessor) : IItemGrant
    {
        private ISkill? _skill;

        public string Id => id;

        public string Description
        {
            get
            {
                field = providerAccessor()?.CreateSkill(SkillId, new RecordProperties(SkillId, Properties))?.Description ?? string.Empty;
                return field;
            }
        }

        /// <summary>Read access for serialization (save system round-trips the grant).</summary>
        public string SkillId => skillId;

        /// <summary>Numeric skill parameters from item JSON; read access for serialization.</summary>
        public IReadOnlyDictionary<string, float> Properties => properties;

        public void Attach(IFightable owner)
        {
            _skill = providerAccessor()?.CreateSkill(skillId, new RecordProperties(skillId, properties));
            if (_skill == null) return;
            owner.PassiveSkills.AddSkill(_skill);
        }

        public void Detach(IFightable owner)
        {
            if (_skill == null) return;
            owner.PassiveSkills.RemoveSkill(_skill);
            _skill = null;
        }

        public IItemGrant Copy() => new PassiveSkillGrant(id, skillId, properties, providerAccessor);

        // Properties are the numeric balance payload the skill provider consumes (RecordProperties):
        // scaling every float here is exactly "the granted effect gets +15%". Keys stay untouched.
        public IItemGrant WithScaledValues(float factor) =>
            new PassiveSkillGrant(id, skillId,
                properties.ToDictionary(property => property.Key, property => property.Value * factor),
                providerAccessor);
    }
}
