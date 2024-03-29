﻿using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOption : Aggregate<OwnedOptionState>
     {
         public OwnedOption(IEnumerable<AggregateEvent> events) : base(events)
         {
         }
 
         public OwnedOption(
             Ticker ticker,
             decimal strikePrice,
             OptionType type,
             DateTimeOffset expiration,
             Guid userId)
         {
             if (userId == Guid.Empty)
             {
                 throw new InvalidOperationException("Missing user id");
             }
 
             if (strikePrice <= 0)
             {
                 throw new InvalidOperationException("Strike price cannot be zero or negative");
             }
 
             if (expiration == DateTimeOffset.MinValue)
             {
                 throw new InvalidOperationException("Expiration date is in the past");
             }
 
             if (expiration == DateTimeOffset.MaxValue)
             {
                 throw new InvalidOperationException("Expiration date is too far in the future");
             }
 
             Apply(new OptionOpened(
                 Guid.NewGuid(),
                 Guid.NewGuid(),
                 DateTimeOffset.UtcNow,
                 ticker.Value,
                 strikePrice,
                 type,
                 expiration.Date,
                 userId));
         }
 
         public void Delete()
         {
             Apply(
                 new OptionDeleted(
                     Guid.NewGuid(),
                     Id,
                     DateTimeOffset.UtcNow
                 )
             );
         }
 
         public bool IsMatch(Ticker ticker, decimal strike, OptionType type, DateTimeOffset expiration)
             => State.IsMatch(ticker, strike, type, expiration);
 
         public void Buy(int numberOfContracts, decimal premium, DateTimeOffset filled, string notes)
         {
             if (numberOfContracts <= 0)
             {
                 throw new InvalidOperationException("Number of contracts cannot be zero or negative");
             }
 
             if (premium < 0)
             {
                 throw new InvalidOperationException("Premium amount cannot be negative");
             }
             
             if (filled.Date > State.Expiration.Date)
             {
                 throw new InvalidOperationException("Filled date cannot be past expiration");
             }
             
             Apply(
                 new OptionPurchased(
                     Guid.NewGuid(),
                     State.Id,
                     filled,
                     State.UserId,
                     numberOfContracts,
                     premium,
                     notes
                 )
             );
         }
 
         public void Sell(int numberOfContracts, decimal premium, DateTimeOffset filled, string notes)
         {
             if (numberOfContracts <= 0)
             {
                 throw new InvalidOperationException("Number of contracts cannot be zero or negative");
             }
 
             if (premium < 0)
             {
                 throw new InvalidOperationException("Premium money cannot be negative");
             }
 
             if (filled > State.Expiration)
             {
                 throw new InvalidOperationException("Filled date cannot be past expiration");
             }
 
             Apply(
                 new OptionSold(
                     Guid.NewGuid(),
                     State.Id,
                     filled,
                     State.UserId,
                     numberOfContracts,
                     premium,
                     notes
                 )
             );
         }
 
         public void Expire(bool assign)
         {
             if (State.Expirations.Count > 0)
             {
                 throw new InvalidOperationException("You already marked this option as expired");
             }
 
             Apply(new OptionExpired(Guid.NewGuid(), State.Id, State.Expiration, assigned: assign));
         }
     }
}