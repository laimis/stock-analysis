#nullable enable
namespace core.Shared
{
    public class ServiceResponse<TSuccess>
    {
        public TSuccess? Success { get; }
        public ServiceError? Error { get; }
        
        public ServiceResponse(TSuccess success) =>
            Success = success;

        public ServiceResponse(ServiceError error) =>
            Error = error;
        
        public bool IsOk => Error == null;
    }

    public class ServiceError
    {
        public ServiceError(string message) =>
            Message = message;

        public string Message { get; }
    }
}

#nullable restore