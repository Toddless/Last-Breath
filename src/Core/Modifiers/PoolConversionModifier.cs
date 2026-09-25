namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;

    /// <summary>One of the two lines a pool conversion writes on a fighter — "the whole of one parameter
    /// becomes the whole of another". <see cref="Gain"/> is the receiving half, a flat line worth the
    /// converted pool; <see cref="Drain"/> is the emptying half, a MARK rather than a number.
    /// <para>The mark is categorical on purpose. A drain written as a multiplier of −1 would share the
    /// <c>1 + Σ</c> bucket with every other multiplicative line on the pool, so a pair of boots granting
    /// +50% evasion would leave half the evasion alive after it had already been given away. The resolver
    /// reads the mark instead and answers zero for the whole parameter, past the formula and past the
    /// decorators over it.</para>
    /// <para>The gain is measured off the pool as it stands UNCONVERTED, and both halves are this one type
    /// so the reading can tell conversion lines from everything else. A conversion that counted them would
    /// close on itself: its own drain would leave it nothing to convert, and a second conversion pointing
    /// back would feed it without end.</para>
    /// <para>The gift lands flat, so the receiving parameter's own increases and multipliers scale it like
    /// any other line — armor bought with evasion answers to armor's increases.</para></summary>
    public sealed class PoolConversionModifier : ConditionalModifier
    {
        /// <summary>The mark carries no amount; the switch is simply on, the way every flag line is written.</summary>
        private const float MarkValue = 1f;

        /// <summary>Below this the pool measures the same, so the carrier is not asked to resolve again.</summary>
        private const float Tolerance = 0.0001f;

        /// <summary>The pool this line is worth, on the receiving half only — the emptying half names none
        /// and has nothing to measure.</summary>
        private readonly EntityParameter? _converted;

        /// <summary>Latch over the announcement: a resolve this line asked for can come back around to it
        /// through a line measured off the parameter it feeds. The measure is still taken — only the
        /// second announcement is dropped, so a mutual pair settles one level deep instead of running the
        /// chain out as far as the numbers take it.</summary>
        private bool _converting;

        /// <summary>Whether this half is the mark that the pool has been given away — what the resolver
        /// reads as "this parameter is nothing, whatever is written on it".</summary>
        public bool IsDrain => _converted is null;

        private PoolConversionModifier(ModifierValueType valueType, EntityParameter parameter, float value, EntityParameter? converted, string source)
            : base(weight: 0f, valueType, parameter, value, condition: null, source) =>
            _converted = converted;

        /// <summary>The receiving half: <paramref name="to"/> gains the whole unconverted
        /// <paramref name="from"/> pool, re-measured whenever that pool moves.</summary>
        public static PoolConversionModifier Gain(EntityParameter from, EntityParameter to, string source) =>
            new(ModifierValueType.Flat, to, value: 0f, from, source);

        /// <summary>The emptying half: <paramref name="from"/> is marked converted and reads nothing.</summary>
        public static PoolConversionModifier Drain(EntityParameter from, string source) =>
            new(ModifierValueType.Flag, from, MarkValue, converted: null, source);

        public override IModifierInstance Copy() =>
            new PoolConversionModifier(ModifierValueType, EntityParameter, BaseValue, _converted, Source);

        /// <summary>Takes the pool's measure and keeps watching it: what the line is worth is the pool's
        /// business, and every move of it is this line's.</summary>
        public override void Bind(IFightable target)
        {
            base.Bind(target);
            if (_converted is null) return;

            target.Parameters.ParameterChanged += OnPoolChanged;
            Measure(target);
        }

        /// <summary>Lets the carrier go and falls back to what the line was born worth — a watch left behind
        /// would keep a released fighter answering, and a measure taken off him is not this line's own.</summary>
        public override void Unbind()
        {
            if (Owner is not { } carrier) return;

            if (_converted is not null) carrier.Parameters.ParameterChanged -= OnPoolChanged;
            base.Unbind();
            Value = BaseValue;
        }

        private void OnPoolChanged(EntityParameter parameter, float value)
        {
            if (parameter != _converted || Owner is not { } carrier) return;

            Measure(carrier);
        }

        /// <summary>The new measure, announced to the carrier so the parameter it feeds is resolved again —
        /// the value moved inside a line already in his list, and nothing else would notice.</summary>
        private void Measure(IFightable carrier)
        {
            if (_converted is not { } pool) return;

            float converted = carrier.Parameters.GetUnconvertedValueForParameter(pool);
            if (Math.Abs(Value - converted) < Tolerance) return;

            Value = converted;
            if (_converting) return;

            _converting = true;
            try
            {
                carrier.ParameterModifiers.RefreshParameter(EntityParameter);
            }
            finally
            {
                _converting = false;
            }
        }
    }
}
