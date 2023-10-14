using System.Security.Cryptography;
using System.Text;
using core.fs.Shared.Adapters.Authentication;

namespace securityutils;

// https://dotnetcodr.com/2017/10/26/how-to-hash-passwords-with-a-salt-in-net-2/
public class PasswordHashProvider : IPasswordHashProvider
{
    public HashAndSalt GenerateHashAndSalt(string password, int saltLength)
    {
        return Generate(
            Encoding.UTF8.GetBytes(password),
            RandomNumberGenerator.GetBytes(saltLength)
        );
    }

    public string GenerateHash(string password, string salt)
    {
        var result = Generate(
            Encoding.UTF8.GetBytes(password),
            Convert.FromBase64String(salt)
        );

        return result.Hash;
    }

    private static HashAndSalt Generate(byte[] passwordAsBytes, byte[] saltBytes)
    {
        var passwordWithSaltBytes = new List<byte>();
        passwordWithSaltBytes.AddRange(passwordAsBytes);
        passwordWithSaltBytes.AddRange(saltBytes);
        byte[] hashBytes = SHA512.Create().ComputeHash(passwordWithSaltBytes.ToArray());
        return new HashAndSalt(hash: Convert.ToBase64String(hashBytes), salt: Convert.ToBase64String(saltBytes));
    }
}