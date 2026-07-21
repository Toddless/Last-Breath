namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    public class BleedingPassiveSkill : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PercentFromDamage)] = PercentFromDamage,
                    [nameof(MaxStack)] = MaxStack,
                    [nameof(BleedDuration)] = BleedDuration,
                };
                return field;
            }
        }

        public BleedingPassiveSkill(float percentFromDamage,
            int bleedDuration,
            int maxStack) : base(id: "Passive_Skill_Bleeding")
        {
            PercentFromDamage = percentFromDamage;
            MaxStack = maxStack;
            BleedDuration = bleedDuration;
            _damageOverTurnEffect = new DamageOverTurnEffect(BleedDuration, StatusEffects.Bleed, MaxStack, PercentFromDamage);
        }

        public float PercentFromDamage { get; }
        public int MaxStack { get; }
        public int BleedDuration { get; }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (Owner == null || obj.Context.Result is not AttackResults.Succeed) return;
            var context = obj.Context;
            float damage = context.FinalDamage;
            var target = context.Target;
            var bleed = _damageOverTurnEffect.Copy();
            var applyContext = new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = InstanceId };
            bleed.Apply(applyContext);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new BleedingPassiveSkill(PercentFromDamage, BleedDuration, MaxStack);


        public override bool IsStronger(ISkill skill)
        {
            if (skill is not BleedingPassiveSkill bleed) return false;

            return BleedDuration > bleed.BleedDuration && MaxStack > bleed.MaxStack;
        }
    }
}
