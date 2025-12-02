namespace Core.Interfaces
{
    public interface ITaggable
    {
        string[] Tags { get; }

        bool HasTag(string tag);
    }
}
