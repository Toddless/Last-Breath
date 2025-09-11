namespace Core.Interfaces
{
    public interface IIdentifiable
    {
        string Id { get; }
        string DisplayName { get; }
        string[] Tags { get; }

        bool HasTag(string tag);
    }
}
