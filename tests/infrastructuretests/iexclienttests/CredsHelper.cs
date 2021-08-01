using System.IO;

namespace iexclienttests
{
    internal class CredsHelper
    {
        internal static string GetIEXToken()
        {
            var path = @"..\..\..\..\..\..\iex_secret";

            return File.ReadAllText(path);
        }
    }
}