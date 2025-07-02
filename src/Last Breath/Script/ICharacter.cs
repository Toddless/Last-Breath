namespace Playground.Script
{
    using System;
    using Playground.Components;
    using Playground.Components.Interfaces;

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
        int Initiative { get; }

        event Action<ICharacter>? Dead;
        event Action? AllAttacksFinished;
        void OnTurnEnd();
        void OnTurnStart(Action nextTurnPhase);
        void OnFightEnds();
        void OnAnimation();
        void OnReceiveAttack(AttackContext context);
        void AllAttacks();
        // TODO: Resources class
        // i need 3 types, for each stance
        // each of resource has its own way to recover
        // but all of them should be recovered at the end of the turn
    }
}
