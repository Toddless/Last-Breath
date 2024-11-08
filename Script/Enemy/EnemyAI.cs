namespace Playground
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.Helpers;
    using Godot;
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;
    using System;
    using System.Linq;

    public partial class EnemyAI : Node2D
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

        [Signal]
        public delegate void EnemyDiedEventHandler(int rarity);

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

        public override void _Ready()
        {
            var parentNode = GetParent().GetNode<EnemyAI>("Enemy");
            _attack = parentNode.GetNode<AttackComponent>("AttackComponent");
            _health = parentNode.GetNode<HealthComponent>("HealthComponent");
            _area = parentNode.GetNode<Area2D>("Area2D");
            _collisionShape = parentNode.GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _rarity = GlobalRarity.Common;
            _health.IncreasedMaximumHealth(500);
            _health.RefreshHealth();
            _attackPassives =
            [
                new BuffAttack(),
                new VampireStrike(),
                new AdditionalAttack(),
            ];
        }

        public float EnemyDealDamage()
        {
            var chosenPasive = _attackPassives![_rnd.RandiRange(0, _attackPassives!.Count - 1)];


            var finalDamage = _attack!.CalculateDamage();


            return finalDamage;
        }

        public void PlayerEntered(Node body)
        {
            if (body is Player)
            {
                _globalSignals!.EmitSignal(GlobalSignals.SignalName.PlayerEncounted);
            }
        }
    }
}
