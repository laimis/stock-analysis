using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using Dapper;

namespace storage
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        public AccountStorage(string cnn) : base(cnn)
        {
        }

        public async void RecordLogin(string username)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"INSERT INTO loginlog (username, date)
                VALUES (@username, @date)";

                await db.ExecuteAsync(query, new {username, date = DateTime.UtcNow});
            }
        }

        public async Task<IEnumerable<LoginLogEntry>> List(int offset, int limit)
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