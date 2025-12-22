namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Abilities.Effects;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    public class PoisonedClaws : Skill
    {
        private readonly DamageOverTurnEffect _damageOverTurnEffect;

        public PoisonedClaws(float percentFormDamageToDealAsPoison, int poisonDuration, int stacks)
            : base(id: "Passive_Skill_Poisoned_Claws")
        {
            PercentToDealAsPoison = percentFormDamageToDealAsPoison;
            PoisonDuration = poisonDuration;
            PoisonStacks = stacks;
            _damageOverTurnEffect = new DamageOverTurnEffect(PoisonDuration, PoisonStacks, PercentToDealAsPoison, StatusEffects.Poison);
        }

        public int PoisonDuration { get; }
        public int PoisonStacks { get; }
        public float PercentToDealAsPoison { get; }

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (Owner == null) return;
            var target = obj.Context.Target;
            float damage = obj.Context.FinalDamage;
            var poison = _damageOverTurnEffect.Clone();
            poison.Apply(new EffectApplyingContext { Caster = Owner, Target = target, Damage = damage, Source = InstanceId });
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PoisonedClaws(PercentToDealAsPoison, PoisonDuration, PoisonStacks);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PoisonedClaws claws) return false;

            return claws.PercentToDealAsPoison > PercentToDealAsPoison;
        }
    }
}
