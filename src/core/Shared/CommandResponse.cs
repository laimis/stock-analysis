#nullable enable
namespace core.Shared
{
    public class CommandResponse<T> : CommandResponse
    {
        protected CommandResponse(string? error) : base(error)
        {
        }

        protected CommandResponse(T? agg) : base(null)
        {
            Aggregate = agg;
        }

        public T? Aggregate { get; }

        public static new CommandResponse<T> Failed(string error) => new(error);

        public static CommandResponse<T> Success(T arr) => new(arr);
    }

    public class CommandResponse
    {
        protected CommandResponse(string? error)
        {
            Error = error;
        }

        public string? Error { get; }

        internal static CommandResponse Success() => new CommandResponse(null);

        internal static CommandResponse Failed(string error) => new CommandResponse(error);
    }

    public class ServiceResponse<TSuccess>
    {
        public TSuccess? Success { get; }
        public ServiceError? Error { get; }
        public bool IsOk => Error == null;
        
        public ServiceResponse(TSuccess success) =>
            Success = success;

        public ServiceResponse(ServiceError error) =>
            Error = error;
    }

    public class ServiceError
    {
        public ServiceError(string message) =>
            Message = message;

        public string Message { get; }
    }
}

#nullable restore