namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    public class PorcupinePassiveSkill(
        float damagePercentFromTakenDamageToBeReturned,
        float armorAsDamage)
        : Skill(id: "Passive_Skill_Porcupine")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(DamageReturn)] = DamageReturn,
                    [nameof(ArmorAsDamage)] = ArmorAsDamage
                };
                return field;
            }
        }

        public float DamageReturn { get; } = damagePercentFromTakenDamageToBeReturned;
        public float ArmorAsDamage { get; } = armorAsDamage;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<DamageTakenEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(DamageTakenEvent evnt)
        {
            if (Owner == null) return;

            var attacker = evnt.Context.Source;
            float armorAsDamage = Owner.Parameters.Armor * ArmorAsDamage;
            float fromDamageTaken = evnt.Context.TotalDamage * DamageReturn;
            var context = new DamageContext { Source = attacker, Cause = DamageCause.Passive };
            context.Add(DamageType.Pure, armorAsDamage + fromDamageTaken);
            attacker.TakeDamage(context);
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<DamageTakenEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new PorcupinePassiveSkill(DamageReturn, ArmorAsDamage);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not PorcupinePassiveSkill porcupine) return false;
            return DamageReturn > porcupine.DamageReturn;
        }
    }
}
