namespace core.fs.Alerts

    open System
    open core.Alerts
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.Shared.Adapters.SMS
    open core.Stocks
    open core.fs
    
    module ScanScheduling =
        
        let private _monitorTimes = [
            TimeOnly.Parse("09:45")
            TimeOnly.Parse("11:15")
            TimeOnly.Parse("13:05")
            TimeOnly.Parse("14:35")
            TimeOnly.Parse("15:30")
        ]

        let nextRun referenceTimeUtc (marketHours:IMarketHours) =
            let easternTime = marketHours.ToMarketTime(referenceTimeUtc)
            
            let candidates =
                _monitorTimes
                |> List.map (fun t -> DateTimeOffset(easternTime.Date.Add(t.ToTimeSpan())))
                
                
            let candidatesInFuture =
                candidates
                |> List.filter (fun t -> t > easternTime)
                |> List.map (fun t -> marketHours.ToUniversalTime(t))
                
            match candidates with
            | head :: _ -> head
            | _ -> 
                // if we get here, we need to look at the next day
                let nextDay =
                    match candidates.Head.AddDays(1).DayOfWeek with
                    | DayOfWeek.Saturday -> candidates.Head.AddDays(3)
                    | DayOfWeek.Sunday -> candidates.Head.AddDays(2)
                    | _ -> candidates.Head.AddDays(1)
                
                marketHours.ToUniversalTime(nextDay);

    module AlertContainer =
        
        type Query = { UserId:Guid;}
        type QueryAvailableMonitors = struct end
        type Run = struct end
        
        type Handler(container:StockAlertContainer) =
        
            let deregister userId ticker =
                StopPriceMonitor.Deregister(container, ticker, userId)
                
            interface IApplicationService
            member this.StockPurchased() =
                container.RequestManualRun()
                
            member this.Handle(stockSold:StockSold) =
                stockSold.Ticker |> deregister stockSold.UserId
                
            member this.Handle(stopPriceSet:StopPriceSet) =
                stopPriceSet.Ticker |> deregister stopPriceSet.UserId
                
            member this.Handle(stopDeleted:StopDeleted) =
                stopDeleted.Ticker |> deregister stopDeleted.UserId
                
            member this.Handle(query:Query) =
                let alerts = 
                    container.GetAlerts(query.UserId)
                    |> Seq.sortBy (fun a -> a.ticker, a.description)
                    |> Seq.toList

                let recentlyTriggered = container.GetRecentlyTriggeredAlerts(query.UserId)
                
                {| alerts = alerts; recentlyTriggered = recentlyTriggered; messages = container.GetMessages() |}
            
            
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
            
            member this.Handle (send:SendSMS) = task {
                do! smsService.SendSMS(send.Body)
                return ServiceResponse()
            }
            
            member this.Handle (_:TurnSMSOn) = task {
                smsService.TurnOn()
                return ServiceResponse()
            }
            
            member this.Handle (_:TurnSMSOff) = task {
                smsService.TurnOff()
                return ServiceResponse()
            }
            
            member this.Handle (_:Status) = task {
                return smsService.IsOn |> ResponseUtils.success<bool>
            }
            
            