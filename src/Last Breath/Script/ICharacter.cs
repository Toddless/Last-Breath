namespace Playground.Script
{
    using System;
    using Playground.Components;
    using Playground.Script.BattleSystem;

    public interface ICharacter
    {
        public HealthComponent Health { get; }
        public DamageComponent Damage { get; }
        public DefenseComponent Defense { get; }
        public EffectsManager Effects { get; }
        public ModifierManager Modifiers { get; }
        public IStance? CurrentStance { get; }
        bool CanFight { get; set; }
        bool CanMove { get; set; }
        bool IsAlive { get; }
        int Initiative { get; }

        event Action<ICharacter>? Dead;
        event Action? AllAttacksFinished;

        void OnTurnEnd();
        void OnTurnStart(Action nextTurnPhase);
        void OnReceiveAttack(AttackContext context);
        void TakeDamage(float damage);
        void AllAttacks();
    }
}
