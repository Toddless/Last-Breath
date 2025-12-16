namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class SoulDevouringPassiveSkill(string id, float barrierRecoveryAmount)
        : Skill(id)
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

        public override ISkill Copy() => new SoulDevouringPassiveSkill(Id, BarrierRecoveryAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not SoulDevouringPassiveSkill soul) return false;

            return soul.BarrierRecoveryAmount > BarrierRecoveryAmount;
        }
    }
}
