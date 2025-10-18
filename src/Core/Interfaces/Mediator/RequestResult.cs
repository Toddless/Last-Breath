namespace Core.Interfaces.Mediator
{
    public record RequestResult<T>(bool IsSuccess, string? Message, T? Param)
    {
        public static RequestResult<T> Failure(string error) => new(false, error, default);
        public static RequestResult<T> Success(T param) => new(true, null, param);
    }
}
