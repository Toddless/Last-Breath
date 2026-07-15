namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    public class PorcupinePassiveSkill(
        float damagePercentFromTakenDamageToBeReturned,
        float additionalDamageFromArmor)
        : Skill(id: "Passive_Skill_Porcupine")
    {
        public float DamagePercentToReturn { get; } = damagePercentFromTakenDamageToBeReturned;
        public float AdditionalDamageFromArmor { get; } = additionalDamageFromArmor;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<DamageTakenEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(DamageTakenEvent evnt)
        {
            if (Owner == null) return;

            var attacker = evnt.Context.Source;
            float armorAsDamage = Owner.Parameters.Armor * AdditionalDamageFromArmor;
            float fromDamageTaken = evnt.Context.TotalDamage * DamagePercentToReturn;
            var context = new DamageContext { Source = attacker, Cause = DamageCause.Passive };
            context.Add(DamageType.Pure, armorAsDamage + fromDamageTaken);
            attacker.TakeDamage(context);
        }

        public override void Detach(IFightable owner)
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
