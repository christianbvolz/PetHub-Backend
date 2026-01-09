using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Disables parallel test execution to avoid shared-mock race conditions
// Tests use a shared WebApplicationFactory and shared mocks at class level.
