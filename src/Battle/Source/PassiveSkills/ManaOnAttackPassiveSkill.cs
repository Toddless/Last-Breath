namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events.GameEvents;

    /// <summary>Item-grant passive (Righteous Wrath): restores a flat amount of mana on every attack.</summary>
    public class ManaOnAttackPassiveSkill(float amount)
        : Skill(id: "Passive_Skill_Mana_On_Attack")
    {
        public float Amount { get; } = amount;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null) return;
            Owner.RestoreMana(new ManaRecoveryContext(Owner, Owner) { Amount = Amount });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new ManaOnAttackPassiveSkill(Amount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ManaOnAttackPassiveSkill other) return false;
            return Amount > other.Amount;
        }
    }
}
