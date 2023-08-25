namespace core.Shared
{
    public class CommandResponse<T> : CommandResponse
    {
        protected CommandResponse(string error) : base(error)
        {
        }

        protected CommandResponse(T agg) : base(null)
        {
            Aggregate = agg;
        }

        public T Aggregate { get; }

        public static new CommandResponse<T> Failed(string error) => new CommandResponse<T>(error);

        public static CommandResponse<T> Success(T arr) => new CommandResponse<T>(arr);
    }

    public class CommandResponse
    {
        protected CommandResponse(string error)
        {
            Error = error;
        }

        public string Error { get; }

        internal static CommandResponse Success() => new CommandResponse(null);

        internal static CommandResponse Failed(string error) => new CommandResponse(error);
    }
}