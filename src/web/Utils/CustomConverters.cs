using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using core.Shared;

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