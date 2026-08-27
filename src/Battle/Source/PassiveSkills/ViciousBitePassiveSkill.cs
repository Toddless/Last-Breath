namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>"Яростный укус": poison hits harder for every unit of poison resistance the bearer carries
    /// ABOVE his own maximum, and his poison resistance is cut in return.
    /// <para>The two halves are one bargain: the cut lands on the total, so it eats the very reserve the
    /// bonus is paid for. A resistance merely at the cap is left with nothing over it and the keystone pays
    /// nothing — the reserve has to be overfed before it is worth anything.</para>
    /// <para>The reserve is the one <see cref="Core.Entity.Components.ResistanceParameters"/> counts, so the
    /// keystone and the shred that bites into it are reading one figure and never two.</para></summary>
    public class ViciousBitePassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Vicious_Bite";

        /// <summary>The unit the design counts resistance in — one percentage point, as the fraction it is
        /// stored as. The field is written per that unit, the modifier is worth its value per one whole one.</summary>
        private const float ResistancePoint = 0.01f;

        private readonly IModifierInstance _penalty;
        private readonly IModifierInstance _bonus;

        protected override IReadOnlyDictionary<string, object?> DescriptionValues { get; }

        /// <summary>Poison damage multiplier bought by one point of reserve (0.01 = +1%).</summary>
        public float PerOvercap { get; }

        /// <summary>What the bargain costs, as a multiplicative share of the poison resistance (−0.6 = −60%).
        /// <para>The card states that price as a MAGNITUDE — the sentence around it already carries the
        /// direction ("60% less") — and the turning-around belongs to <see cref="PassiveDisplayValues"/>,
        /// where the card and the authoring tool both read it from. A second spelling of it here would be
        /// the same arithmetic in two places, which is how the two readings drift apart.</para></summary>
        public float ResistancePenalty { get; }

        public ViciousBitePassiveSkill(float perOvercap, float resistancePenalty) : base(PassiveId)
        {
            PerOvercap = perOvercap;
            ResistancePenalty = resistancePenalty;
            _penalty = new SimpleModifier(EntityParameter.PoisonResistance, ModifierValueType.Multiplicative, resistancePenalty, PassiveId);
            _bonus = new ScaledByOvercapModifier(ModifierValueType.Flat, EntityParameter.PoisonDamageMultiplier,
                EntityParameter.PoisonResistance, perOvercap / ResistancePoint, condition: null, PassiveId);
            DescriptionValues = PassiveDisplayValues.Of(PassiveId, new Dictionary<string, float>
            {
                ["perOvercap"] = perOvercap,
                ["resistancePenalty"] = resistancePenalty
            });
        }

        /// <summary>The price is written on first so the reserve is measured off the total the bearer will
        /// actually carry; the measure is live either way, and this only spares it one extra reading.</summary>
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _penalty.ApplyTo(owner);
            _bonus.ApplyTo(owner);
        }

        /// <summary>Leaves nothing behind: both halves come off the owner's list and the scaled one stops
        /// watching the resistance and the maximum it was measured by.</summary>
        public override void Detach(IFightable owner)
        {
            _bonus.RemoveFrom(owner);
            _penalty.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new ViciousBitePassiveSkill(PerOvercap, ResistancePenalty);

        /// <summary>A registration is judged by the whole bargain and not by half of it: the one that pays
        /// more per point of reserve is the better, and where both pay the same, the one that charges less
        /// for it. Ranking on the bonus alone would let a harsher price take the slot for free.</summary>
        public override bool IsStronger(ISkill skill) =>
            skill is ViciousBitePassiveSkill other
            && (PerOvercap > other.PerOvercap
                || (PerOvercap.Equals(other.PerOvercap) && ResistancePenalty > other.ResistancePenalty));
    }
}
