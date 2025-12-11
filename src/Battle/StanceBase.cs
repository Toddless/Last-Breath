namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public abstract class StanceBase(IEntity owner, IStanceActivationEffect effect, Stance stanceType) : IStance
    {
        private List<ISkill> _obtainedPassiveSkills = [];
        private List<IAbility> _obtainedAbilities = [];

        protected IStanceActivationEffect ActivationEffect { get; } = effect;
        protected IEntity Owner { get; } = owner;

        public int CurrentLevel { get; }
        public Stance StanceType { get; } = stanceType;

        public IReadOnlyList<ISkill> ObtainedPassiveSkills => _obtainedPassiveSkills;
        public IReadOnlyList<IAbility> ObtainedAbilities => _obtainedAbilities;

        public virtual void OnActivate()
        {
            // I have to reset the modules so that I have the latest updates.
            _obtainedPassiveSkills.ForEach(skill => skill.Attach(Owner));
            ActivationEffect.OnActivate(Owner);
        }

        public virtual void OnDeactivate()
        {
            _obtainedPassiveSkills.ForEach(skill => skill.Detach(Owner));
            ActivationEffect.OnDeactivate(Owner);
        }
    }
}
