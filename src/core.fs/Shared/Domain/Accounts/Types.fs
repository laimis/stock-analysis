namespace core.fs.Shared.Domain.Accounts

open System

type UserId = UserId of Guid

module IdentifierHelper =
    
    let getUserId userId =
        match userId with
        | UserId id -> id
    
[<Struct>]
type EmailIdPair(email:string, id:string) =
    
    member _.Email = email
    member _.Id = id |> Guid |> UserId
    
   
type ProcessIdToUserAssociation(Id:Guid, UserId:UserId, Timestamp:DateTimeOffset) =
    
    new(userId, timestamp:DateTimeOffset) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, timestamp)
    new(userId, timestamp:string) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, DateTimeOffset.Parse(timestamp))
    new(id:Guid, userId:Guid, timestamp:string) = ProcessIdToUserAssociation(id, userId |> UserId, DateTimeOffset.Parse(timestamp))
    
    member _.IsOlderThan(duration:TimeSpan) = DateTimeOffset.UtcNow.Subtract(Timestamp) > duration
    member _.Id = Id
    member _.UserId = UserId
    member _.Timestamp = Timestamp