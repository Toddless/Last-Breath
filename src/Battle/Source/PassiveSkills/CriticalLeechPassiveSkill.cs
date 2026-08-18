namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>Item-grant passive (Creator's Ring): critical hits heal the owner for a share of the damage dealt.</summary>
    public class CriticalLeechPassiveSkill(float percent)
        : Skill(id: "Passive_Skill_Critical_Leech")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Percent)] = Percent,
                };
                return field;
            }
        }
        public float Percent { get; } = percent;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || !evt.Context.IsCritical || evt.Context.Result is not AttackResults.Succeed) return;
            Owner.Heal(new HealContext(Owner, Owner) { Amount = evt.Context.FinalDamage.Total * Percent, Cause = RecoveryCause.Leech });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new CriticalLeechPassiveSkill(Percent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CriticalLeechPassiveSkill other) return false;
            return Percent > other.Percent;
        }
    }
}
