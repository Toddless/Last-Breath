namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    public class SilentFuryPassive(float chance)
        : Skill(id: "Passive_Skill_Silent_Fury")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(SilenceSealChance)] = SilenceSealChance,
                };
                return field;
            }
        }
        public float SilenceSealChance { get; } = chance;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new SilentFuryPassive(SilenceSealChance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not SilentFuryPassive silentFuryPassive) return false;
            return SilenceSealChance > silentFuryPassive.SilenceSealChance;
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (Owner == null || obj.Context.Result is not AttackResults.Succeed) return;
            if (obj.Context.Target is not IFightableNpc { Fraction: Fractions.Undead }) return;
            if (!ChanceRoll.Roll(SilenceSealChance, obj.Context.Rnd)) return;

            _ = new SilenceSeal().Apply(new EffectApplyingContext { Target = obj.Context.Target, Source = InstanceId, Caster = Owner });
        }
    }
}
