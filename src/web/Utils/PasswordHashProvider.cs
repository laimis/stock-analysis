using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using core.Account;

namespace web.Utils
{
    // https://dotnetcodr.com/2017/10/26/how-to-hash-passwords-with-a-salt-in-net-2/
    public class PasswordHashProvider : IPasswordHashProvider
    {
        public (string Hash, string Salt) Generate(string password, int saltLength)
        {
            return Generate(
                Encoding.UTF8.GetBytes(password),
                GenerateRandomCryptographicBytes(saltLength)
            );
        }

        public string Generate(string password, string salt)
        {
            var result = Generate(
                Encoding.UTF8.GetBytes(password),
                Convert.FromBase64String(salt)
            );

            return result.Hash;
        }

        private static (string Hash, string Salt) Generate(byte[] passwordAsBytes, byte[] saltBytes)
        {
            var passwordWithSaltBytes = new List<byte>();
            passwordWithSaltBytes.AddRange(passwordAsBytes);
            passwordWithSaltBytes.AddRange(saltBytes);
            byte[] hashBytes = SHA512.Create().ComputeHash(passwordWithSaltBytes.ToArray());
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        private static byte[] GenerateRandomCryptographicBytes(int keyLength)
        {
            var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            byte[] randomBytes = new byte[keyLength];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}