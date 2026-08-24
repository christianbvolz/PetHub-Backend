using System.Text.Json.Serialization;

namespace PetHub.API.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    AdoptionRequestCreated = 0,
    AdoptionRequestApproved = 1,
    AdoptionRequestRejected = 2,
    AdoptionRequestCancelled = 3,
}
