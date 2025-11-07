namespace Battle.Source
{
    using Godot;
    using System;
    using Core.Enums;
    using Core.Constants;
    using Components;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    internal partial class Player : CharacterBody2D, IFightable
    {
        private float _baseSpeed = 500;
        private readonly Dictionary<Stance, IStance> _stances = [];
        public IHealthComponent Health { get; private set; } = new HealthComponent();

        public IDamageComponent Damage { get; private set; } = new DamageComponent();

        public IDefenceComponent Defence { get; private set; } = new DefenseComponent();
        public IStance CurrentStance { get; private set; }

        public bool IsFighting { get; set; }
        public bool IsAlive { get; set; }


        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;


        public override void _Ready()
        {
            Health = new HealthComponent();
        }

        public override void _Process(double delta)
        {
            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * _baseSpeed;
            MoveAndSlide();
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
