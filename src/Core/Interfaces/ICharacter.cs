namespace Core.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Skills;

    public interface ICharacter
    {
        IHealthComponent Health { get; }
        IDamageComponent Damage { get; }
        IDefenseComponent Defense { get; }
        IEffectsManager Effects { get; }
        IModifierManager Modifiers { get; }

        public IStance? CurrentStance { get; }
        string CharacterName { get; }
        bool CanFight { get; set; }
        bool CanMove { get; set; }
        bool IsAlive { get; }
        int Initiative { get; }

        event Action<ICharacter>? Dead;
        event Action? AllAttacksFinished;
        event Action<IOnGettingAttackEventArgs>? GettingAttack;

        void OnTurnEnd();
        void AddSkill(ISkill skill);
        void RemoveSkill(ISkill skill);
        List<ISkill> GetSkills(SkillType type);
        void OnTurnStart();
        void OnReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
    }
}
