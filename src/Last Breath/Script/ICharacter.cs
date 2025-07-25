namespace Playground.Script
{
    using System;
    using System.Collections.Generic;
    using Playground.Components;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;

    public interface ICharacter
    {
        HealthComponent Health { get; }
        DamageComponent Damage { get; }
        DefenseComponent Defense { get; }
        EffectsManager Effects { get; }
        ModifierManager Modifiers { get; }

        public IStance? CurrentStance { get; }
        bool CanFight { get; set; }
        bool CanMove { get; set; }
        bool IsAlive { get; }
        int Initiative { get; }

        event Action<ICharacter>? Dead;
        event Action? AllAttacksFinished;
        event Action<OnGettingAttackEventArgs>? GettingAttack;

        void OnTurnEnd();
        void AddSkill(ISkill skill);
        void RemoveSkill(ISkill skill);
        List<ISkill> GetSkills(SkillType type);
        void OnTurnStart(Action nextTurnPhase);
        void OnReceiveAttack(AttackContext context);
        void TakeDamage(float damage, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
    }
}
