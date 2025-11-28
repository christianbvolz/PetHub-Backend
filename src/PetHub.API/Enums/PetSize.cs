using System.Text.Json.Serialization;

namespace PetHub.API.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetSize
{
    Small,
    Medium,
    Large,
}
