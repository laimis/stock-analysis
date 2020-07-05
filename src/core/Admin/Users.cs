using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
using core.Alerts;
using MediatR;
using Newtonsoft.Json;

namespace core.Admin
{
    public class Users
    {
        public class Query : IRequest<object>
        {
            [Required]
            public bool? Everyone { get; set; }
        }

        public class Export : IRequest<ExportResponse>
        {
        }

        public class Handler : IRequestHandler<Users.Query, object>,
            IRequestHandler<Users.Export, ExportResponse>
        {
            private IAccountStorage _storage;
            private IPortfolioStorage _portfolio;
            private IAlertsStorage _alerts;

            public Handler(
                IAccountStorage storage,
                IPortfolioStorage portfolio,
                IAlertsStorage alerts,
                IEmailService emails,
                IMediator mediator)
            {
                _storage = storage;
                _portfolio = portfolio;
                _alerts = alerts;
            }

            public async Task<object> Handle(Users.Query cmd, CancellationToken cancellationToken)
            {
                var users = await _storage.GetUserEmailIdPairs();

                var result = new List<object>();

                foreach(var (email,userId) in users)
                {
                    var guid = new System.Guid(userId);
                    var user = await _storage.GetUser(guid);

                    var options = await _portfolio.GetOwnedOptions(guid);
                    var notes = await _portfolio.GetNotes(guid);
                    var stocks = await _portfolio.GetStocks(guid);
                    var alerts = await _alerts.GetAlerts(guid);
                    
                    var u = new {
                        email,
                        userId,
                        user.Firstname,
                        user.Lastname,
                        user.LastLogin,
                        user.Verified,
                        stock = stocks.Count(),
                        options = options.Count(),
                        notes = notes.Count(),
                        alerts = alerts.Count()
                    };

                    result.Add(u);
                }

                return result;
            }

            public async Task<ExportResponse> Handle(Users.Export request, CancellationToken cancellationToken)
            {
                var pairs = await _storage.GetUserEmailIdPairs();

                var users = new List<User>();

                foreach(var (email,userId) in pairs)
                {
                    var guid = new System.Guid(userId);
                    var user = await _storage.GetUser(guid);
                    users.Add(user);
                }

                var filename = CSVExport.GenerateFilename("users");

                return new ExportResponse(filename, CSVExport.Generate(users));
            }
        }
    }
}