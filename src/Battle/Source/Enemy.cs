namespace Battle.Source
{
    using Godot;
    using System;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;

    internal partial class Enemy : CharacterBody2D, IFightable
    {
        public IHealthComponent Health => throw new NotImplementedException();

        public IDamageComponent Damage => throw new NotImplementedException();

        public IDefenceComponent Defence => throw new NotImplementedException();

        public bool IsFighting { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsAlive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event Action? TurnStart;
        public event Action? TurnEnd;
        public event Action<IAttackContext>? BeforeAttack;
        public event Action<IAttackContext>? AfterAttack;
        public event Action<IOnGettingAttackEventArgs>? GettingAttack;

        public void AllAttacks() => throw new NotImplementedException();
        public void OnBlockAttack() => throw new NotImplementedException();
        public void OnEvadeAttack() => throw new NotImplementedException();
        public void OnReceiveAttack(IAttackContext context) => throw new NotImplementedException();
        public void OnTurnEnd() => throw new NotImplementedException();
        public void OnTurnStart() => throw new NotImplementedException();
        public void TakeDamage(float damage, bool isCrit = false) => throw new NotImplementedException();
    }
}
