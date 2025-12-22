namespace Battle.Source.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class SoulDevouringPassiveSkill(float barrierRecoveryAmount)
        : Skill(id: "Passive_Skill_SoulDevouring")
    {
        public float BarrierRecoveryAmount { get; } = barrierRecoveryAmount;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AbilityActivatedGameEvent>(OnAbilityActivatedEvent);
        }

        private void OnAbilityActivatedEvent(AbilityActivatedGameEvent evnt)
        {
            Owner?.CurrentBarrier += BarrierRecoveryAmount;
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivatedGameEvent>(OnAbilityActivatedEvent);
            Owner = null;
        }

        public override ISkill Copy() => new SoulDevouringPassiveSkill(BarrierRecoveryAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not SoulDevouringPassiveSkill soul) return false;

            return soul.BarrierRecoveryAmount > BarrierRecoveryAmount;
        }
    }
}
