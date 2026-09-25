namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;

    public class SoulDevouringPassiveSkill(float barrierRecoveryAmount)
        : Skill(id: "Passive_Skill_Soul_Devouring")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(BarrierRecoveryAmount)] = BarrierRecoveryAmount,
                };
                return field;
            }
        }

        public float BarrierRecoveryAmount { get; } = barrierRecoveryAmount;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AbilityActivationEvent>(OnAbilityActivatedEvent);
        }

        private void OnAbilityActivatedEvent(AbilityActivationEvent evnt)
        {
            Owner?.CurrentBarrier += BarrierRecoveryAmount;
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivationEvent>(OnAbilityActivatedEvent);
            Owner = null;
        }

        public override ISkill Copy() => new SoulDevouringPassiveSkill(BarrierRecoveryAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not SoulDevouringPassiveSkill soul) return false;

            return BarrierRecoveryAmount > soul.BarrierRecoveryAmount;
        }
    }
}
