namespace Battle.Source.PassiveSkills
{
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Effects;

    /// <summary>Item-grant passive (Righteous Wrath): attacks apply a burning stack to undead targets,
    /// ticking for a share of the attack damage.
    /// TODO (отложено): при достижении 9 стаков горения накладывать «Сожжение» на 2 хода.</summary>
    public class RighteousWrathPassiveSkill(float percentFromDamage, int duration, int stackThreshold, int incinerationDuration)
        : Skill(id: "Passive_Skill_Undead_Burning")
    {
        public float PercentFromDamage { get; } = percentFromDamage;
        public int Duration { get; } = duration;
        public int StackThreshold { get; } = stackThreshold;
        public int IncinerationDuration { get; } = incinerationDuration;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || evt.Context.Result is not AttackResults.Succeed) return;
            if (evt.Context.Target is not INpc { Fraction: Fractions.Undead }) return;
            var context = evt.Context;
            var burning = new DamageOverTurnEffect(Duration, StatusEffects.Burning, 999, PercentFromDamage);
            _ = burning.Apply(new EffectApplyingContext
            {
                Caster = Owner,
                Target = context.Target,
                Damage = context.BaseDamage, // weapon-side damage, before crits and reductions
                Source = InstanceId
            });

            if (context.Target.Effects.GetBy(x => x.Status == StatusEffects.Burning).Count() < StackThreshold) return;
            _ = new Incineration(IncinerationDuration).Apply(new EffectApplyingContext { Target = context.Target, Source = InstanceId });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new RighteousWrathPassiveSkill(PercentFromDamage, Duration, StackThreshold, IncinerationDuration);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RighteousWrathPassiveSkill other) return false;
            return PercentFromDamage > other.PercentFromDamage;
        }
    }
}
