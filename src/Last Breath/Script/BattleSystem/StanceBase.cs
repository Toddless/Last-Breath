namespace Playground.Script.BattleSystem
{
    using Godot;
    using System;
    using System.Linq;
    using Playground.Script;
    using Playground.Components;
    using Playground.Script.Enums;
    using System.Collections.Generic;
    using Playground.Components.Interfaces;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Abilities.Interfaces;

    public abstract class StanceBase : IStance
    {
        private readonly Queue<AttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;

        private Dictionary<ModuleParameter, IValueModule<float>> _floatModules { get; set; } = [];
        private Dictionary<ActionModuleType, IActionModule<ICharacter>> _characterActionModules { get; set; } = [];

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
        protected IValueModule<float> this[ModuleParameter parameter] => _floatModules[parameter];
        protected IActionModule<ICharacter> this[ActionModuleType type] => _characterActionModules[type];
        protected IStanceActivationEffect ActivationEffect { get; }
        protected ICharacter Owner { get; }

        public IResource Resource { get; }
        public FloatModuleDecoratorManager FloatDecoratorManager { get; }
        public ActionModuleDecoratorManager ActionDecoratorManager { get; }

        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        protected StanceBase(ICharacter owner, IResource resource, IStanceActivationEffect effect)
        {
            Owner = owner;
            Resource = resource;
            ActivationEffect = effect;
            FloatDecoratorManager = new(Owner);
            ActionDecoratorManager = new(Owner);
            SetModules();
        }

        public virtual void OnActivate()
        {
            // TODO: Am i getting updated module befor subscription??
            SubscribeEvents();
            ActivationEffect.OnActivate(Owner);
        }

        public virtual void OnDeactivate()
        {
            UnsubscribeEvents();
            ActivationEffect.OnDeactivate(Owner);
        }

        public virtual void OnAttack(ICharacter target)
        {
            if (!CanAttack(target)) return;
            PendingAttacks++;

            AttackContext context = new(Owner, target)
            {
                Damage = this[ModuleParameter.Damage].GetValue(),
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

        public bool IsChainAttack() => this[ModuleParameter.AdditionalAttackChance].GetValue() <= Owner.Damage.AdditionalHit;

        protected bool CanAttack(ICharacter target) => target.IsAlive && Owner.IsAlive;

        protected virtual void HandleReceivedAttack(AttackContext context)
        {
            CanProceed = false;
            if (!context.IgnoreEvade && IsEvade())
            {
                HandleEvadeReceivedAttack(context);
                CanProceed = true;
                return;
            }

            context.Armor = this[ModuleParameter.Armor].GetValue();
            context.MaxReduceDamage = this[ModuleParameter.MaxReduceDamage].GetValue();
            context.FinalDamage = Calculations.DamageReduceByArmor(context);

            float damageLeft = DamageLeftAfterBarrierAbsorbation(context);

            if (damageLeft > 0)
            {
                Owner.TakeDamage(damageLeft);
                GD.Print($"{GetName()} taked damage: {damageLeft}");
            }

            context.SetAttackResult(new AttackResult([], AttackResults.Succeed, context));
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

        protected virtual void SubscribeEvents()
        {
            Resource.CurrentChanges += OnCurrentResourceChanges;
            Resource.MaximumChanges += OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged += Resource.OnParameterChanges;
            FloatDecoratorManager.ModuleDecoratorChanges += OnFloatModuleDecoratorChanges;
            ActionDecoratorManager.ModuleDecoratorChanges += OnCharacterActionModuleDecoratorChanges;
        }

        protected bool IsCrit() => this[ModuleParameter.CritChance].GetValue() <= Owner.Damage.CriticalChance;
        protected bool IsEvade() => this[ModuleParameter.EvadeChance].GetValue() <= Owner.Defense.Evade;

        private void HandleEvadeReceivedAttack(AttackContext context)
        {
            context.PassiveSkills.Clear();
            context.SetAttackResult(new AttackResult([], AttackResults.Evaded, context));
            GD.Print($"{GetName()} evaded attack");
        }

        private float DamageLeftAfterBarrierAbsorbation(AttackContext context)
        {
            // TODO: module?
            if (context.IgnoreBarrier) return context.FinalDamage;
            return Owner.Defense.BarrierAbsorbDamage(context.FinalDamage);
        }

        private void CheckIfAllAttacksHandled()
        {
            if (CanProceed && PendingAttacks == 0 && _attackQueue.Count == 0)
                Owner.AllAttacks();
        }

        private void HandleResult(AttackResult result)
        {
            var target = result.Context.Target;

            switch (result.Result)
            {
                case AttackResults.Evaded:
                    this[ActionModuleType.EvadeAction].PerformModuleAction(target);
                    break;
                case AttackResults.Blocked:
                    this[ActionModuleType.BlockAction].PerformModuleAction(target);
                    break;
                case AttackResults.Succeed:
                    this[ActionModuleType.SucceedAction].PerformModuleAction(target);
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

        private void UnsubscribeEvents()
        {
            Resource.CurrentChanges -= OnCurrentResourceChanges;
            Resource.MaximumChanges -= OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged -= Resource.OnParameterChanges;
            FloatDecoratorManager.ModuleDecoratorChanges -= OnFloatModuleDecoratorChanges;
            ActionDecoratorManager.ModuleDecoratorChanges -= OnCharacterActionModuleDecoratorChanges;

            foreach (var attack in _attackQueue)
            {
                attack.OnAttackResult -= OnAttackResult;
                attack.OnAttackCanceled -= OnAttackCanceled;
            }
        }

        private void OnFloatModuleDecoratorChanges(ModuleParameter parameter, IValueModule<float> module) => _floatModules[parameter] = module;
        private void OnCharacterActionModuleDecoratorChanges(ActionModuleType type, IActionModule<ICharacter> module) => _characterActionModules[type] = module;
        private void OnMaximumResourceChanges(float value) => MaximumResourceChanges?.Invoke(value);
        private void OnCurrentResourceChanges(float value) => CurrentResourceChanges?.Invoke(value);
        private void SetModules()
        {
            _floatModules = Enum.GetValues<ModuleParameter>().ToDictionary(param => param, FloatDecoratorManager.GetModule);
            _characterActionModules = Enum.GetValues<ActionModuleType>().ToDictionary(param => param, ActionDecoratorManager.GetModule);
        }
    }
}
