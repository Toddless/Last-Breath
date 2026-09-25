namespace Tooling.Json
{
    /// <summary>The order object keys are written in. The ranks come from outside — the schema knows what
    /// a record's fields are and in what order they were declared; the writer only sorts by the answer.</summary>
    public interface IKeyOrder
    {
        /// <summary>The rank of a key nobody declared. It sorts to the tail of its object, and because the
        /// sort is stable, unknown keys keep the order the file had — that is what lets a document carry
        /// keys this build has never heard of through a save.</summary>
        public const int Unknown = int.MaxValue;

        /// <summary>Where <paramref name="key"/> stands among the keys of the object at
        /// <paramref name="objectPointer"/>. Lower is written earlier; <see cref="Unknown"/> is the tail.</summary>
        int Rank(JsonPointer objectPointer, string key);
    }

    /// <summary>Every key is unknown, so every object keeps the order it had in the file. This is the order
    /// a document uses before a schema is attached to it.</summary>
    public sealed class FileKeyOrder : IKeyOrder
    {
        private FileKeyOrder()
        {
        }

        public static FileKeyOrder Instance { get; } = new();

        public int Rank(JsonPointer objectPointer, string key) => IKeyOrder.Unknown;
    }
}
