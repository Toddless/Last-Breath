namespace Core.Results
{
    using System;
    using Interfaces;

    public class Result<T> : IResult<T>
        where T : struct, Enum
    {
        private Result(bool isSuccess, T info)
        {
            IsSuccess = isSuccess;
            Info = info;
        }


        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public T Info { get; }

        public static IResult<T> Success(T info = default) => new Result<T>(true, info);

        public static IResult<T> Failure(T info) => new Result<T>(false, info);
    }
}
