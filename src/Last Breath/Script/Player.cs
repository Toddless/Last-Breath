namespace LastBreath
{
    using Godot;
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using LastBreath.Script;
    using LastBreath.Components;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Battle;
    using LastBreath.Localization;
    using LastBreath.Script.Items;
    using LastBreath.Script.Helpers;
    using Core.Interfaces.Components;
    using LastBreath.Resource.Quests;
    using System.Collections.Generic;
    using LastBreath.Script.Inventory;
    using LastBreath.Script.QuestSystem;
    using LastBreath.Script.BattleSystem;
    using LastBreath.Script.Abilities.Interfaces;

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
        private IDefenseComponent? _playerDefense;
        private IEffectsManager? _effectsManager;
        private IHealthComponent? _playerHealth;
        private IDamageComponent? _playerDamage;
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
        public string CharacterName { get; private set; } = "Player";
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public PlayerProgress Progress => _progress;
        public IDefenseComponent Defense => _playerDefense ??= new DefenseComponent();
        public IHealthComponent Health => _playerHealth ??= new HealthComponent();
        public IDamageComponent Damage => _playerDamage ??= new DamageComponent(new UnarmedDamageStrategy());
        public IEffectsManager Effects => _effectsManager ??= new EffectsManager(this);
        public IModifierManager Modifiers => _modifierManager;

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
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;
        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();
        #endregion

        public override void _Ready()
        {
            _playerDamage = new DamageComponent(new UnarmedDamageStrategy());
            _effectsManager = new EffectsManager(this);
            _playerHealth = new HealthComponent();
            _playerDefense = new DefenseComponent();
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _sprite.Play("Idle_down");
            _playerSkills = new(this);
            LoadDialogues();
            SetEvents();
            _attribute.IncreaseAttributeByAmount(Parameter.Dexterity, 5);
            _attribute.IncreaseAttributeByAmount(Parameter.Strength, 5);
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

        public void AddItemToInventory(IItem item)
        {
            if (item is IEquipItem)
                _equipInventory?.TryAddItem(item);

            // if (item is CraftingItem)
            // _craftingInventory?.AddItem(item);

            if (item is QuestItem)
            {
                _questItemsInventory?.TryAddItem(item);
                Progress.OnQuestItemCollected(item);
            }
        }

        public void OnItemCollect(IItem item)
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

        public void OnTurnStart()
        {
        }

        public void OnReceiveAttack(IAttackContext context)
        {
            if (_currentStance == null)
            {
                HandleSkills(context.PassiveSkills);
                // TODO: Own method
                var reducedByArmorDamage = Calculations.DamageReduceByArmor(context);
                var damageLeftAfterBarrierabsorption = Defense.BarrierAbsorbDamage(reducedByArmorDamage);

                if (damageLeftAfterBarrierabsorption > 0)
                {
                    Health.TakeDamage(damageLeftAfterBarrierabsorption);
                }

                context.SetAttackResult(new AttackResult(Skills.GetSkills(SkillType.GettingAttack), AttackResults.Succeed, context));
                return;
            }
            _currentStance.OnReceiveAttack(context);
        }

        public void AllAttacks() => AllAttacksFinished?.Invoke();
        public void OnEvadeAttack() => GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Evaded));
        public void OnBlockAttack() => GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Blocked));
        public List<ISkill> GetSkills(SkillType type) => Skills.GetSkills(type);

        public void AddSkill(ISkill skill)
        {
            if (skill is IStanceSkill stanceSkill) AddToStance(stanceSkill);
            else Skills.AddSkill(skill);
        }

        public void RemoveSkill(ISkill skill)
        {
            if (skill is IStanceSkill stanceSkill) RemoveFromStance(stanceSkill);
            else Skills.RemoveSkill(skill);
        }

        public void TakeDamage(float damage, bool isCrit = false)
        {
            // some actions like sound, animation etc.
            Health.TakeDamage(damage);
            GD.Print($"{this.CharacterName} taked: {damage} damage");
            GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Succeed, damage, isCrit));
        }

        private void RemoveFromStance(IStanceSkill stanceSkill) => this[stanceSkill.RequiredStance].StanceSkillComponent.RemoveSkill(stanceSkill);
        private void AddToStance(IStanceSkill stanceSkill) => this[stanceSkill.RequiredStance].StanceSkillComponent.AddSkill(stanceSkill);

        private void OnPlayerDead()
        {
            Dead?.Invoke(this);
            CanFight = false;
            IsAlive = false;
        }

        private void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
          
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

    }
}
