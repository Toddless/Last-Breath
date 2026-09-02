namespace Tooling.Schema.Reflection
{
    /// <summary>
    /// The names the data markup is known by. The attributes themselves live with the game, which the
    /// tool does not reference, so a piece of markup is recognised by the NAME of its type and read by
    /// the NAMES of the properties it carries — the whole contract between the two sides.
    /// <para>Every name the reflector reads by is here and nowhere else: a name spelt in one place only
    /// is a convention nothing can be held to.</para>
    /// </summary>
    public static class MarkupNames
    {
        public const string CatalogRef = "CatalogRefAttribute";
        public const string NotARef = "NotARefAttribute";
        public const string EnumOf = "EnumOfAttribute";
        public const string DictionaryKey = "DictionaryKeyAttribute";
        public const string Range = "RangeAttribute";
        public const string LocalizedKey = "LocalizedKeyAttribute";
        public const string Discriminator = "DiscriminatorAttribute";
        public const string Hidden = "HiddenAttribute";

        /// <summary>Catalog a reference points into, on <see cref="CatalogRef"/> and on
        /// <see cref="DictionaryKey"/>.</summary>
        public const string Catalog = "Catalog";

        /// <summary>Whether an empty value is a legal "nothing named", on <see cref="CatalogRef"/>.</summary>
        public const string AllowEmpty = "AllowEmpty";

        /// <summary>Enum whose members the value may be, on <see cref="EnumOf"/> and on
        /// <see cref="DictionaryKey"/>.</summary>
        public const string EnumType = "EnumType";

        /// <summary>Ends of the accepted numbers, on <see cref="Range"/>.</summary>
        public const string Min = "Min";

        public const string Max = "Max";

        /// <summary>What the localization key is derived with, on <see cref="LocalizedKey"/>.</summary>
        public const string Suffix = "Suffix";

        /// <summary>Json name of the field telling shapes apart, on <see cref="Discriminator"/>.</summary>
        public const string Field = "Field";
    }
}
