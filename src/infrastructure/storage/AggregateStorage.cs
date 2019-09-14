using System.Data;
using Npgsql;

namespace storage
{
    public class AggregateStorage
    {
        protected string _cnn;

        public AggregateStorage(string cnn)
        {
            _cnn = cnn;
        }

        protected IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_cnn);
        }
    }
}