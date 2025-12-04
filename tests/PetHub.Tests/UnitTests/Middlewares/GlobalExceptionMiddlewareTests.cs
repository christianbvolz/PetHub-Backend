using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PetHub.API.Middlewares;

namespace PetHub.Tests.UnitTests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    #region InvokeAsync - Success Cases

    [Fact]
    public async Task InvokeAsync_WithNoException_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        nextCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(TestConstants.Http.StatusOk);
    }

    #endregion

    #region InvokeAsync - OperationCanceledException

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_Returns499WithoutLogging()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) => throw new OperationCanceledException();

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        // 499 is a non-standard HTTP status code ("Client Closed Request"), commonly used by nginx and some web servers.
        _context.Response.StatusCode.Should().Be(TestConstants.Http.StatusClientClosedRequest);
        _loggerMock.Verify(
            x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
        );
    }

    #endregion

    #region Exception Types - Development Environment

    [Theory]
    [InlineData(
        typeof(KeyNotFoundException),
        TestConstants.Http.StatusNotFound,
        TestConstants.ExceptionTitles.ResourceNotFound,
        TestConstants.ExceptionMessages.PetNotFound
    )]
    [InlineData(
        typeof(ArgumentException),
        TestConstants.Http.StatusBadRequest,
        TestConstants.ExceptionTitles.InvalidArgument,
        TestConstants.ExceptionMessages.InvalidTagId
    )]
    [InlineData(
        typeof(ArgumentNullException),
        TestConstants.Http.StatusBadRequest,
        TestConstants.ExceptionTitles.InvalidArgument,
        TestConstants.ExceptionMessages.ArgumentNullPetDto
    )]
    [InlineData(
        typeof(UnauthorizedAccessException),
        TestConstants.Http.StatusForbidden,
        TestConstants.ExceptionTitles.AccessDenied,
        TestConstants.ExceptionMessages.UserNotAuthorized
    )]
    [InlineData(
        typeof(InvalidOperationException),
        TestConstants.Http.StatusConflict,
        TestConstants.ExceptionTitles.InvalidOperation,
        TestConstants.ExceptionMessages.PetAlreadyAdopted
    )]
    public async Task InvokeAsync_WithException_ReturnsCorrectStatusAndDetails_InDevelopment(
        Type exceptionType,
        int expectedStatusCode,
        string expectedTitle,
        string expectedMessage
    )
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var exception =
            exceptionType == typeof(ArgumentNullException)
                ? new ArgumentNullException("petDto")
                : (Exception)Activator.CreateInstance(exceptionType, expectedMessage)!;

        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(expectedStatusCode);
        _context.Response.ContentType.Should().Contain("application/json");

        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails.Status.Should().Be(expectedStatusCode);
        problemDetails.Title.Should().Be(expectedTitle);
        problemDetails.Detail.Should().Be(expectedMessage);
        problemDetails.Extensions.Should().ContainKey("stackTrace");
        problemDetails.Extensions.Should().ContainKey("exceptionType");
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_Returns500WithDetails_InDevelopment()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var exception = new Exception(TestConstants.ExceptionMessages.DatabaseConnectionFailed);
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(TestConstants.Http.StatusInternalServerError);
        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails.Status.Should().Be(TestConstants.Http.StatusInternalServerError);
        problemDetails.Title.Should().Be(TestConstants.ExceptionTitles.AnErrorOccurred);
        problemDetails.Detail.Should().Be(TestConstants.ExceptionMessages.DatabaseConnectionFailed);
        problemDetails.Extensions.Should().ContainKey("stackTrace");
        problemDetails.Extensions.Should().ContainKey("exceptionType");
    }

    #endregion

    #region Exception Types - Production Environment

    [Theory]
    [InlineData(
        typeof(KeyNotFoundException),
        TestConstants.Http.StatusNotFound,
        TestConstants.ExceptionMessages.PetWithIdNotFound
    )]
    [InlineData(
        typeof(ArgumentException),
        TestConstants.Http.StatusBadRequest,
        TestConstants.ExceptionMessages.InvalidTagIdFormat
    )]
    public async Task InvokeAsync_WithException_ReturnsMessageWithoutDetails_InProduction(
        Type exceptionType,
        int expectedStatusCode,
        string expectedMessage
    )
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var exception = (Exception)Activator.CreateInstance(exceptionType, expectedMessage)!;
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(expectedStatusCode);
        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails.Detail.Should().Be(expectedMessage);
        problemDetails.Extensions.Should().NotContainKey("stackTrace");
        problemDetails.Extensions.Should().NotContainKey("exceptionType");
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_Returns500WithGenericMessage_InProduction()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var exception = new Exception("Connection string not found in configuration");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(TestConstants.Http.StatusInternalServerError);
        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails
            .Detail.Should()
            .Be("An internal server error occurred. Please try again later.");
        problemDetails.Extensions.Should().NotContainKey("stackTrace");
        problemDetails.Extensions.Should().NotContainKey("exceptionType");
    }

    #endregion

    #region Logging

    [Fact]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var exception = new Exception(TestConstants.ExceptionMessages.TestException);
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("An unhandled exception occurred")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    #endregion

    #region Response Format

    [Fact]
    public async Task InvokeAsync_IncludesCorrectTypeUrl()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        RequestDelegate next = (HttpContext ctx) => throw new KeyNotFoundException();

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails.Type.Should().Be("https://httpstatuses.com/404");
    }

    [Fact]
    public async Task InvokeAsync_IncludesRequestPath()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _context.Request.Path = TestConstants.Http.ValidRequestPath;
        RequestDelegate next = (HttpContext ctx) => throw new KeyNotFoundException();

        var middleware = new GlobalExceptionMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse();
        problemDetails.Instance.Should().Be(TestConstants.Http.ValidRequestPath);
    }

    #endregion

    #region Helper Methods

    private async Task<ProblemDetails> GetProblemDetailsFromResponse()
    {
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();

        responseBody
            .Should()
            .NotBeNullOrEmpty("middleware should write ProblemDetails to response body");

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails
            .Should()
            .NotBeNull(
                "response body should be valid ProblemDetails JSON. Actual content: {0}",
                responseBody
            );

        return problemDetails!;
    }

    #endregion
}
