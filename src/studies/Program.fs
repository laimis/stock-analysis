open System
open studies

Environment.GetCommandLineArgs()
|> ServiceHelper.init None
|> BreakoutStudy.run
|> Async.RunSynchronously
