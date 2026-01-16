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
    [<CLIMutable>]
    type CreateStockPriceAlertResponse = {AlertId:string}
    type UpdateStockPriceAlert = {UserId:UserId; AlertId:Guid; PriceLevel:decimal; AlertType:string; Note:string; State:string}
    type ResetStockPriceAlert = {UserId:UserId; AlertId:Guid}
    type DeleteStockPriceAlert = {AlertId:Guid}
    
    // Reminder Commands
    type GetReminders = {UserId:UserId}
    type CreateReminder = {UserId:UserId; Date:string; Message:string; Ticker:string option}
    [<CLIMutable>]
    type CreateReminderResponse = {ReminderId:string}
    type UpdateReminder = {UserId:UserId; ReminderId:Guid; Date:string; Message:string; Ticker:string option; State:string}
    type DeleteReminder = {ReminderId:Guid}
    
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
        
        member this.Handle (query:GetStockPriceAlerts) : System.Threading.Tasks.Task<Result<StockPriceAlertDto seq, ServiceError>> = task {
            let! alerts = accountStorage.GetStockPriceAlerts(query.UserId)
            let dtos = alerts |> Seq.map StockPriceAlertDto.fromDomain
            return Ok dtos
        }
        
        member this.Handle (command:CreateStockPriceAlert) : System.Threading.Tasks.Task<Result<CreateStockPriceAlertResponse, ServiceError>> = task {
            try
                let ticker = Ticker command.Ticker
                let alertType = PriceAlertType.fromString command.AlertType
                
                let alert = StockPriceAlert.create 
                                command.UserId 
                                ticker 
                                alertType 
                                command.PriceLevel 
                                command.Note
                
                do! accountStorage.SaveStockPriceAlert alert
                return Ok ({AlertId = alert.AlertId.ToString()} : CreateStockPriceAlertResponse)
            with
            | ex ->
                logger.LogError $"Error creating stock price alert: {ex}"
                return Error (ServiceError ex.Message)
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
        
        member this.Handle (command:ResetStockPriceAlert) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                let! allAlerts = accountStorage.GetStockPriceAlerts(command.UserId)
                let existingAlert = 
                    allAlerts 
                    |> Seq.tryFind (fun a -> a.AlertId = command.AlertId)
                
                match existingAlert with
                | Some existing ->
                    let resetAlert = StockPriceAlert.reset existing
                    do! accountStorage.SaveStockPriceAlert(resetAlert)
                    return Ok ()
                | None ->
                    return Error (ServiceError("Alert not found"))
            with
            | ex ->
                logger.LogError($"Error resetting stock price alert: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
        
        member this.Handle (command:DeleteStockPriceAlert) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                do! accountStorage.DeleteStockPriceAlert command.AlertId
                return Ok ()
            with
            | ex ->
                logger.LogError $"Error deleting stock price alert: {ex.Message}"
                return Error (ServiceError ex.Message)
        }
        
        // Reminder Handlers
        member this.Handle (query:GetReminders) : System.Threading.Tasks.Task<Result<ReminderDto seq, ServiceError>> = task {
            let! reminders = accountStorage.GetReminders(query.UserId)
            let dtos = reminders |> Seq.map ReminderDto.fromReminder
            return Ok dtos
        }
        
        member this.Handle (command:CreateReminder) : System.Threading.Tasks.Task<Result<CreateReminderResponse, ServiceError>> = task {
            try
                let date = DateTimeOffset.Parse command.Date
                let ticker = 
                    command.Ticker 
                    |> Option.map (fun t -> Ticker t)
                
                let reminder = Reminder.create 
                                command.UserId 
                                date
                                command.Message
                                ticker
                
                do! accountStorage.SaveReminder reminder
                return Ok ({ReminderId = reminder.ReminderId.ToString()} : CreateReminderResponse)
            with
            | ex ->
                logger.LogError $"Error creating reminder: {ex}"
                return Error (ServiceError ex.Message)
        }
        
        member this.Handle (command:UpdateReminder) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                let! allReminders = accountStorage.GetReminders(command.UserId)
                let existingReminder = 
                    allReminders 
                    |> Seq.tryFind (fun r -> r.ReminderId = command.ReminderId)
                
                match existingReminder with
                | Some existing ->
                    let date = DateTimeOffset.Parse(command.Date)
                    let ticker = 
                        command.Ticker 
                        |> Option.map (fun t -> Ticker(t))
                    let state = ReminderState.fromString(command.State)
                    
                    let updatedReminder = {
                        existing with
                            Date = date
                            Message = command.Message
                            Ticker = ticker
                            State = state
                    }
                    
                    do! accountStorage.SaveReminder(updatedReminder)
                    return Ok ()
                | None ->
                    return Error (ServiceError("Reminder not found"))
            with
            | ex ->
                logger.LogError($"Error updating reminder: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
        
        member this.Handle (command:DeleteReminder) : System.Threading.Tasks.Task<Result<Unit, ServiceError>> = task {
            try
                do! accountStorage.DeleteReminder(command.ReminderId)
                return Ok ()
            with
            | ex ->
                logger.LogError($"Error deleting reminder: {ex.Message}")
                return Error (ServiceError(ex.Message))
        }
