namespace Playground
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Godot;
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;
    using System;
    using Playground.Script.Helpers;
    using Playground.Script.StateMachine;

    public partial class EnemyAI : ObservableObject
    {
        private Dictionary<Type, int> _passivesOnCooldown = [];
        private readonly RandomNumberGenerator _rnd = new();
        private CollisionShape2D? _collisionShape;
        private GlobalSignals? _globalSignals;
        private bool _playerEncounted = false;
        private List<Node>? _attackPassives;
        private AnimatedSprite2D? _sprite;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private StateMachine? _machine;
        private GlobalRarity _rarity;
        private Player? _player;
        private Area2D? _area;

        [Signal]
        public delegate void EnemyDiedEventHandler(int rarity);
        [Signal]
        public delegate void EnemyInitializedEventHandler();

        public AttackComponent? EnemyAttack
        {
            get => _attack;
            set => _attack = value;
        }

        public HealthComponent? Health
        {
            get => _health;
            set => _health = value;
        }

        public bool PlayerEncounted
        {
            get => _playerEncounted;
            set
            {
                if (SetProperty(ref _playerEncounted, value))
                {
                    GD.Print($"Changed to: {_playerEncounted}");
                }
            }
        }

        public override void _Ready()
        {
            var parentNode = GetParent().GetNode<EnemyAI>($"{Name}");
            _attack = parentNode.GetNode<AttackComponent>("AttackComponent");
            _health = parentNode.GetNode<HealthComponent>("HealthComponent");
            _sprite = parentNode.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            _machine = parentNode.GetNode<StateMachine>("StateMachine");
            _area = parentNode.GetNode<Area2D>("Area2D");
            _collisionShape = parentNode.GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
            _player = GetParent().GetNode<Player>("CharacterBody2D");
            _rarity = GlobalRarity.Common;
            _health.IncreasedMaximumHealth(500);
            _health.RefreshHealth();
            _attackPassives =
            [
                new BuffAttack(),
                new VampireStrike(),
                new AdditionalAttack(),
            ];
            _sprite.Play();
            EmitSignal(SignalName.EnemyInitialized);
        }

        public float EnemyDealDamage()
        {
            var chosenPasive = _attackPassives![_rnd.RandiRange(0, _attackPassives!.Count - 1)];
            var finalDamage = _attack!.CalculateDamage();
            return finalDamage;
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
