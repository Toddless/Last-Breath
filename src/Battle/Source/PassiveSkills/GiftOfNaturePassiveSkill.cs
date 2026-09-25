namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Context;

    /// <summary>"Дар природы": every elemental damage of the bearer is worth more, and his attacks stop
    /// dealing physical damage at all.
    /// <para>The bonus is a line on the elemental bucket, so it reaches fire, cold and lightning at once and
    /// keeps reaching whatever is added to that family later.</para>
    /// <para>The price is paid on the blow and not on the parameter: physical damage stays the number it was,
    /// so everything counted per point of it keeps paying and abilities that spend it keep spending it — only
    /// what is still physical when an attack lands is lost. Conversions run first, so physical a mythic mark
    /// has already turned into an element arrives as that element and is kept — and any conversion added
    /// later keeps that place by taking <see cref="ContextModifierPriority.Absolute"/> like the rest of
    /// them.</para>
    /// <para>The same holds for what is merely MEASURED by the doomed part: an added elemental share
    /// (<see cref="Core.Modifiers.Context.AddedElementalDamageContextModifier"/>, Late) counts the physical
    /// still standing at that moment and keeps everything it adds. Everything paid per point of physical
    /// damage goes on being paid.</para></summary>
    public class GiftOfNaturePassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Gift_Of_Nature";

        private readonly IModifierInstance _elemental;

        /// <summary>Owner-gated, so it can only be built once there is an owner to gate it on.</summary>
        private IDamageModifier? _noPhysical;

        protected override IReadOnlyDictionary<string, object?> DescriptionValues { get; }

        /// <summary>What every elemental damage is multiplied by, as the share it adds (0.35 = +35%).</summary>
        public float ElementalBonus { get; }

        public GiftOfNaturePassiveSkill(float elementalBonus) : base(PassiveId)
        {
            ElementalBonus = elementalBonus;
            _elemental = new SimpleModifier(EntityParameter.AllElementalDamage, ModifierValueType.Multiplicative, elementalBonus, PassiveId);
            DescriptionValues = new Dictionary<string, object?> { [nameof(ElementalBonus)] = elementalBonus };
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _elemental.ApplyTo(owner);
            _noPhysical = new DamageTypeDenialContextModifier(owner, DamageType.Physical, DamageCause.Attack);
            owner.ModifierHandler.Add(_noPhysical);
        }

        /// <summary>Leaves nothing behind: the elemental line comes off the owner's list and his attacks
        /// carry their physical part again the moment the keystone is refunded or suppressed.</summary>
        public override void Detach(IFightable owner)
        {
            if (_noPhysical != null) owner.ModifierHandler.Remove(_noPhysical);
            _noPhysical = null;
            _elemental.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new GiftOfNaturePassiveSkill(ElementalBonus);

        /// <summary>The price is the same for everyone — an attack deals no physical damage or it does —
        /// so the registration paying more for the elements is the better one.</summary>
        public override bool IsStronger(ISkill skill) =>
            skill is GiftOfNaturePassiveSkill other && ElementalBonus > other.ElementalBonus;
    }
}
