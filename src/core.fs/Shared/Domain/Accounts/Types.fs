namespace core.fs.Shared.Domain.Accounts

open System

[<Struct>]
type EmailIdPair(email:string, id:string) =
    
    member _.Email = email
    member _.Id = id
    
   
type ProcessIdToUserAssociation(Id:Guid, UserId:Guid, Timestamp:DateTimeOffset) =
    
    new(userId:Guid, timestamp:DateTimeOffset) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, timestamp)
    new(userId:Guid, timestamp:string) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, DateTimeOffset.Parse(timestamp))
    new(id:Guid, userId:Guid, timestamp:string) = ProcessIdToUserAssociation(id, userId, DateTimeOffset.Parse(timestamp))
    
    member _.IsOlderThan(duration:TimeSpan) = DateTimeOffset.UtcNow.Subtract(Timestamp) > duration
    member _.Id = Id
    member _.UserId = UserId
    member _.Timestamp = Timestamp