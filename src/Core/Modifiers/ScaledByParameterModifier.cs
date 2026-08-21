namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;
    using Interfaces;

    /// <summary>A modifier worth its base value FOR EVERY UNIT of another parameter its carrier holds
    /// ("+1% physical damage per point of Strength"), re-measured whenever that parameter moves.
    /// <para>Inert until it is bound: with nobody to measure there is no scale to read, so an unbound line
    /// contributes nothing rather than its per-unit figure.</para></summary>
    public sealed class ScaledByParameterModifier : ConditionalModifier
    {
        /// <summary>Below this the scale is the same number, so the carrier is not asked to resolve again.</summary>
        private const float Tolerance = 0.0001f;

        /// <summary>Latch over the announcement: a resolve this line asked for can come back around to it
        /// (two lines measuring each other's parameters), and an announcement from inside one would
        /// recurse until the process dies. The measure is still taken — only the second announcement is
        /// dropped, so a mutual pair settles one level deep instead of on the stack.</summary>
        private bool _rescaling;

        /// <summary>Carrier parameter the value is counted per unit of. Never the parameter this line feeds
        /// — that loop is refused where the line is authored.</summary>
        public EntityParameter PerParameter { get; }

        public ScaledByParameterModifier(
            ModifierValueType valueType,
            EntityParameter parameter,
            EntityParameter perParameter,
            float value,
            ICondition? condition,
            string source)
            : base(weight: 0f, valueType, parameter, value, condition, source)
        {
            PerParameter = perParameter;
            Value = 0f;
        }

        public override IModifierInstance Copy() =>
            new ScaledByParameterModifier(ModifierValueType, EntityParameter, PerParameter, BaseValue, Condition?.Copy(), Source);

        /// <summary>Takes the carrier's measure and keeps watching it: the scale is his, so every move of
        /// that parameter is this line's business.</summary>
        public override void Bind(IFightable target)
        {
            base.Bind(target);
            target.Parameters.ParameterChanged += OnCarrierParameterChanged;
            Rescale(target.Parameters.GetValueForParameter(PerParameter));
        }

        /// <summary>Lets the carrier go and falls back to nothing — a line nobody carries has no scale, and
        /// a watch left behind would keep a released fighter answering.</summary>
        public override void Unbind()
        {
            if (Owner is not { } carrier) return;

            carrier.Parameters.ParameterChanged -= OnCarrierParameterChanged;
            base.Unbind();
            Value = 0f;
        }

        /// <summary>Only the carrier's own scale moves this line; every other parameter of his is noise.</summary>
        private void OnCarrierParameterChanged(EntityParameter parameter, float value)
        {
            if (parameter != PerParameter) return;

            Rescale(value);
        }

        /// <summary>The new measure, announced to the carrier so the parameter it feeds is resolved again —
        /// the line is pulled from a source rather than written onto him, so nothing else would notice.</summary>
        private void Rescale(float scale)
        {
            float scaled = BaseValue * scale;
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
