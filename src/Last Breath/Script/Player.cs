namespace Playground
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Localization;
    using Playground.Resource.Quests;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;
    using Playground.Script.QuestSystem;

    public partial class Player : CharacterBody2D, ICharacter
    {
        #region Private fields
        private const int BaseSpeed = 600;
        private bool _canMove = true, _canFight = true, _isPlayerRunning = false, _isAlive = true;
        private float _moveProgress = 0f;
        private int _exp, _gold;
        private ICharacter? _target;
        private IStance? _currentStance;
        private AnimatedSprite2D? _sprite;
        private Vector2 _targetPosition, _startPosition;
        private Inventory? _equipInventory, _craftingInventory, _questItemsInventory;
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private DefenseComponent? _playerDefense;
        private EffectsManager? _effectsManager;
        private HealthComponent? _playerHealth;
        private DamageComponent? _playerDamage;
        private SkillsComponent? _playerSkills;
        private readonly ModifierManager _modifierManager = new();
        private readonly AttributeComponent _attribute = new();
        private readonly PlayerProgress _progress = new();
        private readonly Dictionary<Stance, IStance> _stances = [];
        private RandomNumberGenerator _rnd = new();
        #endregion

        #region Properties
        protected SkillsComponent Skills => _playerSkills ??= new(this);
        protected IStance this[Stance stance] => _stances[stance];
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }

        public bool CanFight
        {
            get => _canFight;
            set => _canFight = value;
        }

        public IStance? CurrentStance
        {
            get => _currentStance;
            set
            {
                if (ObservableProperty.SetProperty(ref _currentStance, value))
                {

                }
            }
        }

        public ICharacter? Target
        {
            get => _target;
            set
            {
                if (ObservableProperty.SetProperty(ref _target, value))
                {
                    GD.Print($"New Target set to: {_target?.GetType().Name}");
                    UpdateTargetForCurrentSetOfAbilities(value);
                }
            }
        }

        [Export]
        public bool FirstSpawn { get; set; } = true;
        [Export]
        public int Speed { get; private set; } = BaseSpeed;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public PlayerProgress Progress => _progress;
        public DefenseComponent Defense => _playerDefense ??= new();
        public HealthComponent Health => _playerHealth ??= new();
        public DamageComponent Damage => _playerDamage ??= new(new UnarmedDamageStrategy());
        public Inventory EquipInventory => _equipInventory ??= new();
        public Inventory CraftingInventory => _craftingInventory ??= new();
        public Inventory QuestItemsInventory => _questItemsInventory ??= new();
        public EffectsManager Effects => _effectsManager ??= new(this);
        public ModifierManager Modifiers => _modifierManager;

        int ICharacter.Initiative => _rnd.RandiRange(0, 15);

        public bool IsAlive
        {
            get => _isAlive;
            protected set => _isAlive = value;
        }
        #endregion

        #region Events and Signals
        public event Action<string>? ItemCollected, QuestCompleted, LocationVisited, DialogueCompleted;
        public event Action<EnemyKilledEventArgs>? EnemyKilled;
        public event Action<List<IAbility>>? SetAvailableAbilities;
        public event Action<ICharacter>? Dead;
        public event Action? AllAttacksFinished;
        public event Action<DamageTakenEventArgs>? DamageTaken;
        public Action? NextPhase;

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();
        #endregion

        public override void _Ready()
        {
            _playerDamage = new(new UnarmedDamageStrategy());
            _effectsManager = new(this);
            _playerHealth = new();
            _playerDefense = new();
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _sprite.Play("Idle_down");
            _equipInventory = new();
            _craftingInventory = new();
            _questItemsInventory = new();
            _playerSkills = new(this);
            LoadDialogues();
            SetEvents();
            _attribute.IncreaseAttributeByAmount(Parameter.Dexterity, 5);
            _attribute.IncreaseAttributeByAmount(Parameter.Strength, 5);
            Modifiers.AddPermanentModifier(new MaxHealthModifier(ModifierType.Flat, 800, this));
            Modifiers.AddPermanentModifier(new DamageModifier(ModifierType.Flat, 150, this));
            Health.HealUpToMax();
            _stances.Add(Stance.Dexterity, new DexterityStance(this));
            _stances.Add(Stance.Strength, new StrengthStance(this));
            _stances.Add(Stance.Intelligence, new IntelligenceStance(this));
            GameManager.Instance.Player = this;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_isPlayerRunning)
            {
                _moveProgress += (float)delta;
                float t = Mathf.Clamp(_moveProgress / 0.06f, 0f, 6f);
                Position = _targetPosition.Lerp(_startPosition, t);
                if (t >= 6f)
                {
                    _canMove = true;
                    _canFight = true;
                    _isPlayerRunning = false;
                }
            }

            if (!_canMove) return;

            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * Speed;
            MoveAndSlide();
        }

        public void AddItemToInventory(Item item)
        {
            if (item is EquipItem)
                _equipInventory?.AddItem(item);

            // if (item is CraftingItem)
            // _craftingInventory?.AddItem(item);

            if (item is QuestItem)
            {
                _questItemsInventory?.AddItem(item);
                Progress.OnQuestItemCollected(item);
            }
        }

        public void OnItemCollect(Item item)
        {
            AddItemToInventory(item);
            ItemCollected?.Invoke(item.Id);
        }

        public void OnRunAway(Vector2 enemyPosition)
        {
            _targetPosition = enemyPosition;
            _startPosition = Position;
            _moveProgress = 0;
            _isPlayerRunning = true;
        }

        public void OnEquipWeapon(IDamageStrategy strategy) => _playerDamage?.ChangeStrategy(strategy);
        public void OnUnequipWeapon() => _playerDamage?.ChangeStrategy(new UnarmedDamageStrategy());
        public void OnEnemyKilled(BaseEnemy enemy) => EnemyKilled?.Invoke(new EnemyKilledEventArgs(enemy.EnemyId, enemy.EnemyType));
        public void OnLocationVisited(string id) => LocationVisited?.Invoke(id);

        public void SetDexterityStance()
        {
            _currentStance = _stances[Stance.Dexterity];
            _currentStance.OnActivate();
        }

        public void SetStrengthStance()
        {
            _currentStance = _stances[Stance.Strength];
            _currentStance.OnActivate();
        }

        public void SetIntelligenceStance()
        {
            _currentStance = _stances[Stance.Intelligence];
            _currentStance.OnActivate();
        }

        public void OnDialogueCompleted(string id)
        {
            Progress.OnDialogueCompleted(id);
            DialogueCompleted?.Invoke(id);
        }

        public void OnQuestCompleted(Quest quest)
        {
            Progress?.OnQuestCompleted(quest.Id);
            AcceptReward(quest.GetReward());
            QuestCompleted?.Invoke(quest.Id);
        }

        public void OnTurnEnd()
        {
            Effects.UpdateEffects();
            UpdateAbilityCoodowns();
        }

        public void OnTurnStart(Action nextTurnPhase)
        {
            NextPhase = nextTurnPhase;

            if (Effects.IsEffectApplied(Script.Enums.Effects.Stun | Script.Enums.Effects.Paralysis)) NextPhase.Invoke();
        }

        public void OnReceiveAttack(AttackContext context)
        {
            if (_currentStance == null)
            {
                HandleSkills(context.PassiveSkills);
                // TODO: Own method
                var reducedByArmorDamage = Calculations.DamageReduceByArmor(context);
                var damageLeftAfterBarrierabsorption = this.Defense.BarrierAbsorbDamage(reducedByArmorDamage);

                if (damageLeftAfterBarrierabsorption > 0)
                {
                    this.Health.TakeDamage(damageLeftAfterBarrierabsorption);
                }

                context.SetAttackResult(new AttackResult([], AttackResults.Succeed, context));
                return;
            }
            _currentStance.OnReceiveAttack(context);
        }

        public void AllAttacks() => AllAttacksFinished?.Invoke();

        public void AddSkill(ISkill skill)
        {
            if (skill is IStanceSkill stanceSkill) AddToStance(stanceSkill);
            else Skills.AddSkill(skill);
        }


        public void TakeDamage(float damage, bool isCrit = false)
        {
            // some actions like sound, animation etc.
            Health.TakeDamage(damage);

            DamageTaken?.Invoke(new(damage, isCrit, this));
        }

        private void AddToStance(IStanceSkill stanceSkill) => this[stanceSkill.RequiredStance].StanceSkillManager.AddSkill(stanceSkill);

        private void OnPlayerDead()
        {
            Dead?.Invoke(this);
            CanFight = false;
            IsAlive = false;
        }

        private void OnParameterChanges(object? sender, ModifiersChangedEventArgs args)
        {
            switch (args.Parameter)
            {
                case Parameter.Movespeed:
                    Speed = Mathf.RoundToInt(Calculations.CalculateFloatValue(BaseSpeed, args.Modifiers));
                    break;
                default:
                    break;
            }
        }

        private void UpdateTargetForCurrentSetOfAbilities(ICharacter value)
        {
            //_abilities[_currentStance].ForEach(x => x.Target = value);
        }

        private void UpdateAbilityStates()
        {
            //foreach (var abilityList in _abilities)
            //{
            //    abilityList.Value.ForEach(x => x.UpdateState());
            //}
        }

        private void UpdateAbilityCoodowns()
        {
            //foreach (var abilityList in _abilities)
            //{
            //    abilityList.Value.ForEach(x => x.UpdateCooldown());
            //}
        }

        private void AcceptReward(Reward? reward)
        {
            if (reward == null) return;
            if (reward.Items.Count > 0)
            {
                foreach (var item in reward.Items)
                {
                    AddItemToInventory(item);
                }
            }
            _exp += reward.Exp;
            _gold += reward.Gold;
        }

        private void HandleSkills(List<ISkill> passiveSkills)
        {

        }

        private void SetEvents()
        {
            Modifiers.ParameterModifiersChanged += Damage.OnParameterChanges;
            Modifiers.ParameterModifiersChanged += Health.OnParameterChanges;
            Modifiers.ParameterModifiersChanged += Defense.OnParameterChanges;
            Modifiers.ParameterModifiersChanged += OnParameterChanges;
            Health.EntityDead += OnPlayerDead;
            _attribute.CallModifierManager = _modifierManager.UpdatePermanentModifier;
        }


        private void LoadDialogues()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>("res://Resources/Dialogues/PlayerDialogues/playerDialoguesData.tres");
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }

        public List<ISkill> GetSkills(SkillType type) => throw new NotImplementedException();
    }
}
