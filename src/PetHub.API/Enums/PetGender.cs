using System.Text.Json.Serialization;

namespace pethub.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetGender
{
    Male,
    Female,
    Unknown,
}
