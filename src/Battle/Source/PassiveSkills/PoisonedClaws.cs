namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;
    using Effects;

    public class PoisonedClaws : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        public PoisonedClaws(float percentFormDamageToDealAsPoison, int poisonDuration)
            : base(id: "Passive_Skill_Poisoned_Claws")
        {
            PercentToDealAsPoison = percentFormDamageToDealAsPoison;
            PoisonDuration = poisonDuration;
            _damageOverTurnEffect = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison, 999,PercentToDealAsPoison);
        }

        public int PoisonDuration { get; }
        public float PercentToDealAsPoison { get; }

        public override void Attach(IEntity owner)
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
            poison.Apply(new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = Id });
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PoisonedClaws(PercentToDealAsPoison, PoisonDuration);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PoisonedClaws claws) return false;

            return claws.PercentToDealAsPoison > PercentToDealAsPoison;
        }
    }
}
