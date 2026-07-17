namespace Core.Modifiers
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    /// <summary>Immutable, engine-free description of one rollable modifier line. Lives in resource/pool data;
    /// a <see cref="IModifierMaterializer"/> turns it into a fresh instance at apply time (rolling the value
    /// range), so a pool entry can never mutate a shared template. Aggregate ("all X") lines need no special
    /// kind — they are a <see cref="ParameterDescriptor"/> on an aggregate <see cref="EntityParameter"/>.
    /// NameKey is a naming hook (affixed item names) — parsed and carried, not consumed yet.</summary>
    public interface IModifierDescriptor : IWeightable
    {
        /// <summary>Which slot family this entry competes for. None only on authored item lines —
        /// rollable pool entries are parsed strictly and never carry None.</summary>
        AffixKind Affix { get; }
    }

    public sealed record ParameterDescriptor(EntityParameter Parameter, ModifierValueType ValueType, ValueRange Value, ModifierScope Scope) : IModifierDescriptor
    {
        public float Weight { get; set; }
        public AffixKind Affix { get; init; }
        public string? NameKey { get; init; }
    }

    public sealed record ContextDescriptor(ContextParameter Parameter, ModifierValueType ValueType, ValueRange Value) : IModifierDescriptor
    {
        public float Weight { get; set; }
        public AffixKind Affix { get; init; }
        public string? NameKey { get; init; }
    }

    /// <summary>Mythic-pool entry "+Min..Max sharpening levels" — an operation on the item, not a line.
    /// Only the ascension gift path applies it; the materializer refuses it loudly, so a stray entry in
    /// a regular roll pool is a no-op with an error, never a silent mis-line.</summary>
    public sealed record UpgradeLevelsDescriptor(int Min, int Max) : IModifierDescriptor
    {
        public float Weight { get; set; }
        public AffixKind Affix { get; init; }
    }

    /// <summary>A weighted bundle rolled as one unit at creation; flattened to its atomic parts for reroll (1-for-1).
    /// Affix lives on the root only — parts inherit it when materialized or flattened.</summary>
    public sealed record CompositeDescriptor(IReadOnlyList<IModifierDescriptor> Parts) : IModifierDescriptor
    {
        public float Weight { get; set; }
        public AffixKind Affix { get; init; }
        public string? NameKey { get; init; }
    }
}
