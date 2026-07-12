namespace Core.Modifiers
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    /// <summary>Immutable, engine-free description of one rollable modifier line. Lives in resource/pool data;
    /// a <see cref="IModifierMaterializer"/> turns it into a fresh instance at apply time, so a pool entry can
    /// never mutate a shared template. Aggregate ("all X") lines need no special kind — they are a
    /// <see cref="ParameterDescriptor"/> on an aggregate <see cref="EntityParameter"/>.</summary>
    public interface IModifierDescriptor : IWeightable
    {
    }

    public sealed record ParameterDescriptor(EntityParameter Parameter, ModifierValueType ValueType, float Value, ModifierScope Scope) : IModifierDescriptor
    {
        public float Weight { get; set; }
    }

    public sealed record ContextDescriptor(ContextParameter Parameter, ModifierValueType ValueType, float Value) : IModifierDescriptor
    {
        public float Weight { get; set; }
    }

    /// <summary>A weighted bundle rolled as one unit at creation; flattened to its atomic parts for reroll (1-for-1).</summary>
    public sealed record CompositeDescriptor(IReadOnlyList<IModifierDescriptor> Parts) : IModifierDescriptor
    {
        public float Weight { get; set; }
    }
}
