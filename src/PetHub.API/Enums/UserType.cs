using System.Text.Json.Serialization;

namespace PetHub.API.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserType
{
    Person = 0,
    Shelter = 1,
}
