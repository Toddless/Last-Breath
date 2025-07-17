namespace Playground
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    [Inject]
    public partial class BaseEnemy : CharacterBody2D, ICharacter
    {
        #region Components
        private readonly AttributeComponent _enemyAttribute = new();
        private readonly ModifierManager _modifierManager = new();
        private EffectsManager? _effectsManager;
        private HealthComponent? _enemyHealth;
        private DamageComponent? _enemyDamage;
        private DefenseComponent? _enemyDefense;
        #endregion

        private bool _enemyFight = false, _playerEncounter = false, _canMove, _isAlive = true;
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
        private GlobalRarity _rarity;
        private EnemyType _enemyType;
        private IStance? _currentStance;

        #region UI
        private Node2D? _inventoryNode;
        private Panel? _inventoryWindow;
        private GridContainer? _inventoryContainer;
        private EnemyInventory? _inventory;
        #endregion

        #region Properties
        public EnemyType EnemyType => _enemyType;
        public AttributeType? AttributeType => _enemyAttributeType;
        public bool CanFight
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

        public RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public Vector2 RespawnPosition
        {
            get => _respawnPosition;
        }

        public GlobalRarity Rarity
        {
            get => _rarity;
            set => _rarity = value;

        }

        public EnemyInventory? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        [Inject]
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

        public DamageComponent Damage => _enemyDamage ??= new(new UnarmedDamageStrategy());

        public HealthComponent Health => _enemyHealth ??= new();

        public DefenseComponent Defense => _enemyDefense ??= new();

        public EffectsManager Effects => _effectsManager ??= new(this);

        public ModifierManager Modifiers => _modifierManager;

        public IStance? CurrentStance => _currentStance;

        int ICharacter.Initiative => Rnd.RandiRange(0, 15);

        public bool IsAlive => _isAlive;

        public event Action<ICharacter>? Dead, InitializeFight;
        public event Action? AllAttacksFinished;

        #endregion

        public override void _Ready()
        {
            _effectsManager = new(this);
            _enemyHealth = new();
            _enemyDefense = new();
            // later i need strategy for enemies
            _enemyDamage = new(new UnarmedDamageStrategy());
            var parentNode = GetParent().GetNode<BaseEnemy>($"{Name}");
            _inventoryNode = parentNode.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            Inventory = new EnemyInventory();
            Inventory.Initialize(25, _inventoryContainer);
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _inventoryNode.Hide();
            SetEvents();
            DiContainer.InjectDependencies(this);
            SetStats();
            // SpawnItems();
            Health?.HealUpToMax();
        }

        public void OnGettingKill()
        {
            CanFight = false;
            CanMove = false;
            // TODO: Turn "death" state on in witch enemy lay down for N time befor reincarnated
            // change sprite and animation
            Area?.Hide();
        }

        public void TakeDamage(float damage)
        {
            // some actions like sound, animation etc.

            Health.TakeDamage(damage);
        }

        protected void SpawnItems()
        {
            // _inventory?.AddItem(LootTable?.GetRandomItem());
        }

        private void SetStats()
        {
            _enemyAttributeType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 300);
            var points = SetAttributesDependsOnType(_enemyAttributeType);
            _enemyAttribute.IncreaseAttributeByAmount(Parameter.Dexterity, 5);
            _enemyAttribute.IncreaseAttributeByAmount(Parameter.Strength, 5);
            _modifierManager.AddPermanentModifier(new MaxHealthModifier(ModifierType.Flat, 500, this));
            SetAnimation();
            _currentStance = SetStance(_enemyAttributeType);
            _currentStance.OnActivate();
        }

        private void SetEvents()
        {
            _area!.BodyEntered += PlayerEntered;
            _modifierManager.ParameterModifiersChanged += Damage.OnParameterChanges;
            _modifierManager.ParameterModifiersChanged += Health.OnParameterChanges;
            _modifierManager.ParameterModifiersChanged += Defense.OnParameterChanges;
            _enemyHealth.EntityDead += OnEntityDead;
            _enemyAttribute.CallModifierManager = _modifierManager.UpdatePermanentModifier;
        }

        private void OnEntityDead()
        {
            CanFight = false;
            _isAlive = false;
            Dead?.Invoke(this);
            GD.Print($"Dead: {GetType().Name}");
        }

        private IStance SetStance(AttributeType? enemyAttributeType)
        {
            return enemyAttributeType switch
            {
                Script.Enums.AttributeType.Dexterity => new DexterityStance(this),
                Script.Enums.AttributeType.Intelligence => new IntelligenceStance(this),
                Script.Enums.AttributeType.Strength => new StrengthStance(this),
                _ => throw new ArgumentOutOfRangeException(nameof(enemyAttributeType)),
            };
        }

        private GlobalRarity EnemyRarity()
        {
            return GlobalRarity.Uncommon;
        }

        private void SetAnimation()
        {
            switch (Rarity)
            {
                case GlobalRarity.Rare:
                    _sprite!.Play("Bat_Rare");
                    break;
                case GlobalRarity.Epic:
                    _sprite!.Play("Bat_Epic");
                    break;
                case GlobalRarity.Legendary:
                    _sprite!.Play("Bat_Legend");
                    break;
                case GlobalRarity.Mythic:
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
                CanFight = false;
            }
        }

        private (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(AttributeType? enemyType)
        {
            var totalAttributes = _level + ((int)_rarity * (int)_rarity);

            int dominantAttribute = (int)(totalAttributes * 0.8f);
            int secondaryAttribute = (int)(totalAttributes * 0.1f);

            return enemyType switch
            {
                Script.Enums.AttributeType.Dexterity => (secondaryAttribute, dominantAttribute, secondaryAttribute),
                Script.Enums.AttributeType.Strength => (dominantAttribute + 15, secondaryAttribute, secondaryAttribute),
                _ => (1, 1, 1)
            };
        }

        private AttributeType SetRandomEnemyType(int index)
        {
            return index switch
            {
                1 => Script.Enums.AttributeType.Dexterity,
                2 => Script.Enums.AttributeType.Strength,
                _ => Script.Enums.AttributeType.Intelligence,
            };
        }

        private string SetId()
        {
            var id = new StringBuilder();
            id.Append(NpcName);
            id.Append('_');
            id.Append(Fraction.ToString());
            return id.ToString();
        }

        public void OnTurnStart(Action action)
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
            action.Invoke();
        }

        public void OnTurnEnd()
        {
            //Effects.UpdateEffects();
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
                    GD.Print($"Character: {GetName()} take damage: {damageLeftAfterBarrierabsorption}");
                }

                context.SetAttackResult(new AttackResult([], AttackResults.Succeed, context));
                return;
            }
            _currentStance.OnReceiveAttack(context);
        }


        private void HandleSkills(List<ISkill> passiveSkills)
        {

        }

        public void AllAttacks() => AllAttacksFinished?.Invoke();
    }
}
