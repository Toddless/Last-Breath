namespace Battle
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public abstract class StanceBase(IEntity owner, IStanceActivationEffect effect, Stance stanceType)
    {
        protected Dictionary<int, (ISkill Skill, bool Opened)> Skills { get; } = [];
        protected IStanceActivationEffect ActivationEffect { get; } = effect;
        protected IEntity Owner { get; } = owner;

        public IResource Resource { get; } = new ManaResource();
        public Stance StanceType { get; } = stanceType;

        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        public virtual void OnActivate()
        {
            // I have to reset the modules so that I have the latest updates.
            SetModules();
            SubscribeEvents();
            ActivationEffect.OnActivate(Owner);
        }

        public virtual void OnDeactivate()
        {
            UnsubscribeEvents();
            ActivationEffect.OnDeactivate(Owner);
        }

        public virtual void OnAttack(IEntity target)
        {
            if (!CanAttack(target)) return;

            AttackContext context = new(Owner, target)
            {
                Damage = Owner.Parameters.Damage, CriticalDamageMultiplier = Owner.Parameters.CriticalDamage
            };

            // create list with skills applied on attack
            ApplyPreAttackSkills(context);

            // why subscribe?
            // I need the result to decide what to do next
            context.OnAttackResult += OnAttackResult;
            context.OnAttackCanceled += OnAttackCanceled;
        }

        public virtual void OnReceiveAttack(IAttackContext context)
        {
        }

        protected bool CanAttack(IEntity target) => target.IsAlive && Owner.IsAlive;

        protected virtual void HandleReceivedAttack(IAttackContext context)
        {
            context.Armor = Owner.Parameters.Armor;
            context.MaxReduceDamage = Owner.Parameters.MaxReduceDamage;
            Owner.TakeDamage(context.FinalDamage, context.IsCritical);

            HandleOnAttackSkills(context.PassiveSkills);
            context.SetAttackResult(new AttackResult([], AttackResults.Succeed, context));
        }


        protected virtual void OnAttackResult(IAttackResult result)
        {
            // I don't need this context anymore, unsubscribe
            Unsubscribe(result);
            // Handle skills, we may will getting from attacked target
            HandleOnGettingAttackSkills(result.PassiveSkills);
            // different reactions to attack results
            HandleResult(result);
        }

        protected void ApplyPreAttackSkills(IAttackContext context)
        {
        }

        protected virtual void HandleOnAttackSkills(List<ISkill> passiveSkills)
        {
        }

        protected virtual void HandleOnGettingAttackSkills(List<ISkill> passiveSkills)
        {
        }

        protected virtual void SubscribeEvents()
        {
            Resource.CurrentChanges += OnCurrentResourceChanges;
            Resource.MaximumChanges += OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged += Resource.OnParameterChanges;
        }

        private void HandleEvadeReceivedAttack(IAttackContext context)
        {
            Owner.OnEvadeAttack();
            context.SetAttackResult(new AttackResult([], AttackResults.Evaded,
                context));
        }

        private void CheckIfAllAttacksHandled()
        {
        }

        private void HandleResult(IAttackResult result)
        {
            var target = result.Context.Target;
        }

        private void FlushQueue()
        {
        }

        private void OnAttackCanceled(IAttackContext context)
        {
        }

        private void Unsubscribe(IAttackResult result)
        {
        }

        private void UnsubscribeEvents()
        {
        }

        private void OnMaximumResourceChanges(float value) => MaximumResourceChanges?.Invoke(value);
        private void OnCurrentResourceChanges(float value) => CurrentResourceChanges?.Invoke(value);

        private void SetModules()
        {
        }
    }
}
