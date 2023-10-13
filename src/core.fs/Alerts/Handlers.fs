namespace core.fs.Alerts

    open core.Shared
    open core.Shared.Adapters.SMS
    open core.Stocks
    open core.fs.Shared
    open core.fs.Shared.Domain.Accounts

    module AlertContainer =
        
        type Query = {
            UserId:UserId
        }
        type QueryAvailableMonitors = struct end
        type Run = struct end
        
        type Handler(container:StockAlertContainer) =
        
            let deregisterStopPriceMonitoring userId ticker =
                container.DeregisterStopPriceAlert ticker userId
                
            interface IApplicationService
            member this.StockPurchased() =
                container.RequestManualRun()
                
            member this.Handle(stockSold:StockSold) =
                stockSold.Ticker |> Ticker |> deregisterStopPriceMonitoring (stockSold.UserId |> UserId)
                
            member this.Handle(stopPriceSet:StopPriceSet) =
                stopPriceSet.Ticker |> Ticker |> deregisterStopPriceMonitoring (stopPriceSet.UserId |> UserId)
                
            member this.Handle(stopDeleted:StopDeleted) =
                stopDeleted.Ticker |> Ticker |> deregisterStopPriceMonitoring (stopDeleted.UserId |> UserId)
                
            member this.Handle(query:Query) =
                let alerts = 
                    container.GetAlerts(query.UserId)
                    |> Seq.sortBy (fun a -> a.ticker.Value, a.description)
                    |> Seq.toList

                let recentlyTriggered = container.GetRecentlyTriggered(query.UserId)
                
                {| alerts = alerts; recentlyTriggered = recentlyTriggered; messages = container.GetNotices() |}
            
            
            member this.Handle (_:QueryAvailableMonitors) =
                [
                    {| name = Constants.MonitorNamePattern; tag = Constants.MonitorTagPattern |}
                ]
            
            member this.Handle (_:Run) =
                container.RequestManualRun()
                
    module SMS =
        
        type SendSMS = {
            Body:string
        }
        
        type TurnSMSOn = struct end
        type TurnSMSOff = struct end
        type Status = struct end
        
        type Handler(smsService:ISMSClient) =
            
            interface IApplicationService
            
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
            
            member this.Handle (_:Status) = task {
                return smsService.IsOn |> ResponseUtils.success<bool>
            }
            
            