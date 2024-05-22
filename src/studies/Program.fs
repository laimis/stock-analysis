open System
open studies

Environment.GetCommandLineArgs()
|> ServiceHelper.init None
|> ScreenerStudy.run
