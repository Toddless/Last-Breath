namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;
    using Effects;

    public class BleedingPassiveSkill : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

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

        public override void Attach(IEntity owner)
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

        public override void Detach(IEntity owner)
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
