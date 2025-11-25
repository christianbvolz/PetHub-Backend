using System.Text.Json.Serialization;

namespace pethub.DTOs;

public class ViaCepResponse
{
    // Mapeia o campo "localidade" do JSON para a propriedade "City"
    [JsonPropertyName("localidade")]
    public string City { get; set; } = string.Empty;

    // Mapeia o campo "uf" do JSON para a propriedade "State"
    [JsonPropertyName("uf")]
    public string State { get; set; } = string.Empty;

    // Se precisar da rua no futuro:
    // [JsonPropertyName("logradouro")]
    // public string Street { get; set; }

    // Caso o CEP não exista, o ViaCEP retorna { "erro": true }
    [JsonPropertyName("erro")]
    public bool Error { get; set; }
}
