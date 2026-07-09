namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Modifiers.Context;
    using Core.Entity;
    using Core.Enums;

    /// <summary>Item-grant passive: all heals received by the owner are increased by <paramref name="bonus"/>.</summary>
    public class HealingEfficiencyPassiveSkill(float bonus)
        : Skill(id: "Passive_Skill_Healing_Efficiency")
    {
        private readonly HealingBonusContextModifier _contextModifier = new(bonus);

        public float Bonus { get; } = bonus;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.ModifierHandler.Add(_contextModifier);
        }

        public override void Detach(IFightable owner)
        {
            owner.ModifierHandler.Remove(_contextModifier);
            Owner = null;
        }

        public override ISkill Copy() => new HealingEfficiencyPassiveSkill(Bonus);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not HealingEfficiencyPassiveSkill other) return false;
            return Bonus > other.Bonus;
        }
    }
}
