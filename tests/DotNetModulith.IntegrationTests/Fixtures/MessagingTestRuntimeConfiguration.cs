namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 为消息闭环集成测试创建运行期环境变量配置
/// </summary>
internal static class MessagingTestRuntimeConfiguration
{
    public static TestEnvironmentVariables Create(string connectionString, RabbitMqFixture rabbitMqFixture)
        => new(new Dictionary<string, string?>
        {
            ["ConnectionStrings__modulithdb"] = connectionString,
            ["RabbitMQ__HostName"] = rabbitMqFixture.HostName,
            ["RabbitMQ__Port"] = rabbitMqFixture.Port.ToString(),
            ["RabbitMQ__UserName"] = "guest",
            ["RabbitMQ__Password"] = "guest",
            ["RabbitMQ__VirtualHost"] = "/",
            ["OpenObserve__Enabled"] = "false"
        });
}
