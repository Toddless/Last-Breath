namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class SoulDevouringPassiveSkill(string id, float barrierRecoveryAmount)
        : Skill(id)
    {
        public float BarrierRecoveryAmount { get; } = barrierRecoveryAmount;

        public override void Attach(IEntity owner)
        {
            owner.CombatEvents.Subscribe<AbilityActivatedEvent>(OnAbilityActivatedEvent);
        }

        private void OnAbilityActivatedEvent(AbilityActivatedEvent evnt)
        {
            var owner = evnt.Source;
            owner.CurrentBarrier += BarrierRecoveryAmount;
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AbilityActivatedEvent>(OnAbilityActivatedEvent);
        }

        public override ISkill Copy() => new SoulDevouringPassiveSkill(Id, BarrierRecoveryAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not SoulDevouringPassiveSkill soul) return false;

            return soul.BarrierRecoveryAmount > BarrierRecoveryAmount;
        }
    }
}
