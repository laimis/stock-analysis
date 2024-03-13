open System
open studies


Environment.GetCommandLineArgs()
|> ServiceHelper.init None
|> MarketTrendsStudy.run
