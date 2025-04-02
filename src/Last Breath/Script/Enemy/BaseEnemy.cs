namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Attribute;
    using Playground.Script.BattleSystem;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.ScenesHandlers;

    [Inject]
    public partial class BaseEnemy : ObservableCharacterBody2D, ICharacter
    {
        #region Components
        private readonly AttributeComponent _enemyAttribute = new();
        private HealthComponent? _enemyHealth;
        private DamageComponent? _enemyDamage;
        #endregion

        private bool _enemyFight = false, _playerEncounter = false;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities = [];
        private IBasedOnRarityLootTable? _lootTable;
        private CollisionShape2D? _enemiesCollisionShape;
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private RandomNumberGenerator? _rnd;
        private AnimatedSprite2D? _sprite;
        private Vector2 _respawnPosition;
        private Area2D? _area;
        private BattleBehavior? _battleBehavior;
        private List<IAbility>? _abilities = new();
        private IBattleContext? _battleContext;
        private EnemyAttributeType? _enemyAttributeType;
        private GlobalRarity _rarity;
        private int _level;
        private string? _enemyId;
        private EnemyType _enemyType;
        private readonly ModifierManager _modifierManager = new();

        [Signal]
        public delegate void EnemyDiedEventHandler(BaseEnemy enemy);
        [Signal]
        public delegate void EnemyInitializedEventHandler();
        [Signal]
        public delegate void EnemyReachedNewPositionEventHandler();

        #region UI
        private Node2D? _inventoryNode;
        private Panel? _inventoryWindow;
        private GridContainer? _inventoryContainer;
        private EnemyInventory? _inventory;
        #endregion


        #region Properties
        public EnemyType EnemyType => _enemyType;

        public NavigationAgent2D? NavigationAgent2D
        {
            get => _navigationAgent2D;
            set => _navigationAgent2D = value;
        }

        [Inject]
        public BattleBehavior? BattleBehavior
        {
            get => _battleBehavior;
            set => _battleBehavior = value;
        }

        public Area2D? Area
        {
            get => _area;
        }

        public DamageComponent? EnemyDamage
        {
            get => _enemyDamage;
            set => _enemyDamage = value;
        }

        public HealthComponent? EnemyHealth
        {
            get => _enemyHealth;
            set => _enemyHealth = value;
        }

        public AttributeComponent? EnemyAttribute
        {
            get => _enemyAttribute;
        }

        [Inject]
        public RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public List<IAbility>? Abilities
        {
            get => _abilities;
        }

        public Vector2 RespawnPosition
        {
            get => _respawnPosition;
        }

        public bool PlayerEncounter
        {
            get => _playerEncounter;
            set => SetProperty(ref _playerEncounter, value);
        }

        public GlobalRarity Rarity
        {
            get => _rarity;
            set => _rarity = value;

        }

        public bool EnemyFight
        {
            get => _enemyFight;
            set
            {
                SetProperty(ref _enemyFight, value);
            }
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

        public ObservableCollection<IAbility>? AppliedAbilities
        {
            get => _appliedAbilities;
            set => _appliedAbilities = value;
        }

        [Export]
        public Fractions Fraction { get; set; }

        [Export]
        public string NpcName { get; set; } = string.Empty;

        public string EnemyId => _enemyId ??= SetId();

        private string SetId()
        {
            var id = new StringBuilder();
            id.Append(NpcName);
            id.Append('_');
            id.Append(Fraction.ToString());
            return id.ToString();
        }
        #endregion

        public override void _Ready()
        {
            _effects = [];
            _enemyHealth = new HealthComponent(_modifierManager);
            // later i need strategy for enemies
            _enemyDamage = new DamageComponent(new UnarmedDamageStrategy(), _modifierManager);
            var parentNode = GetParent().GetNode<BaseEnemy>($"{Name}");
            _inventoryNode = parentNode.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            Inventory = new EnemyInventory();
            Inventory.Initialize(25, _inventoryContainer, _inventoryNode.Hide, _inventoryNode.Show);
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _battleBehavior = new BattleBehavior();
            _inventoryNode.Hide();
            DiContainer.InjectDependencies(this);
            SetStats();
            SpawnItems();
        }

        public bool IsPlayerNearby() => Area?.GetOverlappingBodies().Any(x => x is Player) == true;

        protected void SpawnItems()
        {
            _inventory?.AddItem(LootTable?.GetRandomItem());
        }

        protected void SetStats()
        {
            _enemyAttributeType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 50);
            var points = SetAttributesDependsOnType(_enemyAttributeType);
            _enemyAttribute.AddAttribute(new Dexterity(_modifierManager) { InvestedPoints = points.Dexterity });
            _enemyAttribute.AddAttribute(new Strength(_modifierManager) { InvestedPoints = points.Strength });
            _enemyAttribute.AddAttribute(new Intelligence(_modifierManager) { InvestedPoints = points.Intelligence });
            SetAnimation();
            EmitSignal(SignalName.EnemyInitialized);
        }

        protected GlobalRarity EnemyRarity()
        {
            return GlobalRarity.Common;
        }

        protected void SetAnimation()
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

        public void PlayerExited(Node2D body)
        {
            if (body is Player s && !_area!.OverlapsBody(s))
            {
                PlayerEncounter = false;
            }
        }

        public void PlayerEntered(Node2D body)
        {
            if (body is Player s && _area!.OverlapsBody(s))
            {
                PlayerEncounter = true;
            }
        }

        protected (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(EnemyAttributeType? enemyType)
        {
            var totalAttributes = _level + ((int)_rarity * (int)_rarity);

            int dominantAttribute = (int)(totalAttributes * 0.6f);
            int secondaryAttribute = (int)(totalAttributes * 0.2f);

            return enemyType switch
            {
                EnemyAttributeType.DexterityBased => (secondaryAttribute, dominantAttribute, secondaryAttribute),
                EnemyAttributeType.StrengthBased => (dominantAttribute + 15, secondaryAttribute, secondaryAttribute),
                EnemyAttributeType.IntelligenceBased => (secondaryAttribute, secondaryAttribute, dominantAttribute),
                _ => (1, 1, 1)
            };
        }

        protected EnemyAttributeType SetRandomEnemyType(int index)
        {
            return index switch
            {
                1 => EnemyAttributeType.DexterityBased,
                2 => EnemyAttributeType.StrengthBased,
                _ => EnemyAttributeType.None,
            };
        }
    }
}
