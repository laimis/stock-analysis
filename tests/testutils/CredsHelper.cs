using System;
using System.IO;

namespace testutils
{
    public class CredsHelper
    {
        private static string FindSolutionRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            
            // Walk up looking for the .sln file
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "tradewatch.sln")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            
            throw new InvalidOperationException("Could not find solution root (tradewatch.sln)");
        }
        
        public static string GetCoinMarketCapToken()
        {
            var solutionRoot = FindSolutionRoot();
            var path = Path.Combine(solutionRoot, "coinmarketcap_secret");
            return File.ReadAllText(path);
        }

        public static string GetDbCreds()
        {
            var solutionRoot = FindSolutionRoot();
            var path = Path.Combine(solutionRoot, "postgres_secret");
            return File.ReadAllText(path);
        }
    }
}