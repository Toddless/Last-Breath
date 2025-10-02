namespace Core.Interfaces
{
    public interface IResult<T>
        where T : struct
    {
        T Info { get; }
        bool IsFailure { get; }
        bool IsSuccess { get; }

        static abstract IResult<T> Failure(T info);
        static abstract IResult<T> Success(T info = default);
    }
}
