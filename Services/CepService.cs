using System.Text.Json;
using pethub.DTOs;

namespace pethub.Services;

public class CepService(HttpClient httpClient)
{
    private const string BaseUrl = "https://viacep.com.br/ws/";

    public async Task<ViaCepResponse?> GetAddressByZipCode(string zipCode)
    {
        // 1. Clean the input (remove hyphens or spaces)
        var cleanZipCode = zipCode.Replace("-", "").Trim();

        // Validate length (Brazilian ZipCodes are always 8 digits)
        if (cleanZipCode.Length != 8)
            return null;

        try
        {
            // 2. Call ViaCEP API
            var response = await httpClient.GetAsync($"{BaseUrl}{cleanZipCode}/json/");

            if (!response.IsSuccessStatusCode)
                return null;

            // 3. Convert JSON to C# Object
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ViaCepResponse>(content);

            // Check if API returned a logical error (e.g., valid format but non-existent ZipCode)
            if (result is not null && result.Error)
                return null;

            return result;
        }
        catch
        {
            // Handle network errors gracefully
            return null;
        }
    }
}
