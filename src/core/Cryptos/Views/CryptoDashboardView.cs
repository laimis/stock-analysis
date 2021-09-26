using System.Collections.Generic;

namespace core.Cryptos.Views
{
    public class CryptoDashboardView
    {
        public CryptoDashboardView(List<OwnedCryptoView> ownedStocks)
        {
            Owned = ownedStocks;
        }

        public List<OwnedCryptoView> Owned { get; }
    }
}