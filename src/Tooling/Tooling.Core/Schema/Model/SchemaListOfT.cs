namespace Tooling.Schema.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>An immutable list that compares by its contents, so that two descriptions of one
    /// catalog compare as one description however they were built.</summary>
    [CollectionBuilder(typeof(SchemaList), nameof(SchemaList.Create))]
    public readonly struct SchemaList<T> : IReadOnlyList<T>, IEquatable<SchemaList<T>>
    {
        private readonly T[]? _items;

        /// <summary>Copies what it is given: a list that shares an array with its caller is immutable
        /// only until that caller writes to it.</summary>
        public SchemaList(T[] items)
        {
            ArgumentNullException.ThrowIfNull(items);
            foreach (T item in items)
                if (item is null)
                    throw new ArgumentException("A schema list holds no empty entries.", nameof(items));

            _items = [.. items];
        }

        /// <summary>Nothing at all — the same value an unset list has, so a missing list and an empty
        /// one are one case rather than two.</summary>
        public static SchemaList<T> Empty => default;

        public int Count => _items?.Length ?? 0;

        private T[] Items => _items ?? [];

        public T this[int index] => Items[index];

        public static implicit operator SchemaList<T>(T[] items) => new(items);

        public static bool operator ==(SchemaList<T> left, SchemaList<T> right) => left.Equals(right);

        public static bool operator !=(SchemaList<T> left, SchemaList<T> right) => !left.Equals(right);

        public bool Equals(SchemaList<T> other)
        {
            if (Count != other.Count) return false;

            for (int index = 0; index < Count; index++)
                if (!EqualityComparer<T>.Default.Equals(this[index], other[index]))
                    return false;

            return true;
        }

        public override bool Equals(object? obj) => obj is SchemaList<T> other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach (T item in Items) hash.Add(item);
            return hash.ToHashCode();
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
