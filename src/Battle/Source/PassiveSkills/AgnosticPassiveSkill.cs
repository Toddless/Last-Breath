namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Агностик": the mana pool becomes health, and with no mana left to spend every cast is paid
    /// with health instead — at a surcharge. The conversion is the family's; what this passive adds is the
    /// price of living without mana.</summary>
    public sealed class AgnosticPassiveSkill : PoolConversionPassiveSkill
    {
        public const string PassiveId = "Passive_Skill_Agnostic";

        private readonly CostTypeActivationContextModifier _paidWithHealth = new(Costs.Health);
        private readonly CostScaleActivationContextModifier _surcharge;

        protected override IReadOnlyDictionary<string, object?> DescriptionValues { get; }

        /// <summary>How much dearer a cast is with no mana behind it (0.25 = +25%).</summary>
        public float CostScale { get; }

        public AgnosticPassiveSkill(float costScale) : base(PassiveId, EntityParameter.Mana, EntityParameter.Health)
        {
            CostScale = costScale;
            _surcharge = new CostScaleActivationContextModifier(1 + costScale);
            DescriptionValues = new Dictionary<string, object?> { [nameof(CostScale)] = costScale };
        }

        public override void Attach(IFightable owner)
        {
            base.Attach(owner);
            owner.ModifierHandler.Add(_paidWithHealth);
            owner.ModifierHandler.Add(_surcharge);
        }

        public override void Detach(IFightable owner)
        {
            owner.ModifierHandler.Remove(_paidWithHealth);
            owner.ModifierHandler.Remove(_surcharge);
            base.Detach(owner);
        }

        public override ISkill Copy() => new AgnosticPassiveSkill(CostScale);

        /// <summary>Both registrations convert the whole pool, so the cheaper surcharge is the better one.</summary>
        public override bool IsStronger(ISkill skill) => skill is AgnosticPassiveSkill other && CostScale < other.CostScale;
    }
}
