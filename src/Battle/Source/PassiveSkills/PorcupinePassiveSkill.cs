namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class PorcupinePassiveSkill(
        float percentFromTakenDamageToBeReturned,
        float percentArmorToDealAsDamage)
        : Skill(id: "Passive_Skill_Porcupine")
    {
        public float PercentToReturn { get; } = percentFromTakenDamageToBeReturned;
        public float PercentArmorToDealAsDamage { get; } = percentArmorToDealAsDamage;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<DamageTakenEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(DamageTakenEvent evnt)
        {
            if (Owner == null) return;
            var attacker = evnt.From;
            float armorAsDamage = Owner.Parameters.Armor * PercentArmorToDealAsDamage;
            float fromDamageTaken = evnt.Damage * PercentToReturn;
            attacker.TakeDamage(attacker, armorAsDamage + fromDamageTaken, DamageType.Normal, DamageSource.Passive);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<DamageTakenEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PorcupinePassiveSkill(PercentToReturn, PercentArmorToDealAsDamage);
        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PorcupinePassiveSkill porcupine) return false;
            return PercentToReturn > porcupine.PercentToReturn;
        }
    }
}
