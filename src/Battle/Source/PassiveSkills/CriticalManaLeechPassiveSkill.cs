namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;

    /// <summary>Item-grant passive (Creator's Ring): critical hits restore mana for a share of the damage dealt.</summary>
    public class CriticalManaLeechPassiveSkill(float percent)
        : Skill(id: "Passive_Skill_Critical_Mana_Leech")
    {
        public float Percent { get; } = percent;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || !evt.Context.IsCritical || evt.Context.Result is not AttackResults.Succeed) return;
            Owner.RestoreMana(new ManaRecoveryContext(Owner, Owner) { Amount = evt.Context.FinalDamage * Percent });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new CriticalManaLeechPassiveSkill(Percent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CriticalManaLeechPassiveSkill other) return false;
            return Percent > other.Percent;
        }
    }
}
