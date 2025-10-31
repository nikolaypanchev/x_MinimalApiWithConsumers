using System.Text.Json.Serialization;

namespace KafkaDemo.Functions;

public class KafkaHeader
{
    public KafkaHeader()
    {
    }

    [JsonPropertyName("Key")]
    public string Key { get; set; }

    [JsonPropertyName("Value")]
    public string Value { get; set; }

    /// <summary>
    /// Decodes the Base64-encoded header value.
    /// </summary>
    [JsonIgnore]
    public string DecodedValue
    {
        get
        {
            try
            {
                var bytes = Convert.FromBase64String(Value);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return Value; // fallback if not valid Base64
            }
        }
    }
}

//public class OrderValue
//{
//    public int orderId { get; set; }
//    public int status { get; set; }
//}