#nullable enable

namespace core.Adapters.Stocks
{
    public class StockServiceResponse<TSuccess>
    {
        public TSuccess? Success { get; }
        public ServiceError? Error { get; }
        public bool IsOk => Error == null;

        public StockServiceResponse(){}
        
        public StockServiceResponse(TSuccess success) =>
            Success = success;

        public StockServiceResponse(ServiceError error) =>
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