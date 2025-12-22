namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Abilities.Effects;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    public class BurningPassiveSkill : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        public BurningPassiveSkill(float percentFromDamage, int burningDuration, int burningStacks) : base(id: "Passive_Skill_Burning")
        {
            PercentFromDamage = percentFromDamage;
            BurningDuration = burningDuration;
            BurningStacks = burningStacks;
            _damageOverTurnEffect = new DamageOverTurnEffect(BurningDuration, BurningStacks, PercentFromDamage, StatusEffects.Burning);
        }

        public float PercentFromDamage { get; }
        public int BurningDuration { get; }
        public int BurningStacks { get; }

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (Owner == null) return;
            var context = obj.Context;
            float damage = context.FinalDamage;
            var target = context.Target;
            var burning = _damageOverTurnEffect.Clone();
            var applyContext = new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = InstanceId };
            burning.Apply(applyContext);
        }

        public override void Detach(IEntity owner)
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
