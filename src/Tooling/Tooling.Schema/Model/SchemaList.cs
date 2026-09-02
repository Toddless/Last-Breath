namespace Tooling.Schema.Model
{
    using System;

    /// <summary>Builds <see cref="SchemaList{T}"/> values, including from collection expressions.</summary>
    public static class SchemaList
    {
        public static SchemaList<T> Create<T>(ReadOnlySpan<T> items) => new(items.ToArray());
    }
}
