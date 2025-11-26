using System.Text.Json.Serialization;

namespace pethub.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgeRangeFilter
{
    Baby, // 0-11 months
    Young, // 12-36 months
    Adult, // 36-96 months
}
