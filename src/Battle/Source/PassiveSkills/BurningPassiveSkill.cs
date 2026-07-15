namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    public class BurningPassiveSkill : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        public BurningPassiveSkill(float percentFromDamage, int burningDuration, int burningStacks) : base(id: "Passive_Skill_Burning")
        {
            PercentFromDamage = percentFromDamage;
            BurningDuration = burningDuration;
            BurningStacks = burningStacks;
            _damageOverTurnEffect = new DamageOverTurnEffect(BurningDuration, StatusEffects.Burning,BurningStacks, PercentFromDamage);
        }

        public float PercentFromDamage { get; }
        public int BurningDuration { get; }
        public int BurningStacks { get; }

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
            var burning = _damageOverTurnEffect.Copy();
            var applyContext = new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = InstanceId };
            burning.Apply(applyContext);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new BurningPassiveSkill(PercentFromDamage, BurningDuration, BurningStacks);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not BurningPassiveSkill burning) return false;
            return burning.PercentFromDamage > PercentFromDamage;
        }
    }
}
