namespace Battle.Source
{
    using Godot;
    using System;
    using Components;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;

    internal partial class Enemy : CharacterBody2D, IFightable
    {
        public IHealthComponent Health { get; private set; } = new HealthComponent();

        public IDamageComponent Damage { get; private set; } = new DamageComponent();

        public IDefenceComponent Defence { get; private set; } = new DefenseComponent();

        public bool IsFighting { get; set; }
        public bool IsAlive { get; set; }

        public IStance CurrentStance { get; set; }

        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;


        public override void _Ready()
        {

        }

        public void AllAttacks() { }
        public void OnBlockAttack() { }
        public void OnEvadeAttack() { }
        public void OnReceiveAttack(IAttackContext context)
        {

        }
        public void OnTurnEnd()
        {

        }
        public void OnTurnStart()
        {

        }
        public void TakeDamage(float damage, bool isCrit = false)
        {

        }
    }
}
