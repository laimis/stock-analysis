open System
open studies

Environment.GetCommandLineArgs()
|> ServiceHelper.init None
|> ObvStudy.run
|> Async.RunSynchronously
