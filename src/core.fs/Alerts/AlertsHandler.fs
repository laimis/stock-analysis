namespace core.fs.Alerts

    open core.Shared
    open core.Stocks
    open core.fs
    open core.fs.Accounts
    open core.fs.Adapters.Logging
    open core.fs.Adapters.SMS
    open core.fs.Alerts.MonitoringServices

    
    type QueryAlerts = {UserId:UserId}
    type QueryAvailableMonitors = struct end
    type Run = struct end
    type SendSMS = {Body:string}    
    type TurnSMSOn = struct end
    type TurnSMSOff = struct end
    type SMSStatus = struct end
    type RunEmailJob = struct end
    
    type Handler(container:StockAlertContainer,smsService:ISMSClient,alertEmailService:AlertEmailService,logger:ILogger) =
    
        let deregisterStopPriceMonitoring userId ticker =
            container.Deregister ticker Constants.StopLossIdentifier userId
            
        interface IApplicationService
            
        member this.Handle(stockSold:StockSold) =
            stockSold.Ticker |> Ticker |> deregisterStopPriceMonitoring (stockSold.UserId |> UserId)
            
        member this.Handle(stopPriceSet:StopPriceSet) =
            stopPriceSet.Ticker |> Ticker |> deregisterStopPriceMonitoring (stopPriceSet.UserId |> UserId)
            
        member this.Handle(stopDeleted:StopDeleted) =
            stopDeleted.Ticker |> Ticker |> deregisterStopPriceMonitoring (stopDeleted.UserId |> UserId)
            
        member this.Handle(query:QueryAlerts) =
            let alerts = 
                container.GetAlerts(query.UserId)
                |> Seq.sortBy (fun a -> a.ticker.Value, a.description)
                |> Seq.toList

            let recentlyTriggered = container.GetRecentlyTriggered(query.UserId)
            
            { alerts = alerts; recentlyTriggered = recentlyTriggered; messages = container.GetNotices() }
        
        member this.Handle (_:QueryAvailableMonitors) =
            [
                {| name = Constants.MonitorNamePattern; tag = Constants.MonitorTagPattern |}
                {| name = Constants.MonitorNameObvPriceTrend; tag = Constants.MonitorTagObvPriceTrend |}
            ]
            
        member this.Handle (_:Run) : Result<Unit,ServiceError> =
            // TODO: need to bring this functionality back, it should kick off pattern monitoring run
            Ok ()
      
        member this.Handle (send:SendSMS) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
            do! smsService.SendSMS(send.Body)
            return Ok ()
        }
        
        member this.Handle (_:TurnSMSOn) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
            return smsService.TurnOn() |> Ok
        }
        
        member this.Handle (_:TurnSMSOff) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
            return smsService.TurnOff() |> Ok
        }
        
        member this.Handle (_:SMSStatus) : System.Threading.Tasks.Task<Result<bool, ServiceError>> = task {
            return smsService.IsOn |> Ok
        }
        
        member this.Handle (_:RunEmailJob) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
            do! alertEmailService.Execute() |> Async.AwaitTask
            return Ok ()
        }
