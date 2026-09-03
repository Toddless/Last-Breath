namespace Tooling.Tests.Schema
{
    using System;

    // The data markup as the tests write it. Deliberately NOT the game's attributes: the tool reads
    // markup by the name of its type and the names of what it carries, and a test sharing the game's
    // types would prove only that reading by type still works. These carry the same names and nothing
    // else in common, so the reflector reading them is the convention holding.

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class CatalogRefAttribute(string catalog) : Attribute
    {
        public string Catalog { get; } = catalog;

        public string? Section { get; init; }

        public bool AllowEmpty { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class NotARefAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class EnumOfAttribute(Type enumType) : Attribute
    {
        public Type EnumType { get; } = enumType;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class DictionaryKeyAttribute : Attribute
    {
        public DictionaryKeyAttribute(Type enumType) => EnumType = enumType;

        public DictionaryKeyAttribute(string catalog) => Catalog = catalog;

        public Type? EnumType { get; }

        public string? Catalog { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class RangeAttribute(double min, double max) : Attribute
    {
        public double Min { get; } = min;

        public double Max { get; } = max;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class LocalizedKeyAttribute : Attribute
    {
        public const string NoSuffix = "";

        public LocalizedKeyAttribute(string suffix = NoSuffix) => Suffix = suffix;

        public string Suffix { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class DiscriminatorAttribute(string field) : Attribute
    {
        public string Field { get; } = field;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class HiddenAttribute : Attribute
    {
    }
}

namespace Tooling.Tests.Schema.Broken
{
    using System;

    // Markup wearing a name the tool reads by and answering to nothing behind it. A convention has two
    // halves, and this is what the reading half does when the writing half breaks it: a word in the
    // report, not a walk that stops.

    /// <summary>Names the shapes' field and carries no field to name it with.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class DiscriminatorAttribute : Attribute
    {
    }

    /// <summary>Carries both ends of a range as words.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class RangeAttribute(string min, string max) : Attribute
    {
        public string Min { get; } = min;

        public string Max { get; } = max;
    }
}
