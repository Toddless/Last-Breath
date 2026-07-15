namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>Item-grant passive: the owner's hits can never be critical.
    /// An Override-to-zero decorator on CriticalChance covers every crit roll
    /// (base attacks, attack series, multicast volleys) since they all read the parameter.</summary>
    public class NoCriticalHitsPassiveSkill()
        : Skill(id: "Passive_Skill_No_Critical_Hits")
    {
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.Parameters.AddModuleDecorator(
                new EntityParameterDecorator(InstanceId, 0f, OperationType.Override, EntityParameter.CriticalChance, Priority.Absolute));
        }

        public override void Detach(IFightable owner)
        {
            owner.Parameters.RemoveModuleDecorator(InstanceId, EntityParameter.CriticalChance);
            Owner = null;
        }

        public override ISkill Copy() => new NoCriticalHitsPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}
