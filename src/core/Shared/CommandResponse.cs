using System;

namespace core.Shared
{
    public class CommandResponse<T> : CommandResponse where T : Aggregate
    {
        protected CommandResponse(string error) : base(error)
        {
        }

        protected CommandResponse(T agg) : base(null)
        {
            this.Aggregate = agg;
        }

        public T Aggregate { get; }

        public static new CommandResponse<T> Failed(string error)
        {
            return new CommandResponse<T>(error);
        }

        public static CommandResponse<T> Success(T arr)
        {
            return new CommandResponse<T>(arr);
        }
    }

    public class CommandResponse
    {
        protected CommandResponse(string error)
        {
            this.Error = error;
        }

        public string Error { get; }

        internal static CommandResponse Success()
        {
            return new CommandResponse(null);
        }

        internal static CommandResponse Failed(string error)
        {
            return new CommandResponse(error);
        }
    }
}