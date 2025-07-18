namespace Playground
{
    using Godot;
    using System;
    using System.Linq;
    using Playground.Script;
    using Playground.Components;
    using Playground.Localization;
    using Playground.Script.Enums;
    using Playground.Script.Items;
    using Playground.Script.Helpers;
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using Playground.Script.Inventory;
    using Playground.Script.QuestSystem;
    using Playground.Script.BattleSystem;
    using Playground.Components.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Abilities.Interfaces;

    public partial class Player : CharacterBody2D, ICharacter
    {
        #region Private fields
        private const int BaseSpeed = 1200;
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
        private readonly List<IStance> _stances = [];
        private RandomNumberGenerator _rnd = new();
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region Properties
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
        // i think i need some ability component later, because i need a place where player can modifiy, learn or forget abilities

        int ICharacter.Initiative => _rnd.RandiRange(0, 15);

        public bool IsAlive => _isAlive;

        public SkillsComponent Skills => _playerSkills ??= new(this);
        #endregion

        #region Events
        public event Action<string>? ItemCollected, QuestCompleted, LocationVisited, DialogueCompleted;
        public event Action<EnemyKilledEventArgs>? EnemyKilled;
        public event Action<List<IAbility>>? SetAvailableAbilities;
        public event Action<ICharacter>? Dead;
        public event Action? AllAttacksFinished;

        public Action? NextPhase;
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
            GameManager.Instance.Player = this;
            _attribute.IncreaseAttributeByAmount(Parameter.Dexterity, 5);
            _attribute.IncreaseAttributeByAmount(Parameter.Strength, 5);
            Modifiers.AddPermanentModifier(new MaxHealthModifier(ModifierType.Flat, 800, this));
            Modifiers.AddPermanentModifier(new DamageModifier(ModifierType.Flat, 150, this));
            _playerHealth.HealUpToMax();
            _stances.Add(new DexterityStance(this));
            _stances.Add(new StrengthStance(this));
            _stances.Add(new IntelligenceStance(this));
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
            var stance = _stances.FirstOrDefault(x => x is DexterityStance);
            if (stance != null)
            {
                _currentStance = stance;
                _currentStance.OnActivate();
            }
        }

        public void SetStrengthStance()
        {
            var stance = _stances.FirstOrDefault(x => x is StrengthStance);

            if (stance != null)
            {
                _currentStance = stance;
                _currentStance.OnActivate();
            }
        }

        public void SetIntelligenceStance()
        {
            var stance = _stances.FirstOrDefault(y => y is IntelligenceStance);
            if (stance != null)
            {
                _currentStance = stance;
                _currentStance.OnActivate();
            }
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
        }

        public void OnReceiveAttack(AttackContext context)
        {
            if (_currentStance == null)
            {
              //  HandleSkills(context.PassiveSkills);
                // TODO: Own method
                var reducedByArmorDamage = Calculations.DamageReduceByArmor(context);
                var damageLeftAfterBarrierabsorption = this.Defense.BarrierAbsorbDamage(reducedByArmorDamage);

                if (damageLeftAfterBarrierabsorption > 0)
                {
                    this.Health.TakeDamage(damageLeftAfterBarrierabsorption);
                    GD.Print($"Character: {GetName()} take damage: {damageLeftAfterBarrierabsorption}");
                }

                context.SetAttackResult(new AttackResult([], AttackResults.Succeed, context));
                return;
            }
            _currentStance.OnReceiveAttack(context);
        }

        public void AllAttacks() => AllAttacksFinished?.Invoke();

        public void TakeDamage(float damage)
        {
            // some actions like sound, animation etc.

            Health.TakeDamage(damage);
        }

        private void OnPlayerDead()
        {
            Dead?.Invoke(this);
            CanFight = false;
            _isAlive = false;
            // play death animation, call death screen
            GD.Print($"Dead: {GetType().Name}");
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
            _modifierManager.ParameterModifiersChanged += Damage.OnParameterChanges;
            _modifierManager.ParameterModifiersChanged += Health.OnParameterChanges;
            _modifierManager.ParameterModifiersChanged += Defense.OnParameterChanges;
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
            _playerHealth.EntityDead += OnPlayerDead;
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

    }
}
