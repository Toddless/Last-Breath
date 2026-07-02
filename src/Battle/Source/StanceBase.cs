namespace Battle.Source
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public abstract class StanceBase(IFightable owner, IStanceActivationEffect effect, Stance stanceType) : IStance
    {
        protected List<ISkill> _obtainedPassiveSkills = [];

        protected IStanceActivationEffect ActivationEffect { get; } = effect;
        protected IFightable Owner { get; } = owner;

        public int CurrentLevel { get; }
        public Stance StanceType { get; } = stanceType;

        public IReadOnlyList<ISkill> ObtainedPassiveSkills => _obtainedPassiveSkills;

        public virtual void OnActivate()
        {
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
