namespace Core.Interfaces
{
    public interface IIdentifiable
    {
        string Id { get; }
        string InstanceId { get; }

        bool IsSame(string otherId);
    }
}
