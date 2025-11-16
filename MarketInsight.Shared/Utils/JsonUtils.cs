
using System.Text.Json;

namespace MarketInsight.Shared.Utils
{
    public static class JsonUtils
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static string ToJson<T>(T value) =>
            JsonSerializer.Serialize(value, Default);

        public static T? FromJson<T>(string json) =>
            string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Default);
    }
}
