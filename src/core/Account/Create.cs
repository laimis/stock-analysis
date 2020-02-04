using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Create
    {
        public class Command : IRequest<CommandResponse<User>>
        {
            [Required]
            public string Email { get; set; }
            
            [Required]
            public string Firstname { get; set; }
            
            [Required]
            public string Lastname { get; set; }
            
            [Required]
            [MinLength(12)]
            [MaxLength(1000)]
            public string Password { get; set; }

            [Required]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to Terms & Conditions")]
            public bool Terms { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<User>>
        {
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hash)
            {
                _storage = storage;
                _hash = hash;
            }

            public async Task<CommandResponse<User>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var exists = await this._storage.GetUserByEmail(cmd.Email);
                if (exists != null)
                {
                    return CommandResponse<User>.Failed($"Account with {cmd.Email} already exists");
                }

                var u = new User(cmd.Email, cmd.Firstname, cmd.Lastname);

                var (hash, salt) = _hash.Generate(cmd.Password, 32);

                u.SetPassword(hash, salt);

                await _storage.Save(u);

                return CommandResponse<User>.Success(u);
            }
        }
    }
}