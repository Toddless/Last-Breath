namespace Core.Modifiers
{
    using System;
    using Entity;
    using Entity.Components;
    using Enums;
    using Interfaces;

    /// <summary>A modifier worth its base value FOR EVERY UNIT of resistance its carrier holds ABOVE the cap
    /// ("+1% poison damage multiplier per unit of poison resistance over the maximum") — the twin of
    /// <see cref="ScaledByParameterModifier"/> whose carrier is a reserve rather than a parameter.
    /// <para>The reserve is the one <see cref="ResistanceParameters"/> counts, so a keystone paid for it and
    /// the shred that eats it are reading the same figure. It is taken off the FINAL total, which is what
    /// makes a keystone that cuts the same resistance eat its own carrier.</para>
    /// <para>Two parameters move it — the resistance and the maximum capping it — so both are watched: an
    /// essence that raises the cap shrinks the reserve exactly as a lost resistance line would.</para>
    /// <para>Inert until it is bound: with nobody to measure there is no reserve to read, so an unbound line
    /// contributes nothing rather than its per-unit figure.</para></summary>
    public sealed class ScaledByOvercapModifier : ConditionalModifier
    {
        /// <summary>Below this the reserve is the same number, so the carrier is not asked to resolve again.</summary>
        private const float Tolerance = 0.0001f;

        /// <summary>Latch over the announcement — see <see cref="ScaledByParameterModifier"/>: a resolve this
        /// line asks for can come back around to it, and an announcement from inside one would recurse.</summary>
        private bool _rescaling;

        /// <summary>The resistance whose reserve the value is counted per unit of.</summary>
        public EntityParameter Resistance { get; }

        /// <summary>The maximum capping it, or null for a parameter no cap holds — such a line has no reserve
        /// to be paid for and stays at nothing however the parameter moves.</summary>
        public EntityParameter? Maximum { get; }

        public ScaledByOvercapModifier(
            ModifierValueType valueType,
            EntityParameter parameter,
            EntityParameter resistance,
            float value,
            ICondition? condition,
            string source)
            : base(weight: 0f, valueType, parameter, value, condition, source)
        {
            Resistance = resistance;
            Maximum = ResistanceParameters.MaximumFor(resistance);
            Value = 0f;
        }

        public override IModifierInstance Copy() =>
            new ScaledByOvercapModifier(ModifierValueType, EntityParameter, Resistance, BaseValue, Condition?.Copy(), Source);

        /// <summary>Takes the carrier's reserve and keeps watching both halves of it: the resistance and its
        /// maximum each move the reserve on their own.</summary>
        public override void Bind(IFightable target)
        {
            base.Bind(target);
            target.Parameters.ParameterChanged += OnCarrierParameterChanged;
            Rescale(target.Parameters);
        }

        /// <summary>Lets the carrier go and falls back to nothing — a line nobody carries has no reserve, and
        /// a watch left behind would keep a released fighter answering.</summary>
        public override void Unbind()
        {
            if (Owner is not { } carrier) return;

            carrier.Parameters.ParameterChanged -= OnCarrierParameterChanged;
            base.Unbind();
            Value = 0f;
        }

        /// <summary>Only the two parameters the reserve is made of move this line; every other one is noise.
        /// The announced value is not used: the reserve is a pair, so it is read whole off the carrier.</summary>
        private void OnCarrierParameterChanged(EntityParameter parameter, float value)
        {
            if (parameter != Resistance && parameter != Maximum) return;
            if (Owner is not { } carrier) return;

            Rescale(carrier.Parameters);
        }

        /// <summary>The new measure, announced to the carrier so the parameter it feeds is resolved again —
        /// the line is pulled from a source rather than written onto him, so nothing else would notice.</summary>
        private void Rescale(IEntityParametersComponent parameters)
        {
            float scaled = BaseValue * ResistanceParameters.Overcap(parameters, Resistance);
            if (Math.Abs(Value - scaled) < Tolerance) return;

            Value = scaled;
            if (_rescaling) return;

            _rescaling = true;
            try
            {
                Owner?.ParameterModifiers.RefreshParameter(EntityParameter);
            }
            finally
            {
                _rescaling = false;
            }
        }
    }
}
