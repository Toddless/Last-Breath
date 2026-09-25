namespace Battle.Source
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;

    public abstract class StanceBase(IFightable owner, IStanceActivationEffect effect, Stance stanceType) : IStance
    {
        protected List<ISkill> _obtainedPassiveSkills = [];
        private bool _passivesAttached;

        protected IStanceActivationEffect ActivationEffect { get; } = effect;
        protected IFightable Owner { get; } = owner;

        public int CurrentLevel { get; }
        public Stance StanceType { get; } = stanceType;

        public IReadOnlyList<ISkill> ObtainedPassiveSkills => _obtainedPassiveSkills;

        public virtual void OnActivate()
        {
            ActivationEffect.ApplyModifiers(Owner);
            Owner.PassiveSkills.SuppressionChanged += OnSuppressionChanged;
            if (!Owner.PassiveSkills.IsSuppressed) AttachPassives();
        }

        public virtual void OnDeactivate()
        {
            Owner.PassiveSkills.SuppressionChanged -= OnSuppressionChanged;
            DetachPassives();
            ActivationEffect.RemoveModifiers(Owner);
        }

        private void OnSuppressionChanged(bool suppressed)
        {
            if (suppressed) DetachPassives();
            else AttachPassives();
        }

        private void AttachPassives()
        {
            if (_passivesAttached) return;
            _passivesAttached = true;
            _obtainedPassiveSkills.ForEach(skill => skill.Attach(Owner));
            ActivationEffect.AttachPassives(Owner);
        }

        private void DetachPassives()
        {
            if (!_passivesAttached) return;
            _passivesAttached = false;
            _obtainedPassiveSkills.ForEach(skill => skill.Detach(Owner));
            ActivationEffect.DetachPassives(Owner);
        }
    }
}
