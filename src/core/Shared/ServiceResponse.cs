#nullable enable

namespace core.Adapters.Stocks
{
    public class ServiceResponse<TSuccess>
    {
        public TSuccess? Success { get; }
        public ServiceError? Error { get; }
        public bool IsOk => Error == null;

        public ServiceResponse(){}
        
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