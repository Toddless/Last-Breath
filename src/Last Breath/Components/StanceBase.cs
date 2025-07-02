namespace Playground.Components
{
    using Godot;
    using System;
    using Playground.Script;
    using System.Collections.Generic;
    using Playground.Components.Interfaces;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.BattleSystem;

    public abstract class StanceBase : IStance
    {
        private ICharacter _owner;
        private IResource _resource;
        private RandomNumberGenerator _rnd;
        private Dictionary<ICharacter, AttackContext?> _attackedTargets = [];
        private Queue<AttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;
        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        public IResource Resource
        {
            get => _resource;
            private set => _resource = value;
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

        protected StanceBase(ICharacter owner, IResource resource)
        {
            _owner = owner;
            _rnd = new();
            _resource = resource;
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
            context.OnAttackResult += OnReceiveAttackResult;
            CacheAttackedTarget(context, target);
            CombatScheduler.Instance?.Schedule(context);
            //  target.OnReceiveAttack(context);
            // Player has no stance yet.
            GD.Print($"{GetName()} Created attack context on: {target.GetType().Name}");
        }

        private void CacheAttackedTarget(AttackContext context, ICharacter target)
        {
            if (_attackedTargets.TryGetValue(target, out var cont))
            {
                _attackedTargets[target] = context;
            }
            else
            {
                _attackedTargets.Add(target, context);
            }
        }

        public virtual void OnReceiveAttack(AttackContext context)
        {
            if (CanProceed) HandleReceivedAttack(context);
            else _attackQueue.Enqueue(context);
        }

        protected virtual void HandleReceivedAttack(AttackContext context)
        {
            CanProceed = false;
            // This is just a basic example of how this implementation could work.

            // TODO: Own method
            if (IsEvade())
            {
                // TODO: Some skills can ignore evasion.
                context.PassiveSkills.Clear();
                context.SetAttackResult(new AttackResult([], Playground.Script.Enums.AttackResults.Evaded, _owner));
                // TODO: decide counter attack
                CanProceed = true;
                PerformActionWhenEvade(context);
                GD.Print($"{GetName()} evaded attack");
                return;
            }

            // TODO: Own method
            var reducedByArmorDamage = Calculations.DamageAfterArmor(context.FinalDamage, Owner);
            var damageLeftAfterBarrierabsorption = Owner.Defense.BarrierAbsorbDamage(reducedByArmorDamage);

            if (damageLeftAfterBarrierabsorption > 0)
            {
                Owner.Health.TakeDamage(damageLeftAfterBarrierabsorption);
                GD.Print($"{GetName()} taked damage: {damageLeftAfterBarrierabsorption}");
            }

            context.SetAttackResult(new AttackResult([], Playground.Script.Enums.AttackResults.Succeed, _owner));
            PerformActionWhenAttackReceived(context);
            CanProceed = true;
        }

        private string GetName() => _owner.GetType().Name;

        protected virtual void OnReceiveAttackResult(AttackResult result)
        {
            // TODO: Queue 
            // i dont need this context anymore, unsubscribe
            if (_attackedTargets.TryGetValue(result.Target, out var context) && context != null)
            {
                context.OnAttackResult -= OnReceiveAttackResult;
                _attackedTargets[result.Target] = null;
            }

            // Handle skills, we getting from attacked target
            HandleSkills(result.PassiveSkills);

            // different reactions to attack results
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
            GD.Print($"Attack on: {result.Target.GetType().Name} was: {result.Result}");
            PendingAttacks--;
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

        protected virtual void HandleAttackResult()
        {

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

        protected float CalculateCriticalDamage(float baseDmg)
        {
            baseDmg = Calculations.DamageAfterCrit(baseDmg, Owner);
            return baseDmg;
        }
    }
}
