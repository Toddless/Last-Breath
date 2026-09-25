namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>Item-grant passive (Goliath's Sign): heals a percentage of CURRENT health at the end of the owner's turn
    /// (unlike <see cref="RegenerationPassiveSkill"/>, which scales off max health).</summary>
    public class CurrentHealthRegenerationPassiveSkill(float percentFromCurrentHealth)
        : Skill(id: "Passive_Skill_Current_Health_Regeneration")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PercentFromCurrentHealth)] = PercentFromCurrentHealth
                };
                return field;
            }
        }

        public float PercentFromCurrentHealth { get; } = percentFromCurrentHealth;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent evt)
        {
            if (Owner == null) return;
            float amount = Owner.CurrentHealth * PercentFromCurrentHealth;
            Owner.Heal(new HealContext(Owner, Owner) { Amount = amount, Cause = RecoveryCause.Regen });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            Owner = null;
        }

        public override ISkill Copy() => new CurrentHealthRegenerationPassiveSkill(PercentFromCurrentHealth);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CurrentHealthRegenerationPassiveSkill other) return false;
            return PercentFromCurrentHealth > other.PercentFromCurrentHealth;
        }
    }
}
