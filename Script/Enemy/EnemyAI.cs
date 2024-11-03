namespace Playground
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.Helpers;
    using Godot;
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives;

    public partial class EnemyAI : Node2D
    {
        private readonly RandomNumberGenerator _rnd = new();
        private GlobalSignals? _globalSignals;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private CollisionShape2D? _collisionShape;
        private List<IAttackPassives>? _attackPassives;
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
            _attack = GetNode<AttackComponent>("/root/MainScene/Enemy/AttackComponent");
            _health = GetNode<HealthComponent>("/root/MainScene/Enemy/HealthComponent");
            _area = GetNode<Area2D>("/root/MainScene/Enemy/Area2D");
            _collisionShape = GetNode<CollisionShape2D>("/root/MainScene/Enemy/Area2D/CollisionShape2D");
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _area.BodyEntered += PlayerEntered;
            _health.OnCharacterDied += IamDead;
            _rarity = GlobalRarity.Common;
            _health.IncreasedMaximumHealth(10);
            _health.RefreshHealth();
            _attack.OnPlayerCriticalHit += EnemyDealCritical;
            _attackPassives =
            [
                new BuffAttack(),
            ];

            PassivesPool.TotalWeight();
        }

        private void EnemyDealCritical()
        {
            GD.Print("Critical Strike!");
        }

        public float EnemyDealDamage()
        {
            _attackPassives![0].ApplyBeforeAttack(_attack!);
            var finalDamage = _attack!.FinalDamage; ;
            _attackPassives[0].ApplyAfterAttack(_attack!, _health!, finalDamage);
            return finalDamage;
        }

        private void IamDead()
        {
            // Godot didnt sent enum value via Signals. Use int cast instead
            EmitSignal(SignalName.EnemyDied, (int)_rarity);
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
