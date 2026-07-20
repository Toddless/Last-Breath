namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events;
    using Godot;

    public class RegenerationPassiveSkill(float percentFromMaxHealth = 0.05f)
        : Skill(id: "Passive_Skill_Regeneration")
    {
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
            GD.Print($"Entity: {Owner?.DisplayName}, heal {healAmount}");
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
