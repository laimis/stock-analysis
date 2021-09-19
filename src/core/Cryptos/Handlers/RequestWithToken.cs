using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Cryptos.Handlers
{
    public class RequestWithToken<T> : RequestWithUserId<T>
    {
        private Token? _token;
        [Required]
        public string Token 
        {
            get 
            { 
                if (_token == null) return null;
                return _token;
            }
            
            set 
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _token = null;
                    return;
                }
                _token = new Token(value);
            }
        }
    }
}