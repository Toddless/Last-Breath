namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    public class PoisonedClaws : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PoisonDuration)] = PoisonDuration,
                    [nameof(PercentFromDamage)] = PercentFromDamage,
                };
                return field;
            }
        }

        public PoisonedClaws(float percentFormDamageFromDamage, int poisonDuration)
            : base(id: "Passive_Skill_Poisoned_Claws")
        {
            PercentFromDamage = percentFormDamageFromDamage;
            PoisonDuration = poisonDuration;
            _damageOverTurnEffect = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison, 999,PercentFromDamage);
        }

        public int PoisonDuration { get; }
        public float PercentFromDamage { get; }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (Owner == null) return;
            if (obj.Context.Result is not AttackResults.Succeed) return;
            var target = obj.Context.Target;
            float damage = obj.Context.FinalDamage;
            var poison = _damageOverTurnEffect.Copy();
            poison.Apply(new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = InstanceId });
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PoisonedClaws(PercentFromDamage, PoisonDuration);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PoisonedClaws claws) return false;

            return claws.PercentFromDamage > PercentFromDamage;
        }
    }
}
