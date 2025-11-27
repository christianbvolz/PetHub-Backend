using System.Text.Json.Serialization;

namespace pethub.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetSize
{
    Small,
    Medium,
    Large,
}
