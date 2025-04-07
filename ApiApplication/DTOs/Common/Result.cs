namespace ApiApplication.DTOs.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }
        public string ErrorCode { get; }
        public IDictionary<string, string[]> ValidationErrors { get; }

        private Result(bool isSuccess, T value, string error, string errorCode, IDictionary<string, string[]> validationErrors)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors;
        }

        public static Result<T> Success(T value) =>
            new Result<T>(true, value, null, null, null);

        public static Result<T> Failure(string error, string errorCode = null) =>
            new Result<T>(false, default, error, errorCode, null);

        public static Result<T> ValidationFailure(IDictionary<string, string[]> validationErrors) =>
            new Result<T>(false, default, "Validation failed", "VALIDATION_ERROR", validationErrors);
    }
}
