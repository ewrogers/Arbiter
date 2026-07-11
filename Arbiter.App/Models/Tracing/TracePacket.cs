using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Arbiter.Json.Converters;
using Arbiter.Net.Filters;

namespace Arbiter.App.Models.Tracing;

public class TracePacket
{
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }

    [JsonPropertyName("direction")] public PacketDirection Direction { get; set; }

    [JsonPropertyName("name")] public string? ClientName { get; set; }

    [JsonPropertyName("connectionId")] public int? ConnectionId { get; set; }

    [JsonPropertyName("command")]
    [JsonConverter(typeof(HexNumberConverterFactory))]
    public byte Command { get; set; }

    [JsonPropertyName("sequence")]
    [JsonConverter(typeof(HexNumberConverterFactory))]
    public byte? Sequence { get; set; }

    [JsonPropertyName("raw")]
    [JsonConverter(typeof(HexByteEnumerableConverterFactory))]
    public IReadOnlyList<byte> RawData { get; set; } = [];

    [JsonPropertyName("payload")]
    [JsonConverter(typeof(HexByteEnumerableConverterFactory))]
    public IReadOnlyCollection<byte> Payload { get; set; } = [];

    [JsonPropertyName("action")] public NetworkFilterAction FilterAction { get; set; } = NetworkFilterAction.Allow;
    
    [JsonPropertyName("filtered")]
    [JsonConverter(typeof(HexByteEnumerableConverterFactory))]
    public IReadOnlyCollection<byte>? FilteredPayload { get; set; }

    [JsonPropertyName("checksum")]
    [JsonConverter(typeof(HexNumberConverterFactory))]
    public uint? Checksum { get; set; }
}
