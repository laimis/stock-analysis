namespace core.fs.Alerts

    open System
    open core.Shared
    open core.Stocks
    open core.fs
    open core.fs.Accounts
    open core.fs.Adapters.Logging
    open core.fs.Adapters.SMS
    open core.fs.Adapters.Storage
    open core.fs.Alerts.MonitoringServices

    
    type QueryAlerts = {UserId:UserId}
    type QueryAvailableMonitors = struct end
    type Run = struct end
    type SendSMS = {Body:string}    
    type TurnSMSOn = struct end
    type TurnSMSOff = struct end
    type SMSStatus = struct end
    type RunEmailJob = struct end
    
    // Stock Price Alert Commands
    type GetStockPriceAlerts = {UserId:UserId}
    type CreateStockPriceAlert = {UserId:UserId; Ticker:string; PriceLevel:decimal; AlertType:string; Note:string}
    type UpdateStockPriceAlert = {UserId:UserId; AlertId:Guid; PriceLevel:decimal; AlertType:string; Note:string; State:string}
    type DeleteStockPriceAlert = {AlertId:Guid}
    
    type Handler(container:StockAlertContainer,smsService:ISMSClient,alertEmailService:AlertEmailService,logger:ILogger,accountStorage:IAccountStorage) =
    
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
        
        member this.Handle (query:GetStockPriceAlerts) : System.Threading.Tasks.Task<Result<StockPriceAlert seq, ServiceError>> = task {
            let! alerts = accountStorage.GetStockPriceAlerts(query.UserId)
            return Ok alerts
        }
        
        member this.Handle (command:CreateStockPriceAlert) : System.Threading.Tasks.Task<Result<Guid, ServiceError>> = task {
            try
                let alertId = Guid.NewGuid()
                let ticker = Ticker(command.Ticker)
                let alertType = PriceAlertType.fromString(command.AlertType)
                
                let alert = {
                    AlertId = alertId
                    UserId = command.UserId
                    Ticker = ticker
                    PriceLevel = command.PriceLevel
                    AlertType = alertType
                    Note = command.Note
                    State = PriceAlertState.Active
                    CreatedAt = System.DateTimeOffset.UtcNow
                    TriggeredAt = None
                    LastResetAt = None
                }
                
                do! accountStorage.SaveStockPriceAlert(alert)
                return Ok alertId
            with
            | ex ->
                logger.LogError($"Error creating stock price alert: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
        
        member this.Handle (command:UpdateStockPriceAlert) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                // We need to get the existing alert first to preserve some fields
                let! allAlerts = accountStorage.GetStockPriceAlerts(command.UserId)
                let existingAlert = 
                    allAlerts 
                    |> Seq.tryFind (fun a -> a.AlertId = command.AlertId)
                
                match existingAlert with
                | Some existing ->
                    let alertType = PriceAlertType.fromString(command.AlertType)
                    let state = PriceAlertState.fromString(command.State)
                    
                    let updatedAlert = {
                        existing with
                            PriceLevel = command.PriceLevel
                            AlertType = alertType
                            Note = command.Note
                            State = state
                    }
                    
                    do! accountStorage.SaveStockPriceAlert(updatedAlert)
                    return Ok ()
                | None ->
                    return Error (ServiceError("Alert not found"))
            with
            | ex ->
                logger.LogError($"Error updating stock price alert: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
        
        member this.Handle (command:DeleteStockPriceAlert) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                do! accountStorage.DeleteStockPriceAlert(command.AlertId)
                return Ok ()
            with
            | ex ->
                logger.LogError($"Error deleting stock price alert: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
