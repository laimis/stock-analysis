namespace core.fs.Shared.Adapters.Subscriptions

open core.fs.Shared.Domain.Accounts

// TODO: does not make sense to have plan ids here. It should be hidden in the plan logic provider
// and in the user state use an enum for the plan, me thinkest
module Plans =
    let Starter = "plan_GmXCWQvmEWwhtr"
    let Full = "plan_GmXCy9dpWKIB4E"
    
type SubscriptionResult(customerId:string,subscriptionId:string) =     
     member _.CustomerId = customerId
     member _.SubscriptionId = subscriptionId
    
type ISubscriptions =
    abstract member Create : user:User -> email:string -> paymentToken:string -> planId:string -> SubscriptionResult
    