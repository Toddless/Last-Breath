namespace Core.Items.Grants
{
    using System;
    using System.Collections.Generic;
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

        /// <summary>Read access for serialization (save system round-trips the grant).</summary>
        public string SkillId => skillId;

        /// <summary>Numeric skill parameters from item JSON; read access for serialization.</summary>
        public IReadOnlyDictionary<string, float> Properties => properties;

        public void Attach(IFightable owner)
        {
            _skill = providerAccessor()?.CreateSkill(skillId, new SkillProperties(skillId, properties));
            if (_skill == null) return;
            owner.PassiveSkills.AddSkill(_skill);
        }

        public void Detach(IFightable owner)
        {
            if (_skill == null) return;
            owner.PassiveSkills.RemoveSkill(_skill.Id);
            _skill = null;
        }

        public IItemGrant Copy() => new PassiveSkillGrant(id, skillId, properties, providerAccessor);
    }
}
