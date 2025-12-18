using PetHub.API.DTOs.Common;

namespace PetHub.Tests.IntegrationTests.Helpers;

/// <summary>
/// Extension methods for HttpResponseMessage to simplify ApiResponse handling in tests
/// </summary>
public static class HttpResponseExtensions
{
    /// <summary>
    /// Deserializes the response content to ApiResponse<T> and extracts the Data property
    /// </summary>
    public static async Task<T?> ReadApiResponseDataAsync<T>(this HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return apiResponse != null ? apiResponse.Data : default;
    }

    /// <summary>
    /// Deserializes the response content to ApiResponse<T>
    /// </summary>
    public static async Task<ApiResponse<T>?> ReadApiResponseAsync<T>(
        this HttpResponseMessage response
    )
    {
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
    }
}
