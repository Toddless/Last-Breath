namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events;

    public class RegenerationPassiveSkill(float percentFromMaxHealth = 0.05f)
        : Skill(id: "Passive_Skill_Regeneration")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PercentFromMaxHealth)] = PercentFromMaxHealth
                };
                return field;
            }
        }


        private float PercentFromMaxHealth { get; } = percentFromMaxHealth;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent evnt)
        {
            float healAmount = Owner?.Parameters.MaxHealth * PercentFromMaxHealth ?? 0f;
            Owner?.Heal(new HealContext(Owner, Owner) { Amount = healAmount });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            Owner = null;
        }

        public override ISkill Copy() => new RegenerationPassiveSkill(PercentFromMaxHealth);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RegenerationPassiveSkill regenerationPassiveSkill) return false;

            return PercentFromMaxHealth > regenerationPassiveSkill.PercentFromMaxHealth;
        }
    }
}
