using System.Text.Json.Serialization;

namespace pethub.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // User Profile Picture (Single photo per user)
    public string ProfilePictureUrl { get; set; } = string.Empty;

    // Contact Information
    public string PhoneNumber { get; set; } = string.Empty;

    // --- FULL ADDRESS DETAILS ---

    // Zip Code (CEP) - 8 digits
    public string ZipCode { get; set; } = string.Empty;

    // State (UF) - e.g., SP, RJ, RS
    public string State { get; set; } = string.Empty;

    // City
    public string City { get; set; } = string.Empty;

    // Neighborhood (Bairro)
    public string Neighborhood { get; set; } = string.Empty;

    // Street Name (Logradouro)
    public string Street { get; set; } = string.Empty;

    // House Number (User must provide this manually)
    public string Number { get; set; } = string.Empty;

    // Navigation Property: One user can register multiple pets
    [JsonIgnore]
    public List<Pet> Pets { get; set; } = new();
}
