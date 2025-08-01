namespace LastBreath.Script
{
    using System;
    using System.Collections.Generic;
    using Contracts.Enums;
    using LastBreath.Components;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.BattleSystem;

    public interface ICharacter
    {
        HealthComponent Health { get; }
        DamageComponent Damage { get; }
        DefenseComponent Defense { get; }
        EffectsManager Effects { get; }
        ModifierManager Modifiers { get; }

        public IStance? CurrentStance { get; }
        string CharacterName { get; }
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
        void OnTurnStart();
        void OnReceiveAttack(AttackContext context);
        void TakeDamage(float damage, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
    }
}
