namespace LastBreath
{
    using Godot;
    using System;
    using Core.Enums;
    using System.Linq;
    using LastBreath.Components;
    using Core.Interfaces.Items;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Skills;
    using LastBreath.Script.Enemy;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Utilities;
    using Core.Interfaces.Entity;

    public partial class BaseEnemy : CharacterBody2D, IEntity
    {
        #region Components
        private readonly AttributeComponent _enemyAttribute = new();
        private readonly IModifierManager _modifierManager = new ModifierManager();
        private IEffectsManager? _effectsManager;
        private IHealthComponent? _enemyHealth;
        private IDamageComponent? _enemyDamage;
        private IDefenceComponent? _enemyDefense;
        private SkillsComponent? _enemySkills;
        #endregion

        private bool _enemyFight = false, _playerEncounter = false, _canMove;
        private int _level;
        private string? _enemyId;
        private IBasedOnRarityLootTable? _lootTable;
        private CollisionShape2D? _enemiesCollisionShape;
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private RandomNumberGenerator _rnd = new();
        private AnimatedSprite2D? _sprite;
        private Vector2 _respawnPosition;
        private Area2D? _area;
        private AttributeType? _enemyAttributeType;
        private Core.Enums.Rarity _rarity;
        private EnemyType _enemyType;
        private IStance? _currentStance;

        #region UI
        private Node2D? _inventoryNode;
        private Panel? _inventoryWindow;
        private GridContainer? _inventoryContainer;
        #endregion


        #region Properties
        protected SkillsComponent Skills => _enemySkills ??= new(this);

        public EnemyType EnemyType => _enemyType;
        public AttributeType? AttributeType => _enemyAttributeType;
        public string CharacterName { get; private set; } = "Enemy";

        [Export] public string Id { get; private set; } = string.Empty;

        [Export] public string[] Tags { get; set; } = [];

        [Export] public Texture2D? Icon { get; set; }

        public string Description => Localizator.LocalizeDescription(Id);

        public string DisplayName => Localizator.Localize(Id);

        public bool IsFighting
        {
            get => _enemyFight;
            set => _enemyFight = value;
        }

        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }

        public NavigationAgent2D? NavigationAgent2D
        {
            get => _navigationAgent2D;
            set => _navigationAgent2D = value;
        }

        public Area2D? Area
        {
            get => _area;
        }

        public RandomNumberGenerator Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public Vector2 RespawnPosition
        {
            get => _respawnPosition;
        }

        public Core.Enums.Rarity Rarity
        {
            get => _rarity;
            set => _rarity = value;

        }

        protected IBasedOnRarityLootTable? LootTable
        {
            get => _lootTable;
            set => _lootTable = value;
        }
        [Export]
        public Fractions Fraction { get; set; }

        [Export]
        public string NpcName { get; set; } = string.Empty;

        public string EnemyId => _enemyId ??= SetId();

        public IDamageComponent Damage => _enemyDamage ??= new DamageComponent(new UnarmedDamageStrategy());
        public IHealthComponent Health => _enemyHealth ??= new HealthComponent();
        public IDefenceComponent Defence => _enemyDefense ??= new DefenseComponent();
        public IEffectsManager Effects => _effectsManager ??= new EffectsManager(this);
        public IModifierManager Modifiers => _modifierManager;

        public IStance CurrentStance => _currentStance ??= SetStance(_enemyAttributeType);

        public bool IsAlive { get; set; }



        public event Action<IEntity>? Dead, InitializeFight;
        public event Action? AllAttacksFinished;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;
        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;

        #endregion

        public override void _Ready()
        {
            _effectsManager = new EffectsManager(this);
            _enemyHealth = new HealthComponent();
            _enemyDefense = new DefenseComponent();
            _enemySkills = new(this);
            // later i need strategy for enemies
            _enemyDamage = new DamageComponent(new UnarmedDamageStrategy());
            var parentNode = GetParent().GetNode<BaseEnemy>($"{Name}");
            _inventoryNode = parentNode.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _inventoryNode.Hide();
            SetEvents();
            SetStats();
            // SpawnItems();
            Health?.HealUpToMax();
        }

        public void OnGettingKill()
        {
            IsFighting = false;
            CanMove = false;
            // TODO: Turn "death" state on in witch enemy lay down for N time befor reincarnated
            // change sprite and animation
            Area?.Hide();
        }

        public void TakeDamage(float damage, bool isCrit = false)
        {
            Health.TakeDamage(damage);
            GD.Print($"{this.Name} taked: {damage} damage");
            GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Succeed, damage, isCrit));
        }

        public void OnTurnStart()
        {
            var handler = BattleHandler.Instance;
            if (handler != null)
            {
                var target = handler.Fighters.FirstOrDefault(x => x is Player);
                if (target == null)
                {
                    return;
                }
                CurrentStance?.OnAttack(target);
            }
            //UIEventBus.PublishNextPhase();
        }

        public void OnEvadeAttack() => GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Evaded));
        public void OnBlockAttack() => GettingAttack?.Invoke(new OnGettingAttackEventArgs(this, AttackResults.Blocked));

        public void OnTurnEnd()
        {
            //Effects.UpdateEffects();
        }

        public void OnReceiveAttack(IAttackContext context)
        {
            if (_currentStance == null)
            {

                HandleSkills(context.PassiveSkills);
                // TODO: Own method
                var reducedByArmorDamage = Calculations.DamageReduceByArmor(context);
                var damageLeftAfterBarrierabsorption = this.Defence.BarrierAbsorbDamage(reducedByArmorDamage);

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

        public void AddSkill(ISkill skill)
        {
            // NPC should only get abilities that work with in their main stance
            if (skill is IStanceSkill stanceSkill)
            {
                if (CurrentStance.StanceType == stanceSkill.RequiredStance)
                    CurrentStance.StanceSkillComponent.AddSkill(stanceSkill);
            }
            else
                Skills.AddSkill(skill);
        }

        public void RemoveSkill(ISkill skill)
        {
            if (skill is IStanceSkill stanceSkill)
            {
                if (CurrentStance.StanceType == stanceSkill.RequiredStance)
                    CurrentStance.StanceSkillComponent.RemoveSkill(stanceSkill);
            }
            else Skills.RemoveSkill(skill);
        }

        public List<ISkill> GetSkills(SkillType type) => Skills.GetSkills(type);

        public void AddItemToInventory(IItem item)
        {

        }

        protected void SpawnItems()
        {
            // _inventory?.AddItem(LootTable?.GetRandomItem());
        }

        private void HandleSkills(List<ISkill> passiveSkills)
        {

        }


        private void SetStats()
        {
            _enemyAttributeType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 300);
            var points = SetAttributesDependsOnType(_enemyAttributeType);
            _enemyAttribute.IncreaseAttributeByAmount(Parameter.Dexterity, points.Dexterity);
            _enemyAttribute.IncreaseAttributeByAmount(Parameter.Strength, points.Strength);
            _enemyAttribute.IncreaseAttributeByAmount(Parameter.Intelligence, points.Intelligence);
            SetAnimation();
            _currentStance = SetStance(_enemyAttributeType);
            _currentStance.OnActivate();
        }

        private void SetEvents()
        {
            _area!.BodyEntered += PlayerEntered;
            Modifiers.ParameterModifiersChanged += Health.OnParameterChanges;
            Modifiers.ParameterModifiersChanged += Defence.OnParameterChanges;
            Health.EntityDead += OnEntityDead;
        }

        private void OnEntityDead()
        {
            IsFighting = false;
            IsAlive = false;
            Dead?.Invoke(this);
            GD.Print($"Dead: {GetType().Name}");
        }

        private IStance SetStance(AttributeType? enemyAttributeType)
        {
            return enemyAttributeType switch
            {
                Core.Enums.AttributeType.Dexterity => new DexterityStance(this),
                Core.Enums.AttributeType.Intelligence => new IntelligenceStance(this),
                Core.Enums.AttributeType.Strength => new StrengthStance(this),
                _ => throw new ArgumentOutOfRangeException(nameof(enemyAttributeType)),
            };
        }

        private Core.Enums.Rarity EnemyRarity()
        {
            return Core.Enums.Rarity.Uncommon;
        }

        private void SetAnimation()
        {
            switch (Rarity)
            {
                case Core.Enums.Rarity.Rare:
                    _sprite!.Play("Bat_Rare");
                    break;
                case Core.Enums.Rarity.Epic:
                    _sprite!.Play("Bat_Epic");
                    break;
                case Core.Enums.Rarity.Legendary:
                    _sprite!.Play("Bat_Legend");
                    break;
                case Core.Enums.Rarity.Mythic:
                    _sprite!.Play("Bat_Myth");
                    break;
                default:
                    _sprite!.Play("Bat_Uncomm");
                    break;
            }
        }

        private void PlayerEntered(Node2D body)
        {
            if (body is Player s && _area!.OverlapsBody(s))
            {
                InitializeFight?.Invoke(this);
                IsFighting = false;
            }
        }

        private (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(AttributeType? enemyType)
        {
            var totalAttributes = _level + ((int)_rarity * (int)_rarity);

            var dominantAttribute = Mathf.RoundToInt(totalAttributes * 0.8f);
            var secondaryAttribute = Mathf.RoundToInt(totalAttributes * 0.1f);

            return enemyType switch
            {
                Core.Enums.AttributeType.Dexterity => (secondaryAttribute, dominantAttribute, secondaryAttribute),
                Core.Enums.AttributeType.Strength => (dominantAttribute, secondaryAttribute, secondaryAttribute),
                Core.Enums.AttributeType.Intelligence => (secondaryAttribute, secondaryAttribute, dominantAttribute),
                _ => (1, 1, 1)
            };
        }

        private AttributeType SetRandomEnemyType(int index)
        {
            return index switch
            {
                1 => Core.Enums.AttributeType.Dexterity,
                2 => Core.Enums.AttributeType.Strength,
                _ => Core.Enums.AttributeType.Intelligence,
            };
        }

        private string SetId() => $"{NpcName}_{Fraction}";
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
