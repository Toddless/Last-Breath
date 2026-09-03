namespace Tooling.Schema.Model
{
    /// <summary>What a field holds, as far as an editor needs to know to draw and check it.</summary>
    public enum FieldKind
    {
        String,
        Integer,
        Number,
        Boolean,

        /// <summary>A member name of an enum; the members are listed on the field.</summary>
        Enum,

        /// <summary>An id of a record in another catalog.</summary>
        Reference,

        /// <summary>A localization key, shown as the text it resolves to.</summary>
        LocalizedKey,

        /// <summary>A nested record.</summary>
        Object,

        /// <summary>A list; the element's own schema is on the field.</summary>
        Array,

        /// <summary>Keys chosen by the author; the value's own schema is on the field.</summary>
        Dictionary,

        /// <summary>Anything at all — kept verbatim, drawn as raw json.</summary>
        Any
    }
}
