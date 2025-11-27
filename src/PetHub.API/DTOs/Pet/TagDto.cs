using pethub.Enums;

namespace pethub.DTOs.Pet;

public class TagDto
{
    public string Name { get; set; } = string.Empty;
    public TagCategory Category { get; set; }
}
