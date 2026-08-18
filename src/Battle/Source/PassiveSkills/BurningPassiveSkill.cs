namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    public class BurningPassiveSkill : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PercentFromDamage)] = PercentFromDamage, [nameof(BurningStacks)] = BurningStacks, [nameof(BurningDuration)] = BurningDuration,
                };
                return field;
            }
        }

        public BurningPassiveSkill(float percentFromDamage, int burningDuration, int burningStacks) : base(id: "Passive_Skill_Burning")
        {
            PercentFromDamage = percentFromDamage;
            BurningDuration = burningDuration;
            BurningStacks = burningStacks;
            _damageOverTurnEffect = new DamageOverTurnEffect(BurningDuration, StatusEffects.Burning, BurningStacks, PercentFromDamage);
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
            var target = context.Target;
            var burning = _damageOverTurnEffect.Copy();
            // A rolled item effect rather than a fire spell: it burns for a share of the WHOLE blow,
            // whatever the blow was made of, and the hit itself is left the kind it was.
            var applyContext = new EffectApplyingContext
            {
                Caster = Owner,
                Target = target,
                Damage = context.FinalDamage,
                PoolFromWholeHit = true,
                Source = InstanceId
            };
            burning.Apply(applyContext);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new BurningPassiveSkill(PercentFromDamage, BurningDuration, BurningStacks);

        /// <summary>Strength is the tick size: duration and stacks only spread the same
        /// <see cref="PercentFromDamage"/> share of the hit over more turns.</summary>
        public override bool IsStronger(ISkill skill)
        {
            if (skill is not BurningPassiveSkill burning) return false;
            return PercentFromDamage > burning.PercentFromDamage;
        }
    }
}
