namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class PorcupinePassiveSkill(
        string id,
        float percentFromTakenDamageToBeReturned,
        float percentArmorToDealAsDamage)
        : Skill(id)
    {
        public float PercentToReturn { get; } = percentFromTakenDamageToBeReturned;
        public float PercentArmorToDealAsDamage { get; } = percentArmorToDealAsDamage;

        public override void Attach(IEntity owner)
        {
            owner.CombatEvents.Subscribe<DamageTakenEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(DamageTakenEvent evnt)
        {
            var owner = evnt.Source;
            var attacker = evnt.Context.Attacker;
            float armorAsDamage = owner.Parameters.Armor * PercentArmorToDealAsDamage;
            float fromDamageTaken = evnt.Context.FinalDamage * PercentToReturn;
            attacker.TakeDamage(armorAsDamage + fromDamageTaken, DamageType.Normal, DamageSource.Passive);
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<DamageTakenEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new PorcupinePassiveSkill(Id, PercentToReturn, PercentArmorToDealAsDamage);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PorcupinePassiveSkill porcupine) return false;
            return PercentToReturn > porcupine.PercentToReturn;
        }
    }
}
