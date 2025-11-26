using System.Text.Json.Serialization;

namespace pethub.Models;

public class PetTag
{
    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }

    public int TagId { get; set; }

    [JsonIgnore]
    public Tag? Tag { get; set; }
}
