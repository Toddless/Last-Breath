namespace Playground.Script.Enemy
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract partial class EnemyGeneric : ObservableCharacterBody2D
    {
        private readonly RandomNumberGenerator _rnd = new();
        private CollisionShape2D? _collisionShape;
        private GlobalSignals? _globalSignals;
        private bool _playerEncounted = false;
        private AnimatedSprite2D? _sprite;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private GlobalRarity _rarity;
        private Player? _player;
        private Area2D? _area;
        private int _level;

        [Signal]
        public delegate void EnemyDiedEventHandler(int rarity);
        [Signal]
        public delegate void EnemyInitializedEventHandler();

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

        public bool PlayerEncounted
        {
            get => _playerEncounted;
            set => SetProperty(ref _playerEncounted, value);
        }

        public override void _Ready()
        {
            var parentNode = GetParent().GetNode<EnemyGeneric>($"{Name}");
            _attack = parentNode.GetNode<AttackComponent>("AttackComponent");
            _health = parentNode.GetNode<HealthComponent>("HealthComponent");
            _sprite = parentNode.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            _area = parentNode.GetNode<Area2D>("Area2D");
            _collisionShape = parentNode.GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
            _player = GetParent().GetNode<Player>("CharacterBody2D");
            Rarity = EnemyRarity();
            _health.RefreshHealth();
            _sprite.Play();
            Position = new Vector2(300, 300);
            GD.Print($"My health: {_health.CurrentHealth}, damage: {_attack.BaseMinDamage} - {_attack.BaseMaxDamage}");
            EmitSignal(SignalName.EnemyInitialized);
        }

        public virtual GlobalRarity EnemyRarity()
        {
            //var rarity = BasedOnRarityLootTable.Instance.GetRarity() ?? new RarityLoodDrop(new Rarity(), GlobalRarity.Uncommon);
            //return rarity.Rarity;
            return GlobalRarity.Uncommon;
        }

        private void SetStatsBasedOnRarity()
        {
            _level = _rnd.RandiRange(1, 50);
            //SetAnimation();
            //SetStats();
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

        public virtual (float, bool) EnemyDealDamage()
        {
            return _attack!.CalculateDamage();
        }

        public virtual void PlayerExited(Node2D body)
        {
            if (body is Player s && !_area!.OverlapsBody(s))
            {
                PlayerEncounted = false;
            }
        }

        public virtual void PlayerEntered(Node2D body)
        {
            if (body is Player s && _area!.OverlapsBody(s))
            {
                PlayerEncounted = true;
            }
        }
    }
}
