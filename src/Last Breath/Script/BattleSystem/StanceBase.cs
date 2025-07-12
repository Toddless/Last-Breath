namespace Playground.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class StanceBase : IStance
    {
        private Queue<AttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;

        private Dictionary<ModuleParameter, IModule> _modules { get; set; } = [];
        protected bool CanProceed
        {
            get => _canProceed;
            set
            {
                _canProceed = value;
                if (_canProceed)
                    FlushQueue();
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
        protected IStanceActivationEffect ActivationEffect { get; }
        protected ICharacter Owner { get; }
        protected IModule this[ModuleParameter parameter] => _modules[parameter];
        protected ModuleDecoratorManager DecoratorManager { get; }
        public IResource Resource { get; }

        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        protected StanceBase(ICharacter owner, IResource resource, IStanceActivationEffect effect)
        {
            Owner = owner;
            Resource = resource;
            ActivationEffect = effect;
            DecoratorManager = new(Owner);
        }

        public virtual void OnActivate()
        {
            SetModules();
            SubscribeEvents();
        }

        public virtual void OnDeactivate()
        {
            UnSubscribeEvents();
        }

        public virtual void OnAttack(ICharacter target)
        {
            if (!CanAttack(target)) return;
            PendingAttacks++;

            AttackContext context = new(Owner, target)
            {
                BaseDamage = this[ModuleParameter.Damage].GetValue(),
                IsCritical = IsCrit(),
                CriticalDamageMultiplier = this[ModuleParameter.CritDamage].GetValue(),
            };

            // create list with skills applied on attack
            ApplySkillsOnAttack(context);

            // why subscribe?
            // I need the result to decide what to do next
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

        protected bool CanAttack(ICharacter target) => target.IsAlive && Owner.IsAlive;

        protected virtual void HandleReceivedAttack(AttackContext context)
        {
            CanProceed = false;
            if (IsEvade())
            {
                HandleEvade(context);
                CanProceed = true;
                return;
            }

            float damageLeft = GetDamageLeftAfterBarrierAbsorbation(context);

            if (damageLeft > 0)
            {
                Owner.TakeDamage(damageLeft);
                GD.Print($"{GetName()} taked damage: {damageLeft}");
            }

            context.SetAttackResult(new AttackResult([], AttackResults.Succeed, Owner, context));
            PerformActionWhenAttackReceived(context);
            CanProceed = true;
        }

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

        protected virtual void ApplySkillsOnAttack(AttackContext context)
        {
            List<ISkill> onAttackSkills = [];
            context.PassiveSkills = onAttackSkills;
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

        protected virtual void HandleAttackSucceed(ICharacter target) => Resource.Recover();

        protected bool IsCrit() => this[ModuleParameter.CritChance].GetValue() <= Owner.Damage.CriticalChance;

        protected bool IsEvade() => this[ModuleParameter.EvadeChance].GetValue() <= Owner.Defense.Evade;

        protected virtual void SubscribeEvents()
        {
            Resource.CurrentChanges += OnCurrentResourceChanges;
            Resource.MaximumChanges += OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged += Resource.OnParameterChanges;
            DecoratorManager.ModuleDecoratorChanges += OnModuleDecoratorChanges;
        }

        private void SetModules()
        {
            _modules = Enum.GetValues<ModuleParameter>()
                .ToDictionary(
                param => param,
                DecoratorManager.GetModule);
        }

        private void UnSubscribeEvents()
        {
            Resource.CurrentChanges -= OnCurrentResourceChanges;
            Resource.MaximumChanges -= OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged -= Resource.OnParameterChanges;
            DecoratorManager.ModuleDecoratorChanges -= OnModuleDecoratorChanges;

            foreach (var attack in _attackQueue)
            {
                attack.OnAttackResult -= OnAttackResult;
                attack.OnAttackCanceled -= OnAttackCanceled;
            }
        }

        private void OnModuleDecoratorChanges(ModuleParameter parameter, IModule module) => _modules[parameter] = module;
        private void OnMaximumResourceChanges(float value) => MaximumResourceChanges?.Invoke(value);
        private void OnCurrentResourceChanges(float value) => CurrentResourceChanges?.Invoke(value);

        private void HandleEvade(AttackContext context)
        {
            context.PassiveSkills.Clear();
            context.SetAttackResult(new AttackResult([], AttackResults.Evaded, Owner, context));
            PerformActionWhenEvade(context);
            GD.Print($"{GetName()} evaded attack");
        }

        // TODO: Remove this from here 
        private float GetDamageLeftAfterBarrierAbsorbation(AttackContext context)
        {
            var reducedByArmorDamage = Calculations.DamageAfterArmor(context, Owner);
            // Move this to ICHaracter method instead DefenceComponent method??
            var damageLeftAfterBarrierabsorption = Owner.Defense.BarrierAbsorbDamage(reducedByArmorDamage);
            return damageLeftAfterBarrierabsorption;
        }

        private void HandleResult(AttackResult result)
        {
            if (!result.Target.IsAlive)
                return;

            switch (result.Result)
            {
                case AttackResults.Evaded:
                    HandleAttackEvaded(result.Target);
                    break;
                case AttackResults.Blocked:
                    HandleAttackBlocked(result.Target);
                    break;
                case AttackResults.Succeed:
                    HandleAttackSucceed(result.Target);
                    break;
                default:
                    break;
            }
        }

        private void FlushQueue()
        {
            while (CanProceed && _attackQueue.Count > 0)
                HandleReceivedAttack(_attackQueue.Dequeue());
            CheckIfAllAttacksHandled();
        }

        private string GetName() => Owner.GetType().Name;

        private void OnAttackCanceled(AttackContext context)
        {
            context.OnAttackResult -= OnAttackResult;
            context.OnAttackCanceled -= OnAttackCanceled;
            PendingAttacks--;
        }

        private void Unsubscribe(AttackResult result)
        {
            result.Context.OnAttackResult -= OnAttackResult;
            result.Context.OnAttackCanceled -= OnAttackCanceled;
        }

        private void CheckIfAllAttacksHandled()
        {
            if (CanProceed && PendingAttacks == 0 && _attackQueue.Count == 0)
                Owner.AllAttacks();
        }
    }
}
