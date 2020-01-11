using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using Dapper;

namespace storage.postgres
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        public AccountStorage(string cnn) : base(cnn)
        {
        }

        public async Task RecordLoginAsync(LoginLogEntry entry)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"INSERT INTO loginlog (username, date)
                VALUES (@username, @date)";

                await db.ExecuteAsync(query, entry);
            }
        }

        public async Task<IEnumerable<LoginLogEntry>> GetLogins()
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"SELECT username, date FROM loginlog ORDER BY date DESC";

                var list = await db.QueryAsync<LoginLogEntry>(query);

                return list;
            }
        }
    }
}