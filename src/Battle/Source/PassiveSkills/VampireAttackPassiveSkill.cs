namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events;

    public class VampireAttackPassiveSkill(float leachPercent)
        : Skill(id: "Passive_Skill_Vampire")
    {

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(LeachPercent)] = LeachPercent
                };
                return field;
            }
        }

        public float LeachPercent { get; } = leachPercent;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            if (Owner == null) return;
            float leeched = evnt.Context.FinalDamage * LeachPercent;
            Owner.Heal(new HealContext(Owner, Owner) { Amount = leeched });
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new VampireAttackPassiveSkill(LeachPercent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not VampireAttackPassiveSkill vampire) return false;

            return vampire.LeachPercent > LeachPercent;
        }
    }
}
