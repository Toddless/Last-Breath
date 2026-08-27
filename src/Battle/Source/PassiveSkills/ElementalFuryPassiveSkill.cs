namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Context;

    /// <summary>"Ярость стихий": the bearer's attacks pierce an elemental resistance by exactly the reserve
    /// he himself carries above his own cap for that element, and every resistance he owns is cut in return.
    /// <para>Element by element and never pooled: a cold reserve pierces cold and nothing else, so an
    /// element left under its cap is pierced by nothing at all.</para>
    /// <para>The two halves are one bargain: the cut lands on the totals, so it eats the very reserves the
    /// penetration is measured from. The reserve is the one <see cref="ResistanceParameters"/> counts, so
    /// this keystone, resistance shred and the keystone paid per unit of reserve are reading one figure.</para>
    /// <para>Measured on the blow rather than at attach: an essence, a shred or the price itself moves a
    /// total, and what the next attack pierces moves with it.</para></summary>
    public class ElementalFuryPassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Elemental_Fury";

        private readonly IModifierInstance _penalty;

        /// <summary>Owner-gated, so it can only be built once there is an owner to gate it on.</summary>
        private OvercapPenetration? _penetration;

        protected override IReadOnlyDictionary<string, object?> DescriptionValues { get; }

        /// <summary>What the bargain costs, as a multiplicative share of every resistance (−0.45 = −45%).</summary>
        public float ResistancePenalty { get; }

        public ElementalFuryPassiveSkill(float resistancePenalty) : base(PassiveId)
        {
            ResistancePenalty = resistancePenalty;
            _penalty = new SimpleModifier(EntityParameter.AllResistance, ModifierValueType.Multiplicative, resistancePenalty, PassiveId);
            DescriptionValues = new Dictionary<string, object?> { [nameof(ResistancePenalty)] = resistancePenalty };
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _penalty.ApplyTo(owner);
            _penetration = new OvercapPenetration(owner);
            owner.ModifierHandler.Add(_penetration);
        }

        /// <summary>Leaves nothing behind: the resistances come back whole and attacks stop piercing the
        /// moment the keystone is refunded or suppressed.</summary>
        public override void Detach(IFightable owner)
        {
            if (_penetration != null) owner.ModifierHandler.Remove(_penetration);
            _penetration = null;
            _penalty.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new ElementalFuryPassiveSkill(ResistancePenalty);

        /// <summary>What is pierced is the same rule for every registration — the reserve, one for one — so
        /// only the price tells two of them apart, and the one charging less for it is the better.</summary>
        public override bool IsStronger(ISkill skill) =>
            skill is ElementalFuryPassiveSkill other && ResistancePenalty > other.ResistancePenalty;

        /// <summary>Outgoing-attack rule: every element is pierced by the owner's own reserve above his cap
        /// for it, each into the channel of that element and no other. The figure is added to the hit rather
        /// than written on the fighter, so it reaches attacks alone and leaves what he casts untouched.</summary>
        private sealed class OvercapPenetration(IFightable owner)
            : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Overcap_Penetration"), IDamageModifier
        {
            public void Apply(IDamageContext context)
            {
                if (!context.Source.IsSame(owner.InstanceId)) return;
                if (context.Cause != DamageCause.Attack) return;

                foreach ((EntityParameter resistance, EntityParameter penetration) in Calculations.ElementalChannels.Values)
                    context.AddResistancePenetration(penetration, ResistanceParameters.Overcap(owner.Parameters, resistance));
            }
        }
    }
}
