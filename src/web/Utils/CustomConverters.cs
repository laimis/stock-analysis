using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using core.Shared.Adapters.Brokerage;
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

public class BrokerageOrderTypeConverter : JsonConverter<BrokerageOrderType>
{
    public override BrokerageOrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new BrokerageOrderType(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BrokerageOrderType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class BrokerageOrderDurationConverter : JsonConverter<BrokerageOrderDuration>
{
    public override BrokerageOrderDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new BrokerageOrderDuration(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BrokerageOrderDuration value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
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