namespace Playground.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class StanceBase : IStance
    {
        private readonly Queue<AttackContext> _attackQueue = [];
        private int _pendingAttacks = 0;
        private bool _canProceed = true;

        private Dictionary<StatModule, IStatModule> _statModules { get; set; } = [];
        private Dictionary<SkillType, ISkillModule> _skillModules { get; set; } = [];
        private Dictionary<ActionModule, IActionModule<ICharacter>> _characterActionModules { get; set; } = [];
        private StanceSkillComponent? _skillComponent;
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

        public StanceSkillComponent StanceSkillManager
        {
            get => _skillComponent ??= new(this);
            protected set => _skillComponent = value;
        }

        public IResource Resource { get; }
        public ModuleManager<StatModule, IStatModule, StatModuleDecorator> StatDecoratorManager { get; }

        public ModuleManager<ActionModule, IActionModule<ICharacter>, ActionModuleDecorator> ActionDecoratorManager { get; }

        public ModuleManager<SkillType, ISkillModule, SkillModuleDecorator> SkillDecoratorManager { get; }

        public Stance StanceType { get; }
     
        public event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        protected StanceBase(ICharacter owner, IResource resource, IStanceActivationEffect effect, Stance stanceType)
        {
            Owner = owner;
            Resource = resource;
            ActivationEffect = effect;
            StanceType = stanceType;
            // TODO: Rework this part later.
            StatDecoratorManager = new(new Dictionary<StatModule, IStatModule>
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

            ActionDecoratorManager = new(new Dictionary<ActionModule, IActionModule<ICharacter>>
            {
                [ActionModule.EvadeAction] = new HandleAttackEvadeModule(Owner),
                [ActionModule.SucceedAction] = new HandleAttackSucceedModule(Owner),
                [ActionModule.BlockAction] = new HandleAttackBlockedModule(Owner),
            });

            SkillDecoratorManager = new(new Dictionary<SkillType, ISkillModule>
            {
                [SkillType.PreAttack] = new PreAttackSkillModule(Owner),
                [SkillType.OnAttack] = new OnAttackSkillModule(Owner),
                [SkillType.AlwaysActive] = new AlwaysActiveSkillModule(Owner),
            });
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

        public virtual void OnReceiveAttack(AttackContext context)
        {
            if (CanProceed) HandleReceivedAttack(context);
            else
            {
                _attackQueue.Enqueue(context);
            }
        }

        public bool IsChainAttack() => this[StatModule.AdditionalAttackChance].GetValue() <= Owner.Damage.AdditionalHit;

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

            HandleSkills(context.PassiveSkills);
            context.Armor = this[StatModule.Armor].GetValue();
            context.MaxReduceDamage = this[StatModule.MaxReduceDamage].GetValue();
            context.FinalDamage = Calculations.DamageReduceByArmor(context);

            Owner.TakeDamage(context.FinalDamage, context.IsCritical);

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

        protected void ApplyPreAttackSkills(AttackContext context)
        {
            var skills = this[SkillType.PreAttack].GetSkills();

            foreach (var skill in skills.OfType<IPreAttackSkill>())
            {
                skill.Activate(context);
            }
        }

        protected virtual void HandleSkills(List<ISkill> passiveSkills)
        {

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

        protected bool IsCrit() => this[StatModule.CritChance].GetValue() <= Owner.Damage.CriticalChance;
        protected bool IsEvade() => this[StatModule.EvadeChance].GetValue() <= Owner.Defense.Evade;

        private void HandleEvadeReceivedAttack(AttackContext context)
        {
            context.SetAttackResult(new AttackResult([], AttackResults.Evaded, context));
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
