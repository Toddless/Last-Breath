namespace Playground
{
    using System;
    using System.Text;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Abilities.Effects;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Attribute;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    [Inject]
    public partial class BaseEnemy : ObservableCharacterBody2D, ICharacter
    {
        #region Components
        private readonly AttributeComponent _enemyAttribute = new();
        private readonly ModifierManager _modifierManager = new();
        private EffectsManager? _effectsManager;
        private HealthComponent? _enemyHealth;
        private DamageComponent? _enemyDamage;
        private DefenseComponent? _enemyDefense;
        private ResourceComponent? _resourceManager;
        #endregion

        private bool _enemyFight = false, _playerEncounter = false, _canMove;
        private int _level;
        private string? _enemyId;
        private IBasedOnRarityLootTable? _lootTable;
        private CollisionShape2D? _enemiesCollisionShape;
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private RandomNumberGenerator? _rnd;
        private AnimatedSprite2D? _sprite;
        private Vector2 _respawnPosition;
        private Area2D? _area;
        private EnemyAttributeType? _enemyAttributeType;
        private GlobalRarity _rarity;
        private EnemyType _enemyType;
        private Stance _stance;

        #region UI
        private Node2D? _inventoryNode;
        private Panel? _inventoryWindow;
        private GridContainer? _inventoryContainer;
        private EnemyInventory? _inventory;
        #endregion

        #region Properties
        public EnemyType EnemyType => _enemyType;
        public EnemyAttributeType? AttributeType => _enemyAttributeType;
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
        public Stance Stance
        {
            get => _stance;
            set => _stance = value;
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

        [Inject]
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

        public DamageComponent Damage => _enemyDamage ??= new(new UnarmedDamageStrategy(), _modifierManager);

        public HealthComponent Health => _enemyHealth ??= new(_modifierManager);

        public DefenseComponent Defense => _enemyDefense ??= new(_modifierManager);

        public EffectsManager Effects => _effectsManager ??= new(this);

        public ModifierManager Modifiers => _modifierManager;

        public ResourceComponent Resource => _resourceManager ??= new(_stance, _modifierManager);

        public event Action<BaseEnemy>? InitializeFight;

        #endregion

        public override void _Ready()
        {
            _effectsManager = new(this);
            _enemyHealth = new(_modifierManager);
            _enemyDefense = new(_modifierManager);
            // later i need strategy for enemies
            _enemyDamage = new(new UnarmedDamageStrategy(), _modifierManager);
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
            _inventoryNode.Hide();
            DiContainer.InjectDependencies(this);
            SetStats();
            SetEvents();
            // SpawnItems();
            Health?.HealUpToMax();
        }

        public IAbility? GetAbility()
        {
            return null;
        }

        public void OnTurnEnd()
        {
            Effects.UpdateEffects();
            RecoverResource();
        }

        public void OnFightEnds()
        {
            Effects.RemoveAllEffects();
            // TODO: on reset temporary i still might have some effects in effects manager
            Modifiers.ResetTemporaryModifiers();
        }

        protected void SpawnItems()
        {
            _inventory?.AddItem(LootTable?.GetRandomItem());
        }

        private void SetStats()
        {
            _enemyAttributeType = SetRandomEnemyType(Rnd!.RandiRange(1, 2));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = Rnd.RandiRange(1, 300);
            var points = SetAttributesDependsOnType(_enemyAttributeType);
            _enemyAttribute.AddAttribute(new Dexterity(_modifierManager) { InvestedPoints = 5 });
            _enemyAttribute.AddAttribute(new Strength(_modifierManager) { InvestedPoints = 5 });
            _modifierManager.AddPermanentModifier(new MaxHealthModifier(ModifierType.Additive, 500));
            SetAnimation();
            _stance = SetStance(_enemyAttributeType);
        }

        private void RecoverResource()
        {
            // TODO: instead of GoliathEffect should be effect that block resource recovery
            //if (Effects.IsEffectApplied(typeof(GoliathEffect)))
            //{
            //    return;
            //}
            //_resourceManager?.RecoverCurrentResource();
        }

        private void SetEvents()
        {
            _area!.BodyEntered += PlayerEntered;
        }

        private Stance SetStance(EnemyAttributeType? enemyAttributeType)
        {
            return enemyAttributeType switch
            {
                EnemyAttributeType.DexterityBased => Stance.Dexterity,
                EnemyAttributeType.StrengthBased => Stance.Strength,
                EnemyAttributeType.IntelligenceBased => Stance.Intelligence,
                _ => Stance.None
            };
        }

        private GlobalRarity EnemyRarity()
        {
            return GlobalRarity.Common;
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
                CanFight = true;
            }
        }

        private (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(EnemyAttributeType? enemyType)
        {
            var totalAttributes = _level + ((int)_rarity * (int)_rarity);

            int dominantAttribute = (int)(totalAttributes * 0.8f);
            int secondaryAttribute = (int)(totalAttributes * 0.1f);

            return enemyType switch
            {
                EnemyAttributeType.DexterityBased => (secondaryAttribute, dominantAttribute, secondaryAttribute),
                EnemyAttributeType.StrengthBased => (dominantAttribute + 15, secondaryAttribute, secondaryAttribute),
                EnemyAttributeType.IntelligenceBased => (secondaryAttribute, secondaryAttribute, dominantAttribute),
                _ => (1, 1, 1)
            };
        }

        private EnemyAttributeType SetRandomEnemyType(int index)
        {
            return index switch
            {
                1 => EnemyAttributeType.DexterityBased,
                2 => EnemyAttributeType.StrengthBased,
                _ => EnemyAttributeType.None,
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
    }
}
