namespace Core.Interfaces.Items
{
    public interface IAscendable
    {
        bool IsAscendable { get; }
        bool TryAscend();
    }
}
