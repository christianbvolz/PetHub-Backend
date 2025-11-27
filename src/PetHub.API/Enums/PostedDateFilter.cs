using System.Text.Json.Serialization;

namespace pethub.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostedDateFilter
{
    Today,
    ThisWeek,
    ThisMonth,
    ThisYear,
}
