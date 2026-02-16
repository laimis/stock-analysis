using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using core.fs.Adapters.Storage;
using core.Shared;
using web.Utils;

#nullable enable

namespace web.Controllers;

public record SearchEntitiesRequest(string Name);
public record CreateEntityRequest(string Name, string EntityType, string? Cik);
public record CreateRoleRequest(Guid EntityId, string CompanyTicker, string CompanyCik, string RelationshipType, string? Title);
public record CreateOwnershipEventRequest(
    Guid EntityId,
    string CompanyTicker,
    string CompanyCik,
    Guid? FilingId,
    string EventType,
    string? TransactionType,
    long? SharesBefore,
    long? SharesTransacted,
    long SharesAfter,
    decimal? PercentOfClass,
    decimal? PricePerShare,
    decimal? TotalValue,
    string TransactionDate,
    string FilingDate,
    bool IsDirect,
    string? OwnershipNature
);

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OwnershipController(IOwnershipStorage storage) : ControllerBase
{
    [HttpGet("ticker/{ticker}")]
    public async Task<ActionResult> GetOwnershipByTicker([FromRoute] string ticker)
    {
        var t = new Ticker(ticker);
        var summary = await storage.GetOwnershipSummary(t);
        return Ok(summary);
    }

    [HttpGet("ticker/{ticker}/timeline")]
    public async Task<ActionResult> GetOwnershipTimeline([FromRoute] string ticker, [FromQuery] int days = 365)
    {
        var t = new Ticker(ticker);
        var timeline = await storage.GetOwnershipTimeline(t, days);
        return Ok(timeline);
    }

    [HttpGet("ticker/{ticker}/events")]
    public async Task<ActionResult> GetEventsByTicker([FromRoute] string ticker)
    {
        var t = new Ticker(ticker);
        var events = await storage.GetEventsByCompany(t);
        return Ok(events);
    }

    [HttpGet("entity/{entityId}")]
    public async Task<ActionResult> GetEntity([FromRoute] Guid entityId)
    {
        var entity = await storage.FindEntityByCik(entityId.ToString());
        if (Microsoft.FSharp.Core.OptionModule.IsNone(entity))
        {
            return NotFound();
        }
        return Ok(Microsoft.FSharp.Core.OptionModule.GetValue(entity));
    }

    [HttpGet("entity/{entityId}/roles")]
    public async Task<ActionResult> GetEntityRoles([FromRoute] Guid entityId)
    {
        var roles = await storage.GetRolesByEntity(entityId);
        return Ok(roles);
    }

    [HttpGet("entity/{entityId}/events")]
    public async Task<ActionResult> GetEventsByEntity([FromRoute] Guid entityId)
    {
        var events = await storage.GetEventsByEntity(entityId);
        return Ok(events);
    }

    [HttpGet("entities/search")]
    public async Task<ActionResult> SearchEntities([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name parameter is required");
        }

        var entities = await storage.FindEntitiesByName(name);
        return Ok(entities);
    }

    [HttpPost("entity")]
    public async Task<ActionResult> CreateEntity([FromBody] CreateEntityRequest request)
    {
        var cikOption = string.IsNullOrWhiteSpace(request.Cik) 
            ? Microsoft.FSharp.Core.FSharpOption<string>.None 
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(request.Cik);

        var entity = new OwnershipEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            EntityType = request.EntityType,
            Cik = cikOption,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var entityId = await storage.SaveEntity(entity);
        return Ok(new { id = entityId });
    }

    [HttpPost("role")]
    public async Task<ActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var titleOption = string.IsNullOrWhiteSpace(request.Title) 
            ? Microsoft.FSharp.Core.FSharpOption<string>.None 
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(request.Title);

        var role = new OwnershipEntityCompanyRole
        {
            Id = Guid.NewGuid(),
            EntityId = request.EntityId,
            CompanyTicker = request.CompanyTicker,
            CompanyCik = request.CompanyCik,
            RelationshipType = request.RelationshipType,
            Title = titleOption,
            IsActive = true,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };

        var roleId = await storage.SaveRole(role);
        return Ok(new { id = roleId });
    }

    [HttpPost("event")]
    public async Task<ActionResult> CreateOwnershipEvent([FromBody] CreateOwnershipEventRequest request)
    {
        var filingIdOption = request.FilingId.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<Guid>.Some(request.FilingId.Value) 
            : Microsoft.FSharp.Core.FSharpOption<Guid>.None;

        var transactionTypeOption = string.IsNullOrWhiteSpace(request.TransactionType) 
            ? Microsoft.FSharp.Core.FSharpOption<string>.None 
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(request.TransactionType);

        var sharesBeforeOption = request.SharesBefore.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<long>.Some(request.SharesBefore.Value) 
            : Microsoft.FSharp.Core.FSharpOption<long>.None;

        var sharesTransactedOption = request.SharesTransacted.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<long>.Some(request.SharesTransacted.Value) 
            : Microsoft.FSharp.Core.FSharpOption<long>.None;

        var percentOfClassOption = request.PercentOfClass.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<decimal>.Some(request.PercentOfClass.Value) 
            : Microsoft.FSharp.Core.FSharpOption<decimal>.None;

        var pricePerShareOption = request.PricePerShare.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<decimal>.Some(request.PricePerShare.Value) 
            : Microsoft.FSharp.Core.FSharpOption<decimal>.None;

        var totalValueOption = request.TotalValue.HasValue 
            ? Microsoft.FSharp.Core.FSharpOption<decimal>.Some(request.TotalValue.Value) 
            : Microsoft.FSharp.Core.FSharpOption<decimal>.None;

        var ownershipNatureOption = string.IsNullOrWhiteSpace(request.OwnershipNature) 
            ? Microsoft.FSharp.Core.FSharpOption<string>.None 
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(request.OwnershipNature);

        var ownershipEvent = new OwnershipEvent
        {
            Id = Guid.NewGuid(),
            EntityId = request.EntityId,
            CompanyTicker = request.CompanyTicker,
            CompanyCik = request.CompanyCik,
            FilingId = filingIdOption,
            EventType = request.EventType,
            TransactionType = transactionTypeOption,
            SharesBefore = sharesBeforeOption,
            SharesTransacted = sharesTransactedOption,
            SharesAfter = request.SharesAfter,
            PercentOfClass = percentOfClassOption,
            PricePerShare = pricePerShareOption,
            TotalValue = totalValueOption,
            TransactionDate = request.TransactionDate,
            FilingDate = request.FilingDate,
            IsDirect = request.IsDirect,
            OwnershipNature = ownershipNatureOption,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var eventId = await storage.SaveEvent(ownershipEvent);
        return Ok(new { id = eventId });
    }

    [HttpGet("ticker/{ticker}/roles")]
    public async Task<ActionResult> GetRolesByTicker([FromRoute] string ticker)
    {
        var t = new Ticker(ticker);
        var roles = await storage.GetRolesByCompany(t);
        return Ok(roles);
    }
}
