namespace Core.EventArgs
{
    public interface IEventArgs<T>
    {
        T Value { get; }
    }
}
