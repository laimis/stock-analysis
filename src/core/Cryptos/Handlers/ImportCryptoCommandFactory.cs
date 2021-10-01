using core.Shared;

namespace core.Cryptos.Handlers
{
    public class ImportCryptoCommandFactory
    {
        public static RequestWithUserId<CommandResponse> Create(string filename, string content) =>
            filename switch {
                string s when s.ToLower().Contains("coinbasepro") => new ImportCoinbasePro.Command(content),
                string s when s.ToLower().Contains("blockfi") => new ImportBlockFi.Command(content),
                string s when s.ToLower().Contains("coinbase") => new ImportCoinbase.Command(content),
                _ => new ImportCoinbase.Command(content)
        };
    }
}