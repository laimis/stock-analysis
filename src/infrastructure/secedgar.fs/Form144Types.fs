namespace secedgar.fs

open System

/// Represents a parsed Form 144 filing (Report of Proposed Sale of Securities)
type ParsedForm144 = {
    /// CIK of the filer (the person or entity filing the Form 144)
    FilerCik: string option

    /// Name of the person for whose account the securities are to be sold
    PersonName: string

    /// Relationships to the issuer (e.g., "Director", "Officer")
    RelationshipsToIssuer: string list

    /// CIK of the issuer/company whose shares are being sold
    IssuerCik: string option

    /// Name of the company/issuer
    IssuerName: string

    /// Securities class title (e.g., "Common")
    SecuritiesClassTitle: string option

    /// Number of shares (units) proposed to be sold
    SharesToSell: int64

    /// Aggregate market value of the proposed sale
    AggregateMarketValue: decimal option

    /// Number of shares outstanding (used for percent calculations)
    SharesOutstanding: int64 option

    /// Approximate date of proposed sale
    ApproxSaleDate: DateTimeOffset option

    /// Name of exchange (e.g., "NASDAQ", "NYSE")
    Exchange: string option

    /// Nature of the acquisition transaction (e.g., "Restricted Stock Units", "Open Market Purchase")
    NatureOfAcquisition: string option

    /// Amount of securities acquired in the related acquisition
    SecuritiesAcquired: int64 option

    /// Date of notice (when the Form 144 was completed)
    NoticeDate: DateTimeOffset option

    /// Rule 10b5-1 plan adoption date, if applicable
    PlanAdoptionDate: DateTimeOffset option

    /// Whether this is an amendment (144/A)
    IsAmendment: bool

    /// Whether there is nothing to report for securities sold in the past 3 months
    NothingToReportPast3Months: bool

    /// Confidence score (0.0 to 1.0) indicating parsing confidence
    Confidence: float

    /// Raw XML content for debugging/manual review
    RawXml: string option

    /// Any parsing errors or warnings
    ParsingNotes: string list
}

/// Result type for parsing operations
type Form144ParsingResult =
    | Form144Success of ParsedForm144
    | Form144PartialSuccess of ParsedForm144 * string list
    | Form144Failure of string

/// Helper functions for working with parsed Form 144 data
module Form144Helpers =

    /// Calculate confidence score based on what was successfully parsed
    let calculateConfidence (parsed: ParsedForm144) =
        [ if parsed.FilerCik.IsSome then 0.15 else 0.0
          if not (String.IsNullOrWhiteSpace parsed.PersonName) then 0.20 else 0.0
          if not (String.IsNullOrWhiteSpace parsed.IssuerName) then 0.15 else 0.0
          if parsed.IssuerCik.IsSome then 0.15 else 0.0
          if parsed.SharesToSell > 0L then 0.15 else 0.0
          if parsed.AggregateMarketValue.IsSome then 0.05 else 0.0
          if parsed.ApproxSaleDate.IsSome then 0.10 else 0.0
          if parsed.RelationshipsToIssuer.Length > 0 then 0.05 else 0.0 ]
        |> List.sum

    /// Determine entity type based on relationships to issuer
    let determineEntityType (relationships: string list) =
        let hasRelationship (keyword: string) =
            relationships |> List.exists (fun r -> r.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        if hasRelationship "Officer" || hasRelationship "Director" || hasRelationship "Executive" then
            Some "EP" // Executive/C-Suite
        elif hasRelationship "10%" || hasRelationship "beneficial" then
            Some "IN" // Individual
        else
            Some "IN" // Default to individual for Form 144 filers

    /// Create a default/empty ParsedForm144 for building up
    let empty = {
        FilerCik = None
        PersonName = ""
        RelationshipsToIssuer = []
        IssuerCik = None
        IssuerName = ""
        SecuritiesClassTitle = None
        SharesToSell = 0L
        AggregateMarketValue = None
        SharesOutstanding = None
        ApproxSaleDate = None
        Exchange = None
        NatureOfAcquisition = None
        SecuritiesAcquired = None
        NoticeDate = None
        PlanAdoptionDate = None
        IsAmendment = false
        NothingToReportPast3Months = false
        Confidence = 0.0
        RawXml = None
        ParsingNotes = []
    }
