namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;

    /// <summary>Luck as a standing trait: the owner's critical rolls are made twice and the better draw counts.</summary>
    public class LuckyCriticalChancePassiveSkill() : Skill(id: "Passive_Skill_LuckyCriticalChance")
    {
        public override void Attach(IFightable owner) => owner.Parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);

        public override void Detach(IFightable owner) => owner.Parameters.RemoveChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);

        public override ISkill Copy() => new LuckyCriticalChancePassiveSkill();

        /// <summary>Parameterless: luck is the same mark from every instance, so none can be stronger than another.</summary>
        public override bool IsStronger(ISkill skill) => false;
    }
}
