namespace web.Utils

open System
open System.Text.Json
open System.Text.Json.Serialization
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Alerts
open core.fs.Options
open core.fs.Reports
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Stocks
open core.Shared
open core.Stocks

[<AbstractClass>]
type GenericConverterWithToString<'T>() =
    inherit JsonConverter<'T>()
    
    override this.Write(writer: Utf8JsonWriter, value: 'T, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())

type TickerConverter() =
    inherit JsonConverter<Ticker>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        Ticker(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: Ticker, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.Value)

type OptionTickerConverter() =
    inherit JsonConverter<OptionTicker>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OptionTicker(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: OptionTicker, options: JsonSerializerOptions) =
        let (OptionTicker str) = value
        writer.WriteStringValue(str)

type UserIdConverter() =
    inherit JsonConverter<UserId>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        UserId(Guid(reader.GetString()))
    
    override this.Write(writer: Utf8JsonWriter, value: UserId, options: JsonSerializerOptions) =
        let (UserId guid) = value
        writer.WriteStringValue(guid)

type GapTypeConverter() =
    inherit GenericConverterWithToString<GapAnalysis.GapType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        GapAnalysis.GapType.FromString(reader.GetString())

type PriceFrequencyConverter() =
    inherit GenericConverterWithToString<PriceFrequency>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        PriceFrequency.FromString(reader.GetString())

type OutcomeReportDurationConverter() =
    inherit GenericConverterWithToString<OutcomesReportDuration>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OutcomesReportDuration.FromString(reader.GetString())

type OptionTypeConverter() =
    inherit GenericConverterWithToString<OptionType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OptionType.FromString(reader.GetString())

type GradeConverter() =
    inherit JsonConverter<TradeGrade>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        TradeGrade(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: TradeGrade, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.Value)

type ValueFormatTypeConverter() =
    inherit GenericConverterWithToString<ValueFormat>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        ValueFormat.FromString(reader.GetString())

type OutcomeTypeTypeConverter() =
    inherit GenericConverterWithToString<OutcomeType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OutcomeType.FromString(reader.GetString())

type BrokerageOrderTypeConverter() =
    inherit GenericConverterWithToString<BrokerageOrderType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        BrokerageOrderType.FromString(reader.GetString())

type BrokerageOrderDurationConverter() =
    inherit GenericConverterWithToString<BrokerageOrderDuration>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        BrokerageOrderDuration.FromString(reader.GetString())

type PositionEventTypeConverter() =
    inherit JsonConverter<PositionEventType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        PositionEventType(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: PositionEventType, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.Value)

type ChartAnnotationLineTypeConverter() =
    inherit GenericConverterWithToString<ChartAnnotationLineType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        ChartAnnotationLineType.FromString(reader.GetString())

type DataPointChartTypeConverter() =
    inherit GenericConverterWithToString<DataPointChartType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        DataPointChartType.FromString(reader.GetString())

type StockPositionIdConverter() =
    inherit JsonConverter<StockPositionId>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        StockPositionId(Guid.Parse(reader.GetString()))
    
    override this.Write(writer: Utf8JsonWriter, value: StockPositionId, options: JsonSerializerOptions) =
        let (StockPositionId guid) = value
        writer.WriteStringValue(guid.ToString())

type SentimentTypeConverter() =
    inherit GenericConverterWithToString<SentimentType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        SentimentType.FromString(reader.GetString())

type TrendDirectionConverter() =
    inherit GenericConverterWithToString<InflectionPoints.TrendDirection>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        InflectionPoints.TrendDirection.FromString(reader.GetString())

type InflectionPointsTypeConverter() =
    inherit GenericConverterWithToString<InflectionPoints.InfectionPointType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        InflectionPoints.InfectionPointType.FromString(reader.GetString())

type TrendTypeConverter() =
    inherit GenericConverterWithToString<Trends.TrendType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        Trends.TrendType.FromString(reader.GetString())

type OrderStatusConverter() =
    inherit GenericConverterWithToString<OrderStatus>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("OrderStatus is not deserializable"))

type OptionOrderTypeConverter() =
    inherit GenericConverterWithToString<OptionOrderType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("OrderType is not deserializable"))

type StockOrderTypeConverter() =
    inherit GenericConverterWithToString<StockOrderType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("OrderType is not deserializable"))

type StockOrderInstructionConverter() =
    inherit GenericConverterWithToString<StockOrderInstruction>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("OrderInstruction is not deserializable"))

type OptionOrderInstructionConverter() =
    inherit GenericConverterWithToString<OptionOrderInstruction>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("OrderInstruction is not deserializable"))

type AssetTypeConverter() =
    inherit GenericConverterWithToString<AssetType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException("AssetType is not deserializable"))

type AccountTransactionTypeConverter() =
    inherit GenericConverterWithToString<AccountTransactionType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException())

type StockTransactionTypeConverter() =
    inherit GenericConverterWithToString<StockTransactionType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        raise (NotImplementedException())

type OptionTypeTypeConverter() =
    inherit JsonConverter<OptionType>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OptionType.FromString(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: OptionType, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())

type OptionPositionIdConverter() =
    inherit JsonConverter<OptionPositionId>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OptionPositionId(Guid.Parse(reader.GetString()))
    
    override this.Write(writer: Utf8JsonWriter, value: OptionPositionId, options: JsonSerializerOptions) =
        let (OptionPositionId guid) = value
        writer.WriteStringValue(guid.ToString())

type OptionExpirationConverter() =
    inherit JsonConverter<OptionExpiration>()
    
    override this.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
        OptionExpiration.create(reader.GetString())
    
    override this.Write(writer: Utf8JsonWriter, value: OptionExpiration, options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())
