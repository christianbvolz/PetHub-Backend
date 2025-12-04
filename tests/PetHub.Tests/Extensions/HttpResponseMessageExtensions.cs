using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluentAssertions.Execution;

namespace PetHub.Tests.Extensions;

/// <summary>
/// Fluent extension methods for HTTP response assertions in tests
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Asserts that the response has a 200 OK status code
    /// </summary>
    public static HttpResponseMessage ShouldBeOk(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 201 Created status code
    /// </summary>
    public static HttpResponseMessage ShouldBeCreated(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 204 No Content status code
    /// </summary>
    public static HttpResponseMessage ShouldBeNoContent(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 400 Bad Request status code
    /// </summary>
    public static HttpResponseMessage ShouldBeBadRequest(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 401 Unauthorized status code
    /// </summary>
    public static HttpResponseMessage ShouldBeUnauthorized(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 403 Forbidden status code
    /// </summary>
    public static HttpResponseMessage ShouldBeForbidden(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 404 Not Found status code
    /// </summary>
    public static HttpResponseMessage ShouldBeNotFound(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 409 Conflict status code
    /// </summary>
    public static HttpResponseMessage ShouldBeConflict(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        return response;
    }

    /// <summary>
    /// Asserts that the response has a 422 Unprocessable Entity status code
    /// </summary>
    public static HttpResponseMessage ShouldBeUnprocessableEntity(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        return response;
    }

    /// <summary>
    /// Asserts that the response body contains a validation error with the specified message
    /// </summary>
    public static async Task<HttpResponseMessage> WithValidationError(
        this HttpResponseMessage response,
        string fieldName,
        string? errorMessage = null
    )
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(fieldName);

        if (errorMessage != null)
        {
            content.Should().Contain(errorMessage);
        }

        return response;
    }

    /// <summary>
    /// Asserts that the response body contains the specified error message
    /// </summary>
    public static async Task<HttpResponseMessage> WithErrorMessage(
        this HttpResponseMessage response,
        string errorMessage
    )
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(errorMessage);
        return response;
    }

    /// <summary>
    /// Asserts that the response body contains the specified title
    /// </summary>
    public static async Task<HttpResponseMessage> WithTitle(
        this HttpResponseMessage response,
        string title
    )
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(title);
        return response;
    }

    /// <summary>
    /// Asserts that the response can be deserialized to the specified type
    /// </summary>
    public static async Task<T> WithContent<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>();
        content.Should().NotBeNull();
        return content!;
    }

    /// <summary>
    /// Asserts that the response can be deserialized to the specified type and applies additional assertions
    /// </summary>
    public static async Task<HttpResponseMessage> WithContent<T>(
        this HttpResponseMessage response,
        Action<T> assertions
    )
    {
        var content = await response.Content.ReadFromJsonAsync<T>();
        content.Should().NotBeNull();
        assertions(content!);
        return response;
    }

    /// <summary>
    /// Asserts multiple conditions on the response
    /// </summary>
    public static HttpResponseMessage ShouldSatisfy(
        this HttpResponseMessage response,
        params Action<HttpResponseMessage>[] assertions
    )
    {
        using (new AssertionScope())
        {
            foreach (var assertion in assertions)
            {
                assertion(response);
            }
        }
        return response;
    }

    /// <summary>
    /// Asserts that the response has a Location header (typically for 201 Created responses)
    /// </summary>
    public static HttpResponseMessage WithLocation(this HttpResponseMessage response)
    {
        response.Headers.Location.Should().NotBeNull();
        return response;
    }

    /// <summary>
    /// Asserts that the response has a Location header with the specified URI
    /// </summary>
    public static HttpResponseMessage WithLocation(
        this HttpResponseMessage response,
        string expectedLocation
    )
    {
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(expectedLocation);
        return response;
    }
}
