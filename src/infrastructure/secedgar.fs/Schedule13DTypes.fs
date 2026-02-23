namespace secedgar.fs

open System

/// Represents a parsed Schedule 13D/13D-A filing
type ParsedSchedule13D = {
    /// Name of the entity filing (e.g., "Pictet Asset Management SA")
    FilerName: string
    
    /// CIK of the filer (e.g., "0001361570")
    FilerCik: string option
    
    /// Entity type code inferred from context (IA, IC, BD, IN, HC, FI, EP, OO, etc.)
    EntityType: string option
    
    /// Country of citizenship/incorporation (from Item 2)
    Citizenship: string option
    
    /// Name of the company/issuer being reported on (e.g., "Elastic N.V.")
    IssuerName: string
    
    /// CIK of the issuer/company (e.g., "0001707753")
    IssuerCik: string option
    
    /// CUSIP of the issuer's securities
    IssuerCusip: string option
    
    /// Stock ticker symbol (may not be present in the XML itself)
    IssuerTicker: string option
    
    /// Class/type of securities (e.g., "COMMON STOCK")
    SecuritiesClassTitle: string option
    
    /// Total shares owned after the transaction/as of filing date
    SharesOwned: int64
    
    /// Percentage of class owned
    PercentOfClass: decimal
    
    /// Sole voting power (shares, extracted from item 5 narrative text)
    SoleVotingPower: int64 option
    
    /// Shared voting power (shares, extracted from item 5 narrative text)
    SharedVotingPower: int64 option
    
    /// Sole dispositive power (shares, extracted from item 5 narrative text)
    SoleDispositivePower: int64 option
    
    /// Shared dispositive power (shares, extracted from item 5 narrative text)
    SharedDispositivePower: int64 option
    
    /// Purpose of acquisition (item 4, narrative text)
    AcquisitionPurpose: string option
    
    /// Filing date (when filed with SEC)
    FilingDate: DateTimeOffset
    
    /// Date of event triggering filing (item 1 / coverPageHeader dateOfEvent)
    AsOfDate: DateTimeOffset option
    
    /// Whether this is an amendment (13D/A)
    IsAmendment: bool
    
    /// Confidence score (0.0 to 1.0) indicating parsing confidence
    Confidence: float
    
    /// Raw XML content for debugging/manual review
    RawXml: string option
    
    /// Any parsing errors or warnings
    ParsingNotes: string list
}

/// Result type for parsing operations
type Schedule13DParsingResult =
    | Schedule13DSuccess of ParsedSchedule13D
    | Schedule13DPartialSuccess of ParsedSchedule13D * string list
    | Schedule13DFailure of string

/// Helper functions for working with parsed Schedule 13D data
module Schedule13DHelpers =
    
    /// Calculate confidence score based on what was successfully parsed
    let calculateConfidence (parsed: ParsedSchedule13D) =
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
    
    /// Infer entity type from available context (13D does not have a structured entity type field)
    let inferEntityType (citizenship: string option) =
        // Without a structured type field, default to OO (Other Org)
        // Subclasses can enhance this based on filer name patterns if needed
        Some "OO"
    
    /// Create a default/empty ParsedSchedule13D for building up
    let empty = {
        FilerName = ""
        FilerCik = None
        EntityType = None
        Citizenship = None
        IssuerName = ""
        IssuerCik = None
        IssuerCusip = None
        IssuerTicker = None
        SecuritiesClassTitle = None
        SharesOwned = 0L
        PercentOfClass = 0.0m
        SoleVotingPower = None
        SharedVotingPower = None
        SoleDispositivePower = None
        SharedDispositivePower = None
        AcquisitionPurpose = None
        FilingDate = DateTimeOffset.UtcNow
        AsOfDate = None
        IsAmendment = false
        Confidence = 0.0
        RawXml = None
        ParsingNotes = []
    }
