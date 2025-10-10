namespace LastBreath.Script.BattleSystem
{
    using Godot;
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using LastBreath.Components;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;
    using LastBreath.Script.BattleSystem.Module;

    public abstract class StanceBase : IStance
    {
        private readonly Queue<IAttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;

        private Dictionary<StatModule, IStatModule> _statModules { get; set; } = [];
        private Dictionary<SkillType, ISkillModule> _skillModules { get; set; } = [];
        private Dictionary<ActionModule, IActionModule<ICharacter>> _characterActionModules { get; set; } = [];
        private IStanceSkillComponent? _skillComponent;

        protected IStatModule this[StatModule type] => _statModules[type];
        protected ISkillModule this[SkillType type] => _skillModules[type];
        protected IActionModule<ICharacter> this[ActionModule type] => _characterActionModules[type];

        protected IStanceActivationEffect ActivationEffect { get; }
        protected ICharacter Owner { get; }

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

        public IStanceSkillComponent StanceSkillComponent
        {
            get => _skillComponent ??= new StanceSkillComponent(this);
            protected set => _skillComponent = value;
        }

        public IResource Resource { get; }

        public IModuleManager<StatModule, IStatModule, StatModuleDecorator> StatDecoratorManager { get; }
        public IModuleManager<ActionModule, IActionModule<ICharacter>, ActionModuleDecorator> ActionDecoratorManager { get; }
        public IModuleManager<SkillType, ISkillModule, SkillModuleDecorator> SkillDecoratorManager { get; }

        public Stance StanceType { get; }

        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        protected StanceBase(ICharacter owner, IResource resource, IStanceActivationEffect effect, Stance stanceType)
        {
            Owner = owner;
            Resource = resource;
            ActivationEffect = effect;
            StanceType = stanceType;
            // TODO: Rework this part later.
            StatDecoratorManager = new ModuleManager<StatModule, IStatModule, StatModuleDecorator>(new Dictionary<StatModule, IStatModule>
            {
                [StatModule.AdditionalAttackChance] = new AdditionalHitChanceModule(),
                [StatModule.BlockChance] = new BlockChanceModule(),
                [StatModule.CritChance] = new CritChanceModule(),
                [StatModule.EvadeChance] = new EvadeChanceModule(),
                [StatModule.Damage] = new DamageModule(Owner),
                [StatModule.Armor] = new ArmorModule(Owner),
                [StatModule.MaxReduceDamage] = new MaxReduceDamageModule(Owner),
                [StatModule.CritDamage] = new CritDamageModule(Owner),
            });

            ActionDecoratorManager = new ModuleManager<ActionModule, IActionModule<ICharacter>, ActionModuleDecorator>(new Dictionary<ActionModule, IActionModule<ICharacter>>
            {
                [ActionModule.EvadeAction] = new HandleAttackEvadeModule(Owner),
                [ActionModule.SucceedAction] = new HandleAttackSucceedModule(Owner),
                [ActionModule.BlockAction] = new HandleAttackBlockedModule(Owner),
            });

            SkillDecoratorManager = new ModuleManager<SkillType, ISkillModule, SkillModuleDecorator>(new Dictionary<SkillType, ISkillModule>
            {
                [SkillType.PreAttack] = new PreAttackSkillModule(Owner),
                [SkillType.OnAttack] = new OnAttackSkillModule(Owner),
                [SkillType.AlwaysActive] = new AlwaysActiveSkillModule(Owner),
                [SkillType.GettingAttack] = new GettingAttackSkillModule(Owner),
            });
            SetModules();
        }

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

        public virtual void OnAttack(ICharacter target)
        {
            if (!CanAttack(target)) return;
            PendingAttacks++;

            AttackContext context = new(Owner, target)
            {
                Damage = this[StatModule.Damage].GetValue(),
                IsCritical = IsCrit(),
                CriticalDamageMultiplier = this[StatModule.CritDamage].GetValue(),
                PassiveSkills = this[SkillType.OnAttack].GetSkills(),
            };

            // create list with skills applied on attack
            ApplyPreAttackSkills(context);

            // why subscribe?
            // I need the result to decide what to do next
            context.OnAttackResult += OnAttackResult;
            context.OnAttackCanceled += OnAttackCanceled;
            CombatScheduler.Instance?.Schedule(context);
        }

        public virtual void OnReceiveAttack(IAttackContext context)
        {
            if (CanProceed) HandleReceivedAttack(context);
            else
            {
                _attackQueue.Enqueue(context);
            }
        }

        public bool IsChainAttack() => Mathf.Min(this[StatModule.AdditionalAttackChance].GetValue(), Owner.Damage.MaxAdditionalHit) <= Owner.Damage.AdditionalHit;

        protected bool CanAttack(ICharacter target) => target.IsAlive && Owner.IsAlive;

        protected virtual void HandleReceivedAttack(IAttackContext context)
        {
            CanProceed = false;
            if (!context.IgnoreEvade && IsEvade())
            {
                HandleEvadeReceivedAttack(context);
                CanProceed = true;
                return;
            }

            context.Armor = this[StatModule.Armor].GetValue();
            context.MaxReduceDamage = this[StatModule.MaxReduceDamage].GetValue();
            context.FinalDamage = Calculations.DamageReduceByArmor(context);
            Owner.TakeDamage(context.FinalDamage, context.IsCritical);

            HandleOnAttackSkills(context.PassiveSkills);
            context.SetAttackResult(new AttackResult(this[SkillType.GettingAttack].GetSkills(), AttackResults.Succeed, context));
            CanProceed = true;
        }


        protected virtual void OnAttackResult(IAttackResult result)
        {
            // i dont need this context anymore, unsubscribe
            Unsubscribe(result);
            // Handle skills, we may getting from attacked target
            HandleOnGettingAttackSkills(result.PassiveSkills);
            // different reactions to attack results
            HandleResult(result);
            PendingAttacks--;
        }

        protected void ApplyPreAttackSkills(IAttackContext context)
        {
            var skills = this[SkillType.PreAttack].GetSkills();

            foreach (var skill in skills.OfType<IPreAttackSkill>())
            {
                skill.Activate(context);
            }
        }

        protected virtual void HandleOnAttackSkills(List<ISkill> passiveSkills)
        {
            foreach (var skill in passiveSkills.OfType<IOnAttackSkill>())
            {
                skill.Activate(Owner);
            }
        }

        protected virtual void HandleOnGettingAttackSkills(List<ISkill> passiveSkills)
        {
            foreach (var skill in passiveSkills.OfType<IOnGettingAttackSkill>())
            {
                skill.Activate(Owner);
            }
        }

        protected virtual void SubscribeEvents()
        {
            Resource.CurrentChanges += OnCurrentResourceChanges;
            Resource.MaximumChanges += OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged += Resource.OnParameterChanges;
            StatDecoratorManager.ModuleDecoratorChanges += OnStatModuleDecoratorChanges;
            ActionDecoratorManager.ModuleDecoratorChanges += OnCharacterActionModuleDecoratorChanges;
            SkillDecoratorManager.ModuleDecoratorChanges += OnSkillModuleDecoratorChanges;
        }

        protected bool IsCrit() => Mathf.Min(this[StatModule.CritChance].GetValue(), Owner.Damage.MaxCriticalChance) <= Owner.Damage.CriticalChance;
        protected bool IsEvade() => Mathf.Min(this[StatModule.EvadeChance].GetValue(), Owner.Defense.MaxEvadeChance) <= Owner.Defense.Evade;

        private void HandleEvadeReceivedAttack(IAttackContext context)
        {
            Owner.OnEvadeAttack();
            context.SetAttackResult(new AttackResult(this[SkillType.GettingAttack].GetSkills(), AttackResults.Evaded, context));
        }

        private void CheckIfAllAttacksHandled()
        {
            if (CanProceed && PendingAttacks == 0 && _attackQueue.Count == 0)
                Owner.AllAttacks();
        }

        private void HandleResult(IAttackResult result)
        {
            var target = result.Context.Target;

            switch (result.Result)
            {
                case AttackResults.Evaded:
                    this[ActionModule.EvadeAction].PerformModuleAction(target);
                    break;
                case AttackResults.Blocked:
                    this[ActionModule.BlockAction].PerformModuleAction(target);
                    break;
                case AttackResults.Succeed:
                    this[ActionModule.SucceedAction].PerformModuleAction(target);
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

        private void OnAttackCanceled(IAttackContext context)
        {
            context.OnAttackResult -= OnAttackResult;
            context.OnAttackCanceled -= OnAttackCanceled;
            PendingAttacks--;
        }

        private void Unsubscribe(IAttackResult result)
        {
            result.Context.OnAttackResult -= OnAttackResult;
            result.Context.OnAttackCanceled -= OnAttackCanceled;
        }

        private void UnsubscribeEvents()
        {
            Resource.CurrentChanges -= OnCurrentResourceChanges;
            Resource.MaximumChanges -= OnMaximumResourceChanges;
            Owner.Modifiers.ParameterModifiersChanged -= Resource.OnParameterChanges;
            StatDecoratorManager.ModuleDecoratorChanges -= OnStatModuleDecoratorChanges;
            ActionDecoratorManager.ModuleDecoratorChanges -= OnCharacterActionModuleDecoratorChanges;
            SkillDecoratorManager.ModuleDecoratorChanges -= OnSkillModuleDecoratorChanges;

            foreach (var attack in _attackQueue)
            {
                attack.OnAttackResult -= OnAttackResult;
                attack.OnAttackCanceled -= OnAttackCanceled;
            }
        }

        private void OnStatModuleDecoratorChanges(StatModule parameter, IStatModule module) => _statModules[parameter] = module;
        private void OnCharacterActionModuleDecoratorChanges(ActionModule type, IActionModule<ICharacter> module) => _characterActionModules[type] = module;
        private void OnSkillModuleDecoratorChanges(SkillType type, ISkillModule module) => _skillModules[type] = module;
        private void OnMaximumResourceChanges(float value) => MaximumResourceChanges?.Invoke(value);
        private void OnCurrentResourceChanges(float value) => CurrentResourceChanges?.Invoke(value);
        private void SetModules()
        {
            _statModules = Enum.GetValues<StatModule>().ToDictionary(param => param, StatDecoratorManager.GetModule);
            _characterActionModules = Enum.GetValues<ActionModule>().ToDictionary(param => param, ActionDecoratorManager.GetModule);
            _skillModules = Enum.GetValues<SkillType>().ToDictionary(param => param, SkillDecoratorManager.GetModule);
        }
    }
}
