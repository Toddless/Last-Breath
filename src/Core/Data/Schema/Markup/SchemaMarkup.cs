namespace Core.Data.Schema
{
    using System;

    // Data markup for external tools. The game reads none of it: the authoring tool recognises an
    // attribute by the NAME of its type and reads what it carries by the NAMES of its properties, so a
    // rename here is a rename of the contract.

    /// <summary>The string holds the id of a record in the named catalog; on a collection of strings,
    /// every element does. Written once per catalog the field may point into: one loot position names
    /// equipment, resources or recipes, and any one of them makes the id a real reference.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class CatalogRefAttribute : Attribute
    {
        public CatalogRefAttribute(string catalog)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalog);
            Catalog = catalog;
        }

        /// <summary>Catalog whose records this string may name.</summary>
        public string Catalog { get; }

        /// <summary>An empty string is a legal "nothing named". Without it an empty value is a broken
        /// reference, which is the answer for every field that must point somewhere.</summary>
        public bool AllowEmpty { get; set; }
    }

    /// <summary>
    /// The string is not a reference, whatever its name suggests. Says so out loud because a field
    /// named like a reference and left unmarked is indistinguishable from one nobody got to yet.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NotARefAttribute : Attribute
    {
    }

    /// <summary>The string holds the name of a member of the given enum, parsed strictly. The tool
    /// offers the members instead of a text box.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class EnumOfAttribute : Attribute
    {
        public EnumOfAttribute(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            if (!enumType.IsEnum)
                throw new ArgumentException($"'{enumType.Name}' is not an enum, so the field would have no members to offer.", nameof(enumType));

            EnumType = enumType;
        }

        public Type EnumType { get; }
    }

    /// <summary>The KEYS of a map are not words the author picks freely: they are the members of an enum,
    /// or ids of records in the named catalog. Written once per catalog the keys may point into; what the
    /// map HOLDS is a separate question, answered by the markup on the values.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class DictionaryKeyAttribute : Attribute
    {
        public DictionaryKeyAttribute(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            if (!enumType.IsEnum)
                throw new ArgumentException($"'{enumType.Name}' is not an enum, so the keys would have no members to offer.", nameof(enumType));

            EnumType = enumType;
        }

        public DictionaryKeyAttribute(string catalog)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalog);
            Catalog = catalog;
        }

        /// <summary>Enum whose members name the keys; null when the keys name records instead.</summary>
        public Type? EnumType { get; }

        /// <summary>Catalog whose records the keys may name; null when the keys are enum members instead.</summary>
        public string? Catalog { get; }
    }

    /// <summary>The numbers this field accepts, ends included.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(double min, double max)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
            Min = min;
            Max = max;
        }

        public double Min { get; }

        public double Max { get; }
    }

    /// <summary>The string is a localization key rather than text to read. A suffix is given when the
    /// key is derived from the record's id rather than typed in full.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class LocalizedKeyAttribute : Attribute
    {
        /// <summary>The key is the value itself, not derived from anything.</summary>
        public const string NoSuffix = "";

        public LocalizedKeyAttribute(string suffix = NoSuffix)
        {
            ArgumentNullException.ThrowIfNull(suffix);
            Suffix = suffix;
        }

        public string Suffix { get; }
    }

    /// <summary>
    /// The records here take more than one shape, and the named field carries the value that says
    /// which. Only the field is named: which shapes exist is the catalog descriptor's answer, because
    /// a shape is a whole record and a record cannot be spelled out in an attribute argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class DiscriminatorAttribute : Attribute
    {
        public DiscriminatorAttribute(string field)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);
            Field = field;
        }

        /// <summary>Json name of the field whose value picks the shape.</summary>
        public string Field { get; }
    }

    /// <summary>The field is machinery — a version stamp, a cache — and is kept out of the inspector.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class HiddenAttribute : Attribute
    {
    }
}
