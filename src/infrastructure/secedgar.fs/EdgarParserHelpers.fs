namespace secedgar.fs

open System
open System.IO
open System.Globalization
open System.Xml
open System.Xml.Linq

/// Shared XML parsing utilities for SEC EDGAR form parsers.
/// Used by Schedule13GParser, Schedule13DParser, Form144Parser, and any future parsers.
module EdgarParserHelpers =

    /// Create secure XmlReader to prevent XXE attacks
    let createSecureXmlReader (xml: string) =
        let settings = XmlReaderSettings()
        settings.DtdProcessing <- DtdProcessing.Prohibit
        settings.XmlResolver <- null
        settings.CloseInput <- true
        let stringReader = new StringReader(xml)
        XmlReader.Create(stringReader, settings)

    /// Get XName with optional namespace
    let getXName (ns: string option) (name: string) =
        match ns with
        | Some nsUri -> XName.Get(name, nsUri)
        | None -> XName.Get(name)

    /// Try to get element value, handling missing elements and namespace. Value is trimmed.
    let tryGetElementValue (element: XElement) (ns: string option) (name: string) =
        let el = element.Element(getXName ns name)
        if el <> null && not (String.IsNullOrWhiteSpace(el.Value)) then
            Some (el.Value.Trim())
        else
            None

    /// Try to get first descendant element with optional namespace
    let tryGetDescendant (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.tryHead

    /// Try to get all descendant elements with optional namespace
    let getAllDescendants (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.toList

    /// Try to parse int64 from string, handling commas, apostrophes, whitespace, and decimals.
    /// Handles both comma (1,234,567) and apostrophe (1'234'567) thousands separators.
    let tryParseInt64 (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace("'", "").Replace(" ", "")
            match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | (true, num) -> Some (int64 (Math.Round(num, 0, MidpointRounding.AwayFromZero)))
            | _ -> None

    /// Try to parse decimal from string, handling commas and whitespace
    let tryParseDecimal (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace(" ", "")
            match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, num -> Some num
            | _ -> None

    /// Try to parse DateTimeOffset - handles multiple date formats used across SEC filings:
    /// - yyyy-MM-dd  (Schedule 13G, ISO format)
    /// - MM/dd/yyyy  (Schedule 13D, Form 144)
    /// - M/d/yyyy and variants
    let tryParseDate (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let formats = [| "MM/dd/yyyy"; "yyyy-MM-dd"; "M/d/yyyy"; "M/dd/yyyy"; "MM/d/yyyy" |]
            match DateTimeOffset.TryParseExact(v.Trim(), formats, null, DateTimeStyles.None) with
            | (true, date) -> Some date
            | _ -> None
