namespace core.fs.Alerts

    open core.Shared
    open core.Stocks
    open core.fs
    open core.fs.Accounts
    open core.fs.Adapters.SMS

    
    type QueryAlerts = {UserId:UserId}
    type QueryAvailableMonitors = struct end
    type Run = struct end
    type SendSMS = {Body:string}    
    type TurnSMSOn = struct end
    type TurnSMSOff = struct end
    type SMSStatus = struct end
    
    type Handler(container:StockAlertContainer,smsService:ISMSClient) =
    
        let deregisterStopPriceMonitoring userId ticker =
            container.Deregister ticker Constants.StopLossIdentifier userId
            
        interface IApplicationService
        member this.StockPurchased() =
            container.RequestManualRun()
            
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
            
            {| alerts = alerts; recentlyTriggered = recentlyTriggered; messages = container.GetNotices() |}
            |> ResponseUtils.success
        
        
        member this.Handle (_:QueryAvailableMonitors) =
            let available =
                [
                    {| name = Constants.MonitorNamePattern; tag = Constants.MonitorTagPattern |}
                ]
            available |> ResponseUtils.success
        
        member this.Handle (_:Run) =
            container.RequestManualRun()
            Ok
      
        member this.Handle (send:SendSMS) = task {
            do! smsService.SendSMS(send.Body)
            return Ok
        }
        
        member this.Handle (_:TurnSMSOn) = task {
            smsService.TurnOn()
            return Ok
        }
        
        member this.Handle (_:TurnSMSOff) = task {
            smsService.TurnOff()
            return Ok
        }
        
        member this.Handle (_:SMSStatus) = task {
            return smsService.IsOn |> ResponseUtils.success<bool>
        }
            
            