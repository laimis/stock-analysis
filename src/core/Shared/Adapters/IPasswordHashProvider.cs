namespace core.Shared.Adapters
{
    public interface IPasswordHashProvider
    {
        (string Hash, string Salt) Generate(string password, int saltLength);
        string Generate(string password, string salt);
    }
}