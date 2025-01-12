namespace Playground
{
    using Playground.Script.Passives.Attacks;
    using Playground.Script.StateMachine;
    using Playground.Script.Passives;
    using System.Collections.Generic;
    using Playground.Script.Helpers;
    using Playground.Script.Enums;
    using Playground.Components;
    using System.ComponentModel;
    using Godot;

    public partial class EnemyAI : ObservableCharacterBody2D
    {
        #region Components
        private AttributeComponent? _attribute;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private DefenceComponent? _defence;
        #endregion

        private readonly RandomNumberGenerator _rnd = new();
        private CollisionShape2D? _enemiesCollisionShape;
        private NavigationAgent2D? _navigationAgent2D;
        private CollisionShape2D? _areaCollisionShape;
        private BattleBehavior? _battleBehavior;
        private GlobalSignals? _globalSignals;
        private bool _playerEncounted = false;
        private List<Ability>? _abilities;
        private AnimatedSprite2D? _sprite;
        private bool _enemyFight = false;
        private Vector2 _respawnPosition;
        private StateMachine? _machine;
        private EnemyType? _enemyType;
        private GlobalRarity _rarity;
        private Area2D? _area;
        private int _level;

        [Signal]
        public delegate void EnemyDiedEventHandler(EnemyAI enemy);
        [Signal]
        public delegate void EnemyInitializedEventHandler();
        [Signal]
        public delegate void EnemyReachedNewPositionEventHandler();

        public NavigationAgent2D? NavigationAgent2D
        {
            get => _navigationAgent2D;
            set => _navigationAgent2D = value;
        }

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

        public RandomNumberGenerator Rnd
        {
            get => _rnd;
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

        public override void _Ready()
        {
            var randomTypeNumber = Rnd.RandiRange(1, 2);
            var parentNode = GetParent().GetNode<EnemyAI>($"{Name}");
            _attack = parentNode.GetNode<AttackComponent>(nameof(AttackComponent));
            _health = parentNode.GetNode<HealthComponent>(nameof(HealthComponent));
            _sprite = parentNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _machine = parentNode.GetNode<StateMachine>(nameof(StateMachine));
            _navigationAgent2D = parentNode.GetNode<NavigationAgent2D>(nameof(NavigationAgent2D));
            _area = parentNode.GetNode<Area2D>(nameof(Area2D));
            _areaCollisionShape = _area.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _enemiesCollisionShape = parentNode.GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _attribute = parentNode.GetNode<AttributeComponent>(nameof(AttributeComponent));
            _enemyType = SetRandomEnemyType(randomTypeNumber);
            SetRandomAbilities();
            _battleBehavior = new BattleBehavior(this);
            _respawnPosition = Position;
            Rarity = EnemyRarity();
            _level = _rnd.RandiRange(1, 50);
            var points = SetAttributesDependsOnType(_enemyType);
            _attribute!.Dexterity!.PropertyChanged += OnDexterityChange;
            _attribute.Strength!.PropertyChanged += OnStrengthChange;
            _attribute!.Strength.Total += points.Strength;
            EmitSignal(SignalName.EnemyInitialized);
            SetAnimation();
            _health.RefreshHealth();
            GD.Print($"Scene initialized: {this.Name}");
        }

        private void OnDexterityChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedWithValuesEventArgs<int> args)
            {
                if (args.NewValue < args.OldValue)
                {

                    _attack!.CurrentCriticalStrikeDamage = _attack.BaseCriticalStrikeDamage * _attribute!.Dexterity!.TotalAdditionalAttackChance();
                    _attack.CurrentCriticalStrikeChance = _attack.BaseCriticalStrikeChance * _attribute.Dexterity.TotalCriticalStrikeChance();
                    GD.Print($"Enemy {this.Name} has: Dex: {_attribute.Dexterity.Total}, CritChance: {_attack.CurrentCriticalStrikeChance}, CritDamage: {_attack.CurrentCriticalStrikeDamage}");
                }
                else
                {

                }
            }
        }

        private void OnStrengthChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedWithValuesEventArgs<int> args)
            {
                if (args.NewValue < args.OldValue)
                {
                    var diff = args.OldValue - args.NewValue;
                    _health.TotalPercentHealthIncreases -= _attribute.Strength.HealthIncrease * diff;

                }
                else
                {
                    _health.TotalPercentHealthIncreases = _attribute.Strength.TotalHealthIncrese();
                }
                GD.Print($"Current hp after strength increased: {_health.CurrentHealth}, Max Health: {_health.MaxHealth}\n" +
                    $"Strength: {_attribute.Strength.Total}");
                if (!_enemyFight)
                {
                    _health.RefreshHealth();
                }
            }
        }

        private void IAmInBattle() => _machine!.TransitionTo(States.Battle.ToString());

        public GlobalRarity EnemyRarity()
        {
            //var rarity = BasedOnRarityLootTable.Instance.GetRarity() ?? new RarityLoodDrop(new Rarity(), GlobalRarity.Uncommon);
            //return rarity.Rarity;
            return GlobalRarity.Common;
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

        public (float damage, bool crit) ActivateAbilityBeforDealDamage()
        {
            var chosenAbility = _battleBehavior!.MakeDecision();
            if (chosenAbility == null)
            {
                return _attack!.CalculateDamage();
            }

            IGameComponent? targetComponent = FindComponentToBeInvolved(chosenAbility);

            chosenAbility.ActivateAbility(targetComponent);

            GD.Print($"Activated ability {chosenAbility.GetType()}. Cooldown {chosenAbility.Cooldown}");
            return _attack!.CalculateDamage();
        }

        public IGameComponent? FindComponentToBeInvolved(Ability? chosenAbility) => chosenAbility?.TargetTypeComponent switch
        {
            var t when t == typeof(AttackComponent) => _attack,
            var t when t == typeof(HealthComponent) => _health,
            var t when t == typeof(AttackComponent) => _attribute,
            _ => null
        };

        public void SetRandomAbilities()
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

        private (int Strength, int Dexterity, int Intelligence) SetAttributesDependsOnType(EnemyType? enemyType)
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


        private EnemyType SetRandomEnemyType(int index)
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
