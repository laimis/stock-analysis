using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using core.fs;
using core.fs.Accounts;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Stocks;
using core.fs.Alerts;
using core.fs.Options;
using core.fs.Reports;
using core.fs.Services;
using core.fs.Services.Analysis;
using core.fs.Stocks;
using core.Shared;
using core.Stocks;

namespace web.Utils;

public abstract class GenericConverterWithToString<T> : JsonConverter<T>
{
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class TickerConverter : JsonConverter<Ticker>
{
    public override Ticker Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Ticker(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, Ticker value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class OptionTickerConverter : JsonConverter<OptionTicker>
{
    public override OptionTicker Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OptionTicker.NewOptionTicker(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, OptionTicker value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Item);
    }
}

public class UserIdConverter : JsonConverter<UserId>
{
    public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return UserId.NewUserId(new Guid(reader.GetString()));
    }

    public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Item);
    }
}

public class GapTypeConverter : GenericConverterWithToString<GapAnalysis.GapType>
{
    public override GapAnalysis.GapType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return GapAnalysis.GapType.FromString(reader.GetString());
    }
}

public class PriceFrequencyConverter : GenericConverterWithToString<PriceFrequency>
{
    public override PriceFrequency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PriceFrequency.FromString(reader.GetString());
    }
}

public class OutcomeReprtDurationConverter : GenericConverterWithToString<OutcomesReportDuration>
{
    public override OutcomesReportDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OutcomesReportDuration.FromString(reader.GetString());
    }
}

public class OptionTypeConverter : GenericConverterWithToString<OptionType>
{
    public override OptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OptionType.FromString(reader.GetString());
    }
}

public class GradeConverter : JsonConverter<TradeGrade>
{
    public override TradeGrade Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new TradeGrade(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, TradeGrade value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class ValueFormatTypeConverter : GenericConverterWithToString<ValueFormat>
{
    public override ValueFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ValueFormat.FromString(reader.GetString());
    }
}

public class OutcomeTypeTypeConverter : GenericConverterWithToString<OutcomeType>
{
    public override OutcomeType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OutcomeType.FromString(reader.GetString());
    }
}

public class BrokerageOrderTypeConverter : GenericConverterWithToString<BrokerageOrderType>
{
    public override BrokerageOrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return BrokerageOrderType.FromString(reader.GetString());
    }
}

public class BrokerageOrderDurationConverter : GenericConverterWithToString<BrokerageOrderDuration>
{
    public override BrokerageOrderDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return BrokerageOrderDuration.FromString(reader.GetString());
    }
}

public class PositionEventTypeConverter : JsonConverter<PositionEventType>
{
    public override PositionEventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new PositionEventType(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, PositionEventType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class ChartAnnotationLineTypeConverter : GenericConverterWithToString<ChartAnnotationLineType>
{
    public override ChartAnnotationLineType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ChartAnnotationLineType.FromString(reader.GetString());
    }
}

public class DataPointChartTypeConverter : GenericConverterWithToString<DataPointChartType>
{
    public override DataPointChartType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DataPointChartType.FromString(reader.GetString());
    }
}

public class StockPositionIdConverter : JsonConverter<StockPositionId>
{
    public override StockPositionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return StockPositionId.NewStockPositionId(Guid.Parse(reader.GetString()));
    }
    
    public override void Write(Utf8JsonWriter writer, StockPositionId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Item.ToString());
    }
}

public class SentimentTypeConverter : GenericConverterWithToString<SentimentType>
{
    public override SentimentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return SentimentType.FromString(reader.GetString());
    }
}

public class TrendDirectionConverter : GenericConverterWithToString<Trends.TrendDirection>
{
    public override Trends.TrendDirection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Trends.TrendDirection.FromString(reader.GetString());
    }
}

public class TrendTypeConverter : GenericConverterWithToString<Trends.TrendType>
{
    public override Trends.TrendType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Trends.TrendType.FromString(reader.GetString());
    }
}

public class OrderStatusConverter : GenericConverterWithToString<OrderStatus>
{
    public override OrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("OrderStatus is not deserializable");
    }
}

public class OptionOrderTypeConverter : GenericConverterWithToString<OptionOrderType>
{
    public override OptionOrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("OrderType is not deserializable");
    }
}

public class StockOrderTypeConverter : GenericConverterWithToString<StockOrderType>
{
    public override StockOrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("OrderType is not deserializable");
    }
}

public class StockOrderInstructionConverter : GenericConverterWithToString<StockOrderInstruction>
{
    public override StockOrderInstruction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("OrderInstruction is not deserializable");
    }
}

public class OptionOrderInstructionConverter : GenericConverterWithToString<OptionOrderInstruction>
{
    public override OptionOrderInstruction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("OrderInstruction is not deserializable");
    }
}

public class AssetTypeConverter : GenericConverterWithToString<AssetType>
{
    public override AssetType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("AssetType is not deserializable");
    }
}

public class AccountTransactionTypeConverter : GenericConverterWithToString<AccountTransactionType>
{
    public override AccountTransactionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class StockTransactionTypeConverter : GenericConverterWithToString<StockTransactionType>
{
    public override StockTransactionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class OptionTypeTypeConverter : JsonConverter<OptionType>
{
public override OptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    return OptionType.FromString(reader.GetString());
}
    
public override void Write(Utf8JsonWriter writer, OptionType value, JsonSerializerOptions options)
{
    writer.WriteStringValue(value.ToString());
}
}

public class OptionPositionIdConverter : JsonConverter<OptionPositionId>
{
    public override OptionPositionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OptionPositionId.NewOptionPositionId(Guid.Parse(reader.GetString()));
    }
    
    public override void Write(Utf8JsonWriter writer, OptionPositionId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Item.ToString());
    }
}

public class OptionExpirationConverter : JsonConverter<OptionExpiration>
{
    public override OptionExpiration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OptionExpiration.create(reader.GetString());
    }
    
    public override void Write(Utf8JsonWriter writer, OptionExpiration value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
