namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class PorcupinePassiveSkill(
        float damagePercentFromTakenDamageToBeReturned,
        float additionalDamageFromArmor)
        : Skill(id: "Passive_Skill_Porcupine")
    {
        public float DamagePercentToReturn { get; } = damagePercentFromTakenDamageToBeReturned;
        public float AdditionalDamageFromArmor { get; } = additionalDamageFromArmor;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<DamageTakenEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(DamageTakenEvent evnt)
        {
            if (Owner == null) return;

            var attacker = evnt.Context.Source;
            float armorAsDamage = Owner.Parameters.Armor * AdditionalDamageFromArmor;
            float fromDamageTaken = evnt.Context.Damage * DamagePercentToReturn;
            var context = new DamageContext { Source = attacker, Cause = DamageCause.Passive, Type = DamageType.Normal, Damage = armorAsDamage + fromDamageTaken };
            attacker.TakeDamage(context);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<DamageTakenEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PorcupinePassiveSkill(DamagePercentToReturn, AdditionalDamageFromArmor);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PorcupinePassiveSkill porcupine) return false;
            return DamagePercentToReturn > porcupine.DamagePercentToReturn;
        }
    }
}
