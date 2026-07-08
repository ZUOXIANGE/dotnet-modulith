namespace DotNetModulith.Modules.Storage.Api;

/// <summary>
/// 对象存储读取服务，供其他模块获取签名读取 URL
/// </summary>
public interface IStorageReadService
{
    /// <summary>
    /// 根据对象 URL 或对象 Key 生成预签名读取 URL
    /// </summary>
    /// <param name="objectUrlOrKey">对象完整 URL 或对象 Key</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>预签名读取 URL；若入参为空则返回空字符串</returns>
    Task<string> GetPresignedReadUrlAsync(string objectUrlOrKey, CancellationToken ct);
}
