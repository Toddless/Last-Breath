namespace Core.Items
{
    public interface IAscendable
    {
        bool IsAscendable { get; }
        bool TryAscend();
    }
}
