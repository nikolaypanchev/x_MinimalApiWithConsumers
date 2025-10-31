using System.Text.Json.Serialization;

namespace KafkaDemo.Functions;

public class KafkaMessage
{
    [JsonPropertyName("Offset")]
    public long Offset { get; set; }

    [JsonPropertyName("Partition")]
    public int Partition { get; set; }

    [JsonPropertyName("Topic")]
    public string Topic { get; set; }

    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("Value")]
    public string Value { get; set; }

    [JsonPropertyName("Key")]
    public string Key { get; set; }

    [JsonPropertyName("Headers")]
    public List<KafkaHeader> Headers { get; set; }
}