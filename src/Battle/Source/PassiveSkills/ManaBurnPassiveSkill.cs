namespace Battle.Source.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class ManaBurnPassiveSkill(float percentToBurn)
        : Skill(id: "Passive_Skill_Mana_Burn")
    {
        public float PercentToBurn { get; } = percentToBurn;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(OnBeforeAttack);
        }

        private void OnBeforeAttack(BeforeAttackEvent obj)
        {
            var target = obj.Context.Target;
            float toBurn = target.CurrentMana * PercentToBurn;
            target.CurrentMana -= toBurn;
            obj.Context.AdditionalDamage += toBurn;
        }

        public override void Detach(IEntity owner)
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
