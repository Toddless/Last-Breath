namespace Core.Items.Grants
{
    using System;
    using Interfaces.Entity;
    using Interfaces.Items;
    using Interfaces.Skills;

    public class PassiveSkillGrant(string id, string skillId, Func<ISkillProvider?> providerAccessor) : IItemGrant
    {
        private ISkill? _skill;

        public string Id => id;

        public void Attach(IFightable owner)
        {
            _skill = providerAccessor()?.CreateSkill(skillId);
            if (_skill == null) return;
            owner.PassiveSkills.AddSkill(_skill);
        }

        public void Detach(IFightable owner)
        {
            if (_skill == null) return;
            owner.PassiveSkills.RemoveSkill(_skill.Id);
            _skill = null;
        }

        public IItemGrant Copy() => new PassiveSkillGrant(id, skillId, providerAccessor);
    }
}
