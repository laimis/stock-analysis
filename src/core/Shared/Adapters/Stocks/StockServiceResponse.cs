#nullable enable

namespace core.Adapters.Stocks
{
    public class StockServiceResponse<TSuccess, TError>
    {
        public TSuccess? Success { get; set; }
        public TError? Error { get; set; }
        public bool IsOk => this.Error == null;

        public StockServiceResponse(){}
        
        public StockServiceResponse(TSuccess success) =>
            Success = success;
    }
}
#nullable restore