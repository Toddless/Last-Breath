namespace Tooling.Schema.Model
{
    /// <summary>The shape of a catalog's file as a whole: where the records sit once it is opened.</summary>
    public enum RootShape
    {
        /// <summary>One key at the root holding the array of records.</summary>
        ArrayUnderKey,

        /// <summary>Several keyed sections, each holding its own array of records.</summary>
        SectionsOfArrays,

        /// <summary>The root is a map of id to record.</summary>
        Dictionary,

        /// <summary>The root is one record — a settings document with nothing to add to.</summary>
        Single
    }
}
