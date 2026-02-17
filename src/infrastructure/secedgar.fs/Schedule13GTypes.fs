namespace secedgar.fs

open System

/// Represents a parsed Schedule 13G/13G-A filing
type ParsedSchedule13G = {
    /// Name of the entity filing (e.g., "FMR LLC")
    FilerName: string
    
    /// CIK of the filer (e.g., "0000315066")
    FilerCik: string option
    
    /// Entity type code from SEC (IA, IC, BD, IN, HC, FI, EP, OO, etc.)
    EntityType: string option
    
    /// Name of the company/issuer being reported on (e.g., "Doximity, Inc.")
    IssuerName: string
    
    /// CIK of the issuer/company (e.g., "0001516513")
    IssuerCik: string option
    
    /// Stock ticker symbol (e.g., "DOCS")
    IssuerTicker: string option
    
    /// Total shares owned after the transaction/as of filing date
    SharesOwned: int64
    
    /// Percentage of class owned
    PercentOfClass: decimal
    
    /// Sole voting power (shares)
    SoleVotingPower: int64 option
    
    /// Shared voting power (shares)
    SharedVotingPower: int64 option
    
    /// Sole dispositive power (shares)
    SoleDispositivePower: int64 option
    
    /// Shared dispositive power (shares)
    SharedDispositivePower: int64 option
    
    /// Filing date (when filed with SEC)
    FilingDate: DateTimeOffset
    
    /// As-of date (the date the ownership is as of)
    AsOfDate: DateTimeOffset option
    
    /// Whether this is an amendment (13G/A)
    IsAmendment: bool
    
    /// Confidence score (0.0 to 1.0) indicating parsing confidence
    Confidence: float
    
    /// Raw XML content for debugging/manual review
    RawXml: string option
    
    /// Any parsing errors or warnings
    ParsingNotes: string list
}

/// Result type for parsing operations
type ParsingResult =
    | Success of ParsedSchedule13G
    | PartialSuccess of ParsedSchedule13G * string list
    | Failure of string

/// Helper functions for working with parsed Schedule 13G data
module Schedule13GHelpers =
    
    /// Calculate confidence score based on what was successfully parsed
    let calculateConfidence (parsed: ParsedSchedule13G) =
        [ if parsed.FilerCik.IsSome then 0.15 else 0.0
          if not (String.IsNullOrWhiteSpace parsed.FilerName) then 0.15 else 0.0
          if parsed.IssuerCik.IsSome then 0.15 else 0.0
          if not (String.IsNullOrWhiteSpace parsed.IssuerName) then 0.15 else 0.0
          if parsed.SharesOwned > 0L then 0.10 else 0.0
          if parsed.PercentOfClass > 0.0m then 0.10 else 0.0
          if parsed.SoleVotingPower.IsSome || parsed.SharedVotingPower.IsSome then 0.05 else 0.0
          if parsed.SoleDispositivePower.IsSome || parsed.SharedDispositivePower.IsSome then 0.05 else 0.0
          if parsed.AsOfDate.IsSome then 0.05 else 0.0
          if parsed.EntityType.IsSome then 0.05 else 0.0 ]
        |> List.sum
    
    /// Map SEC entity type codes to system codes
    let mapEntityType (secType: string option) =
        match secType with
        | Some "IA" -> Some "IA"
        | Some "IC" -> Some "IC"
        | Some "BD" -> Some "BD"
        | Some "IN" -> Some "IN"
        | Some "BK" -> Some "FI"
        | Some "HC" -> Some "HC"
        | Some "EP" -> Some "EP"
        | Some "OO" -> Some "OO"
        | _ -> None
    
    /// Create a default/empty ParsedSchedule13G for building up
    let empty = {
        FilerName = ""
        FilerCik = None
        EntityType = None
        IssuerName = ""
        IssuerCik = None
        IssuerTicker = None
        SharesOwned = 0L
        PercentOfClass = 0.0m
        SoleVotingPower = None
        SharedVotingPower = None
        SoleDispositivePower = None
        SharedDispositivePower = None
        FilingDate = DateTimeOffset.UtcNow
        AsOfDate = None
        IsAmendment = false
        Confidence = 0.0
        RawXml = None
        ParsingNotes = []
    }
