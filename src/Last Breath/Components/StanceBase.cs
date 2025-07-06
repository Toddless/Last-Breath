namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.BattleSystem;

    public abstract class StanceBase : IStance
    {
        private ICharacter _owner;
        private IResource _resource;
        private IStanceActivationEffect _effect;
        private RandomNumberGenerator _rnd;
        private Queue<AttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;
        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        public IResource Resource
        {
            get => _resource;
        }

        protected bool CanProceed
        {
            get => _canProceed;
            set
            {
                _canProceed = value;
                if (_canProceed && _attackQueue.Count > 0)
                {
                    HandleReceivedAttack(_attackQueue.Dequeue());
                }
                CheckIfAllAttacksHandled();
            }
        }

        protected IStanceActivationEffect ActivationEffect => _effect;

        protected int PendingAttacks
        {
            get => _pendingAttacks;
            set
            {
                _pendingAttacks = value;
                CheckIfAllAttacksHandled();
            }
        }

        private void CheckIfAllAttacksHandled()
        {
            if (CanProceed && PendingAttacks == 0 && _attackQueue.Count == 0)
            {
                Owner.AllAttacks();
                GD.Print("Checking attacks end");
            }
        }

        protected ICharacter Owner { get => _owner; }
        protected RandomNumberGenerator Rnd { get => _rnd; }

        protected StanceBase(ICharacter owner, IResource resource, IStanceActivationEffect effect)
        {
            _owner = owner;
            _rnd = new();
            _resource = resource;
            _effect = effect;
            SetEvents();
        }

        protected virtual void SetEvents()
        {
            Resource.CurrentChanges += (value) => CurrentResourceChanges?.Invoke(value);
            Resource.MaximumChanges += (value) => MaximumResourceChanges?.Invoke(value);
            Owner.Modifiers.ParameterModifiersChanged += Resource.OnParameterChanges;
        }

        public abstract void OnActivate();
        public abstract void OnDeactivate();
        public virtual void OnAttack(ICharacter target)
        {
            if (!CanAttack(target)) return;
            PendingAttacks++;
            AttackContext context = new(Owner, target);

            var baseDamage = Owner.Damage.Damage;
            context.BaseDamage = baseDamage;

            // TODO: own method
            var critical = IsCrit();
            if (critical)
            {
                baseDamage *= Owner.Damage.CriticalDamage;
            }

            // some modifiers (e.g increase damage if target is an "undead" etc.)
            context.FinalDamage = baseDamage;

            // TODO: own method
            // create list with skills applied on attack
            List<ISkill> onAttackSkills = [];
            context.PassiveSkills = onAttackSkills;

            // why subscribe?
            // I need the results to help me decide what to do next
            context.OnAttackResult += OnAttackResult;
            context.OnAttackCanceled += OnAttackCanceled;
            CombatScheduler.Instance?.Schedule(context);
            GD.Print($"{GetName()} Created attack context on: {target.GetType().Name}");
        }

      
        public virtual void OnReceiveAttack(AttackContext context)
        {
            if (CanProceed) HandleReceivedAttack(context);
            else
            {
                GD.Print("Added to attack queue");
                _attackQueue.Enqueue(context);
            }
        }

        protected bool CanAttack(ICharacter target) => target.IsAlive || _owner.IsAlive;

        protected virtual void HandleReceivedAttack(AttackContext context)
        {
            CanProceed = false;
            // TODO: Own method
            if (IsEvade())
            {
                HandleEvade(context);
                CanProceed = true;
                return;
            }

            float damageLeft = GetDamageLeftAfterBarrierAbsorbation(context);

            if (damageLeft > 0)
            {
                Owner.Health.TakeDamage(damageLeft);
                GD.Print($"{GetName()} taked damage: {damageLeft}");
            }

            context.SetAttackResult(new AttackResult([], Playground.Script.Enums.AttackResults.Succeed, _owner, context));
            PerformActionWhenAttackReceived(context);
            CanProceed = true;
            GD.Print($"Attacks in queue: {_attackQueue.Count}");
        }

        private void HandleEvade(AttackContext context)
        {
            context.PassiveSkills.Clear();
            context.SetAttackResult(new AttackResult([], Playground.Script.Enums.AttackResults.Evaded, _owner, context));
            PerformActionWhenEvade(context);
            GD.Print($"{GetName()} evaded attack");
        }

        private float GetDamageLeftAfterBarrierAbsorbation(AttackContext context)
        {
            var reducedByArmorDamage = Calculations.DamageAfterArmor(context.FinalDamage, Owner);
            var damageLeftAfterBarrierabsorption = Owner.Defense.BarrierAbsorbDamage(reducedByArmorDamage);
            return damageLeftAfterBarrierabsorption;
        }

        private string GetName() => _owner.GetType().Name;

        protected virtual void OnAttackResult(AttackResult result)
        {
            // i dont need this context anymore, unsubscribe
            Unsubscribe(result);
            // Handle skills, we getting from attacked target
            HandleSkills(result.PassiveSkills);
            // different reactions to attack results
            HandleResult(result);
            PendingAttacks--;
        }

        private void HandleResult(AttackResult result)
        {
            if (!result.Target.IsAlive)
            {
                GD.Print("Target is dead.");
                return;
            }

            switch (result.Result)
            {
                case Playground.Script.Enums.AttackResults.Evaded:
                    HandleAttackEvaded(result.Target);
                    break;
                case Playground.Script.Enums.AttackResults.Blocked:
                    HandleAttackBlocked(result.Target);
                    break;
                case Playground.Script.Enums.AttackResults.Succeed:
                    HandleAttackSucceed(result.Target);
                    break;
                default:
                    break;
            }
        }

        private void OnAttackCanceled(AttackContext context)
        {
            GD.Print("Unsub from canceled attack");
            context.OnAttackResult -= OnAttackResult;
            context.OnAttackCanceled -= OnAttackCanceled;
            PendingAttacks--;
        }


        private void Unsubscribe(AttackResult result)
        {
            result.Context.OnAttackResult -= OnAttackResult;
            result.Context.OnAttackCanceled -= OnAttackCanceled;
        }

        protected virtual void PerformActionWhenAttackReceived(AttackContext context)
        {

        }

        protected virtual void PerformActionWhenEvade(AttackContext context)
        {

        }

        protected virtual void HandleSkills(List<ISkill> passiveSkills)
        {

        }

        protected virtual void HandleAttackBlocked(ICharacter target)
        {

        }

        protected virtual void HandleAttackEvaded(ICharacter target)
        {

        }

        protected virtual void HandleAttackSucceed(ICharacter target)
        {
            _resource.Recover();
        }

        protected bool IsCrit()
        {
            return Owner.Damage.IsCrit();
        }

        protected bool AdditionalAttack()
        {
            return Rnd.Randf() <= Owner.Damage.AdditionalHit;
        }

        protected bool IsEvade()
        {
            return Rnd.Randf() <= Owner.Defense.Evade;
        }
    }
}
