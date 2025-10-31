using System.Text.Json.Serialization;

namespace KafkaDemo.Functions;

// optional: if you also want to deserialize the inner Value JSON
public class OrderValue
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

public enum Status
{
    Created,
    Declined
}