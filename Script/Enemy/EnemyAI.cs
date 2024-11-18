namespace Playground
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.StateMachine;

    public partial class EnemyAI : ObservableObject
    {
        private readonly RandomNumberGenerator _rnd = new();
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private CollisionShape2D? _enemiesCollisionShape;
        private GlobalSignals? _globalSignals;
        private bool _playerEncounted = false;
        private AnimatedSprite2D? _sprite;
        private HealthComponent? _health;
        private Vector2 _respawnPosition;
        private AttackComponent? _attack;
        private StateMachine? _machine;
        private GlobalRarity _rarity;
        private Area2D? _area;
        private int _level;

        [Signal]
        public delegate void EnemyDiedEventHandler(int rarity);
        [Signal]
        public delegate void EnemyInitializedEventHandler();
        [Signal]
        public delegate void EnemyReachedNewPositionEventHandler();

        public AttackComponent? EnemyAttack
        {
            get => _attack;
            set => _attack = value;
        }

        public GlobalRarity Rarity
        {
            get => _rarity;
            set
            {
                if (SetProperty(ref _rarity, value))
                {
                    SetStatsBasedOnRarity();
                }
            }
        }

        public HealthComponent? Health
        {
            get => _health;
            set => _health = value;
        }

        public NavigationAgent2D? NavigationAgent2D
        {
            get => _navigationAgent2D;
            set => _navigationAgent2D = value;
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

        public override void _Ready()
        {
            var parentNode = GetParent().GetNode<EnemyAI>($"{Name}");
            _attack = parentNode.GetNode<AttackComponent>(nameof(AttackComponent));
            _health = parentNode.GetNode<HealthComponent>(nameof(HealthComponent));
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _machine = parentNode.GetNode<StateMachine>(nameof(StateMachine));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _health.RefreshHealth();
            AddToGroup("MovingObstacles");
            EmitSignal(SignalName.EnemyInitialized);
        }

        public GlobalRarity EnemyRarity()
        {
            var rarity = BasedOnRarityLootTable.Instance.GetRarity() ?? new RarityLoodDrop(new Rarity(), GlobalRarity.Uncommon);
            return rarity.Rarity;
        }

        public void SetStatsBasedOnRarity()
        {
            _level = _rnd.RandiRange(1, 50);
            SetAnimation();
            SetStats();
        }

        public void SetAnimation()
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

        public void SetStats()
        {
            switch (Rarity)
            {
                case GlobalRarity.Rare:
                    _attack!.BaseMinDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Rare];
                    _attack.BaseMaxDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Rare];
                    _health!.MaxHealth *= (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Rare];
                    break;
                case GlobalRarity.Epic:
                    _attack!.BaseMinDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Epic];
                    _attack.BaseMaxDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Epic];
                    _health!.MaxHealth *= (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Epic];
                    break;
                case GlobalRarity.Legendary:
                    _attack!.BaseMinDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Legendary];
                    _attack.BaseMaxDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Legendary];
                    _health!.MaxHealth *= (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Legendary];
                    break;
                case GlobalRarity.Mythic:
                    _attack!.BaseMinDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Mythic];
                    _attack.BaseMaxDamage += (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Mythic];
                    _health!.MaxHealth *= (_level + 3 * 6) * ConvertGlobalRarity.multiplier[GlobalRarity.Mythic];
                    break;
                default:
                    _attack!.BaseMinDamage += (_level + 3 * 6) * 1.5f;
                    _attack.BaseMaxDamage += (_level + 3 * 6) * 1.5f;
                    _health!.MaxHealth *= (_level + 3 * 6) * 1.5f;
                    break;
            }
        }

        public float EnemyDealDamage()
        {
            return _attack!.CalculateDamage();
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
    }
}
