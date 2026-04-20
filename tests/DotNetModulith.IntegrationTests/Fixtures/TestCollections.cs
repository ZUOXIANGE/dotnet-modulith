using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

[CollectionDefinition("Api collection")]
public sealed class ApiCollection : ICollectionFixture<ApiWebApplicationFactory>;

[CollectionDefinition("Messaging api collection")]
public sealed class MessagingApiCollection : ICollectionFixture<MessagingApiWebApplicationFactory>;
