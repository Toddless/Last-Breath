namespace Playground.Components
{
    using System;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;

    public class SkillsComponent
    {
        private readonly ICharacter _owner;

        public event Action<ISkill>? AlreadyExist;

        public SkillsComponent(ICharacter owner)
        {
            _owner = owner;
            // должен реагировать в зависимости от SkillType
        }

        public void AddSkill(ISkill skill)
        {
           
        }

        public void RemoveSkill(ISkill skill)
        {

        }
    }
}
