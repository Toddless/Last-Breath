namespace Playground
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Godot;
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;
    using System;
    using Playground.Script.Helpers;

    public partial class EnemyAI : ObservableObject
    {
        private readonly RandomNumberGenerator _rnd = new();
        private GlobalSignals? _globalSignals;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private CollisionShape2D? _collisionShape;
        private List<Node>? _attackPassives;
        private Dictionary<Type, int> _passivesOnCooldown = [];
        private GlobalRarity _rarity;
        private Area2D? _area;
        private Player? _player;
        private bool _playerEncounted = false;

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
