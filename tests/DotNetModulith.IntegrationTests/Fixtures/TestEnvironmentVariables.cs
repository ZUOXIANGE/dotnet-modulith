namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 管理集成测试运行期使用的环境变量集合
/// </summary>
public sealed class TestEnvironmentVariables
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    public TestEnvironmentVariables(IReadOnlyDictionary<string, string?> values)
    {
        _values = values;
    }

    /// <summary>
    /// 应用当前环境变量集合
    /// </summary>
    public void Apply()
    {
        foreach (var entry in _values)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// 清理当前环境变量集合
    /// </summary>
    public void Clear()
    {
        foreach (var key in _values.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}
