namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    public class ManaBurnPassiveSkill(float percentToBurn)
        : Skill(id: "Passive_Skill_Mana_Burn")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(PercentToBurn)] = PercentToBurn,
                };
                return field;
            }
        }
        public float PercentToBurn { get; } = percentToBurn;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(OnBeforeAttack);
        }

        private void OnBeforeAttack(BeforeAttackEvent obj)
        {
            var target = obj.Context.Target;
            float toBurn = target.CurrentMana * PercentToBurn;
            target.CurrentMana -= toBurn;
            obj.Context.AddDamage(DamageType.Physical, toBurn);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeAttackEvent>(OnBeforeAttack);
            Owner = null;
        }

        public override ISkill Copy() => new ManaBurnPassiveSkill(PercentToBurn);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ManaBurnPassiveSkill mana) return false;
            return mana.PercentToBurn > PercentToBurn;
        }
    }
}
