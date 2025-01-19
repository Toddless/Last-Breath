namespace Playground
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.Passives;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.StateMachine;

    [Inject]
    public partial class EnemyAI : ObservableCharacterBody2D
    {
        #region Components
        private AttributeComponent? _attribute;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private DefenceComponent? _defence;
        #endregion

        private bool _enemyFight = false, _playerEncounted = false;
        private CollisionShape2D? _enemiesCollisionShape;
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private RandomNumberGenerator? _rnd;
        private AnimatedSprite2D? _sprite;
        private Vector2 _respawnPosition;
        private Area2D? _area;
        private BattleBehavior? _battleBehavior;
        private List<Ability>? _abilities;
        private StateMachine? _machine;
        private EnemyType? _enemyType;
        private GlobalRarity _rarity;
        private int _level;

        [Signal]
        public delegate void EnemyDiedEventHandler(EnemyAI enemy);
        [Signal]
        public delegate void EnemyInitializedEventHandler();
        [Signal]
        public delegate void EnemyReachedNewPositionEventHandler();

        #region UI
        private Node2D? _inventoryNode;
        private Panel? _inventoryWindow;
        private GridContainer? _inventoryContainer;
        private EnemyInventoryComponent? _inventory;
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

        public AttackComponent? EnemyAttack
        {
            get => _attack;
            set => _attack = value;
        }

        public HealthComponent? EnemyHealth
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

        public List<Ability>? Abilities
        {
            get => _abilities;
        }

        public Vector2 RespawnPosition
        {
            get => _respawnPosition;
        }

        public bool PlayerEncounted
        {
            get => _playerEncounted;
            set => SetProperty(ref _playerEncounted, value);
        }

        public GlobalRarity Rarity
        {
            get => _rarity;
            set => _rarity = value;

        }

        public bool EnemyFigth
        {
            get => _enemyFight;
            set
            {
                if (SetProperty(ref _enemyFight, value))
                {
                    IAmInBattle();
                }
            }
        }

        public EnemyInventoryComponent? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }
        #endregion

        public override void _Ready()
        {
            var parentNode = GetParent().GetNode<EnemyAI>($"{Name}");
            _inventoryNode = parentNode.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            Inventory = _inventoryContainer.GetNode<EnemyInventoryComponent>(nameof(EnemyInventoryComponent));
            Inventory.Initialize(25, ScenePath.InventorySlot, _inventoryContainer, _inventoryNode.Hide, _inventoryNode.Show);
            _attack = parentNode.GetNode<AttackComponent>(nameof(AttackComponent));
            _health = parentNode.GetNode<HealthComponent>(nameof(HealthComponent));
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _machine = parentNode.GetNode<StateMachine>(nameof(StateMachine));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _attribute = parentNode.GetNode<AttributeComponent>(nameof(AttributeComponent));
            _battleBehavior = new BattleBehavior(this);
            _inventoryNode.Hide();
            ResolveDependencies();
            SetStats();
            SpawnItems();
        }

        protected void SpawnItems()
        {
            _inventory?.AddItem(DiContainer.ServiceProvider?.GetService<IBasedOnRarityLootTable>()?.GetRandomItem());
        }

        protected void SetStats()
        {
            _enemyType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            SetRandomAbilities();
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 50);
            var points = SetAttributesDependsOnType(_enemyType);
            _attribute!.Dexterity!.PropertyChanged += OnDexterityChange;
            _attribute.Strength!.PropertyChanged += OnStrengthChange;
            _attribute!.Strength.Total += points.Strength;
            _attribute!.Dexterity!.Total += points.Dexterity;
            SetAnimation();
            _health!.RefreshHealth();
            EmitSignal(SignalName.EnemyInitialized);
        }

        protected void ResolveDependencies() => DiContainer.InjectDependencies(this);

        protected void OnDexterityChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedWithValuesEventArgs<int> args)
            {
                if (args.NewValue < args.OldValue)
                {

                    _attack!.CurrentCriticalStrikeDamage = _attack.BaseCriticalStrikeDamage * _attribute!.Dexterity!.TotalAdditionalAttackChance();
                    _attack.CurrentCriticalStrikeChance = _attack.BaseCriticalStrikeChance * _attribute.Dexterity.TotalCriticalStrikeChance();
                    GD.Print($"Enemy {this.Name} has: Dex: {_attribute.Dexterity.Total}, CritChance: {_attack.CurrentCriticalStrikeChance}, CritDamage: {_attack.CurrentCriticalStrikeDamage}");
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
                    _health!.TotalPercentHealthIncreases -= _attribute!.Strength!.HealthIncrease * diff;

                }
                else
                {
                    _health!.TotalPercentHealthIncreases = _attribute!.Strength!.TotalHealthIncrese();
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

        public (float damage, bool crit) ActivateAbilityBeforDealDamage()
        {
            var chosenAbility = _battleBehavior!.MakeDecision();
            if (chosenAbility == null)
            {
                return _attack!.CalculateDamage();
            }

            IGameComponent? targetComponent = FindComponentToBeInvolved(chosenAbility);

            chosenAbility.ActivateAbility(targetComponent);

            return _attack!.CalculateDamage();
        }

        public IGameComponent? FindComponentToBeInvolved(Ability? chosenAbility) => chosenAbility?.TargetTypeComponent switch
        {
            var t when t == typeof(AttackComponent) => _attack,
            var t when t == typeof(HealthComponent) => _health,
            var t when t == typeof(AttackComponent) => _attribute,
            _ => null
        };

        protected void SetRandomAbilities()
        {
            //var amountAbilities = ConvertGlobalRarity.abilityQuantity[_rarity] + Mathf.Max(1, _level / 10);
            _abilities = new AbilityPool().GetAllAbilities();
        }

        public void PlayerExited(Node2D body)
        {
            if (body is Player s && !_area!.OverlapsBody(s))
            {
                PlayerEncounted = false;
            }
        }

        public void PlayerEntered(Node2D body)
        {
            if (body is Player s && _area!.OverlapsBody(s))
            {
                PlayerEncounted = true;
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
