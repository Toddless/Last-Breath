namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.Passives;
    using Playground.Script.Scenes;
    using Playground.Script.StateMachine;

    [Inject]
    public partial class BaseEnemy : ObservableCharacterBody2D, ICharacter
    {
        #region Components
        private AttributeComponent? _attribute;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        #endregion

        private bool _enemyFight = false, _playerEncounter = false;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities = [];
        private EffectManager? _effectManager;
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
        private StateMachine? _machine;
        private IBattleContext? _battleContext;
        private EnemyType? _enemyType;
        private GlobalRarity _rarity;
        private int _level;

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

        public AttackComponent? AttackComponent
        {
            get => _attack;
            set => _attack = value;
        }

        public HealthComponent? HealthComponent
        {
            get => _health;
            set => _health = value;
        }

        public AttributeComponent? EnemyAttribute
        {
            get => _attribute;
            set => _attribute = value;
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
                if (SetProperty(ref _enemyFight, value))
                    IAmInBattle();
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

        public EffectManager EffectManager => throw new System.NotImplementedException();
        #endregion

        public override void _Ready()
        {
            _effects = [];
            _effectManager = new(_effects);
            _attack = new AttackComponent(_effectManager.CalculateValues);
            _health = new HealthComponent(_effectManager.CalculateValues);
            _attribute = new AttributeComponent();
            var parentNode = GetParent().GetNode<BaseEnemy>($"{Name}");
            _inventoryNode = parentNode.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            Inventory = new EnemyInventory();
            Inventory.Initialize(25, ScenePath.InventorySlot, _inventoryContainer, _inventoryNode.Hide, _inventoryNode.Show);
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _machine = parentNode.GetNode<StateMachine>(nameof(StateMachine));
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

        protected void SpawnItems()
        {
            _inventory?.AddItem(LootTable?.GetRandomItem());
        }

        protected void SetStats()
        {
            _enemyType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 50);
            var points = SetAttributesDependsOnType(_enemyType);
            _attribute!.Dexterity!.PropertyChanged += OnDexterityChange;
            _attribute.Strength!.PropertyChanged += OnStrengthChange;
            _attribute!.Strength.Total += points.Strength;
            _attribute!.Dexterity!.Total += points.Dexterity;
            SetAnimation();
            SetRandomAbilities();
            EmitSignal(SignalName.EnemyInitialized);
        }

        protected void OnDexterityChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedWithValuesEventArgs<int> args)
            {
                if (args.NewValue < args.OldValue)
                {
                    GD.Print($"Enemy {this.Name} has: Dex: {_attribute.Dexterity.Total}, CritChance: {_attack.CurrentCriticalChance}, CritDamage: {_attack.CurrentCriticalDamage}");
                }
            }
        }

        protected void OnStrengthChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedWithValuesEventArgs<int> args)
            {
                if (args.NewValue < args.OldValue)
                {
                    var diff = args.OldValue - args.NewValue;
                    _health!.IncreaseHealth -= _attribute!.Strength!.HealthIncrease * diff;

                }
                else
                {
                    _health!.IncreaseHealth = _attribute!.Strength!.TotalHealthIncrese();
                }
                GD.Print($"Current hp after strength increased: {_health.CurrentHealth}, Max Health: {_health.MaxHealth}\n" +
                    $"Strength: {_attribute.Strength.Total}");
                if (!_enemyFight)
                {
                    _health.RefreshHealth();
                }
            }
        }

        protected void IAmInBattle() => _machine!.TransitionTo(States.Battle.ToString());

        protected GlobalRarity EnemyRarity()
        {
            //var rarity = BasedOnRarityLootTable.Instance.GetRarity() ?? new RarityLoodDrop(new Rarity(), GlobalRarity.Uncommon);
            //return rarity.Rarity;
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

        public (float damage, bool crit, float leeched) ActivateAbilityBeforeDealDamage()
        {
            // working fine for buff, but what should i do to debuff someone?
            BattleBehavior?.MakeDecision()?.ActivateAbility(_battleContext);
            return _attack!.CalculateDamage();
        }

        protected void SetRandomAbilities()
        {
            //var amountAbilities = ConvertGlobalRarity.abilityQuantity[_rarity] + Mathf.Max(1, _level / 10);
            using (var abilities = new AbilityPool())
            {
                _abilities = abilities.SelectAbilities(3);
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

        protected (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(EnemyType? enemyType)
        {
            var totalAttributes = _level + ((int)_rarity * (int)_rarity);

            int dominantAttribute = (int)(totalAttributes * 0.6f);
            int secondaryAttribute = (int)(totalAttributes * 0.2f);

            return enemyType switch
            {
                EnemyType.DexterityBased => (secondaryAttribute, dominantAttribute, secondaryAttribute),
                EnemyType.StrengthBased => (dominantAttribute + 15, secondaryAttribute, secondaryAttribute),
                EnemyType.IntelligenceBased => (secondaryAttribute, secondaryAttribute, dominantAttribute),
                _ => (1, 1, 1)
            };
        }

        protected EnemyType SetRandomEnemyType(int index)
        {
            return index switch
            {
                1 => EnemyType.DexterityBased,
                2 => EnemyType.StrengthBased,
                _ => EnemyType.None,
            };
        }
    }
}
