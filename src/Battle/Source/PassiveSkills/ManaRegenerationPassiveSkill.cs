namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events;

    /// <summary>Item-grant passive (Mana Flow): restores a percentage of max mana at the end of the owner's turn.</summary>
    public class ManaRegenerationPassiveSkill(float percentFromMaxMana)
        : Skill(id: "Passive_Skill_Mana_Regeneration")
    {
        public float PercentFromMaxMana { get; } = percentFromMaxMana;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent evt)
        {
            if (Owner == null) return;
            Owner.RestoreMana(new ManaRecoveryContext(Owner, Owner) { Amount = Owner.Parameters.MaxMana * PercentFromMaxMana });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            Owner = null;
        }

        public override ISkill Copy() => new ManaRegenerationPassiveSkill(PercentFromMaxMana);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ManaRegenerationPassiveSkill other) return false;
            return PercentFromMaxMana > other.PercentFromMaxMana;
        }
    }
}
