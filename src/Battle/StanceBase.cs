namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public abstract class StanceBase(IEntity owner, IStanceActivationEffect effect, Stance stanceType, List<IAbility> abilities) : IStance
    {
        protected List<ISkill> _obtainedPassiveSkills = [];
        protected List<IAbility> _obtainedAbilities = abilities;

        protected IStanceActivationEffect ActivationEffect { get; } = effect;
        protected IEntity Owner { get; } = owner;

        public int CurrentLevel { get; }
        public Stance StanceType { get; } = stanceType;

        public IReadOnlyList<ISkill> ObtainedPassiveSkills => _obtainedPassiveSkills;
        public IReadOnlyList<IAbility> ObtainedAbilities => _obtainedAbilities;

        public virtual void OnActivate()
        {
            _obtainedPassiveSkills.ForEach(skill => skill.Attach(Owner));
            _obtainedAbilities.ForEach(ability => ability.SetOwner(Owner));
            ActivationEffect.OnActivate(Owner);
        }

        public virtual void OnDeactivate()
        {
            _obtainedPassiveSkills.ForEach(skill => skill.Detach(Owner));
            _obtainedAbilities.ForEach(ability => ability.RemoveOwner());
            ActivationEffect.OnDeactivate(Owner);
        }
    }
}
