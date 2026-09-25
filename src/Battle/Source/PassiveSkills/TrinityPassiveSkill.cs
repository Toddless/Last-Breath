namespace Battle.Source.PassiveSkills
{
    using Core;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Триединство": the bearer's attacks carry no physical part of their own — all of it lands as
    /// cold, fire and lightning in equal thirds.
    /// <para>A conversion and not an addition: the physical component is spent rather than copied, so a blow
    /// is worth exactly what it was and only what it is made of changes.</para>
    /// <para>The parameter is untouched — physical damage stays the number it was, so everything counted per
    /// point of it keeps being paid and abilities that spend it keep spending it.</para>
    /// <para>It takes what is LEFT of the physical part rather than a share of it, which is the whole of why
    /// it stands at <see cref="ContextModifierPriority.Remainder"/>: a mythic mark converting a share into
    /// its element is owed that share and takes it first, and the three thirds are cut from the remainder.
    /// Past it stands only <see cref="ContextModifierPriority.Denial"/>, so a keystone that ENDS the physical
    /// part finds it already spent and nothing of the blow is lost to it.</para></summary>
    public class TrinityPassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Trinity";

        /// <summary>Owner-gated, so it can only be built once there is an owner to gate it on.</summary>
        private PhysicalIntoElements? _split;

        public TrinityPassiveSkill() : base(PassiveId)
        {
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _split = new PhysicalIntoElements(owner);
            owner.ModifierHandler.Add(_split);
        }

        /// <summary>Leaves nothing behind: attacks carry their physical part again the moment the keystone
        /// is refunded or suppressed.</summary>
        public override void Detach(IFightable owner)
        {
            if (_split != null) owner.ModifierHandler.Remove(_split);
            _split = null;
            Owner = null;
        }

        public override ISkill Copy() => new TrinityPassiveSkill();

        /// <summary>Numberless: the whole physical part goes and the three elements share it evenly, so no
        /// registration can be worth more than another.</summary>
        public override bool IsStronger(ISkill skill) => false;

        /// <summary>Outgoing-attack rule: whatever is still physical is split evenly between the elements.</summary>
        private sealed class PhysicalIntoElements(IFightable owner)
            : ContextModifier(priority: ContextModifierPriority.Remainder, id: "Context_Modifier_Trinity"), IDamageModifier
        {
            /// <summary>The elements as the mitigation rules name them — the keystone splits into whatever
            /// they are rather than into a list of its own.</summary>
            private static readonly DamageType[] s_elements = [.. Calculations.ElementalChannels.Keys];

            public void Apply(IDamageContext context)
            {
                if (!context.Source.IsSame(owner.InstanceId)) return;
                if (context.Cause != DamageCause.Attack) return;
                if (!context.DamageComponents.TryGetValue(DamageType.Physical, out float physical) || physical <= 0) return;

                // Each element takes an even share of what is STILL physical, so the last of them empties
                // the component and every one is worth the same third of what stood there when it began.
                for (int index = 0; index < s_elements.Length; index++)
                    context.Convert(DamageType.Physical, s_elements[index], 1f / (s_elements.Length - index));
            }
        }
    }
}
