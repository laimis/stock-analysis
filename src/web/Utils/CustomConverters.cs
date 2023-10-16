using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using core.fs.Options;
using core.fs.Reports;
using core.fs.Services;
using core.fs.Shared;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Stocks;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using core.Stocks;

namespace web.Utils;

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

public class GapTypeConverter : JsonConverter<GapAnalysis.GapType>
{
    public override GapAnalysis.GapType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return GapAnalysis.GapType.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, GapAnalysis.GapType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class PriceFrequencyConverter : JsonConverter<PriceFrequency>
{
    public override PriceFrequency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PriceFrequency.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, PriceFrequency value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class OutcomeReprtDurationConverter : JsonConverter<OutcomesReportDuration>
{
    public override OutcomesReportDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return OutcomesReportDuration.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, OutcomesReportDuration value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class OptionTypeConverter : JsonConverter<OptionType>
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

public class ValueFormatTypeConverter : JsonConverter<ValueFormat>
{
    public override ValueFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ValueFormat.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, ValueFormat value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class BrokerageOrderTypeConverter : JsonConverter<BrokerageOrderType>
{
    public override BrokerageOrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return BrokerageOrderType.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BrokerageOrderType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class BrokerageOrderDurationConverter : JsonConverter<BrokerageOrderDuration>
{
    public override BrokerageOrderDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return BrokerageOrderDuration.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BrokerageOrderDuration value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
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