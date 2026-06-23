using System.Security.Claims;
using DotNetModulith.Abstractions.Authorization;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Storage.Api;
using DotNetModulith.Modules.Storage.Api.Contracts.Requests;
using DotNetModulith.Modules.Storage.Api.Contracts.Responses;
using DotNetModulith.Modules.Storage.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Storage.Api.Controllers;

/// <summary>
/// 文件存储接口
/// </summary>
[ApiController]
[Route("api/storage")]
public sealed class StorageController : ControllerBase
{
    private readonly IObjectStorageService _storageService;
    private readonly IStorageUploadSessionService _uploadSessionService;

    public StorageController(
        IObjectStorageService storageService,
        IStorageUploadSessionService uploadSessionService)
    {
        _storageService = storageService;
        _uploadSessionService = uploadSessionService;
    }

    /// <summary>
    /// 直传上传文件（文件经由 API 中转后写入对象存储）
    /// </summary>
    [Authorize(Policy = PermissionCodes.StorageUpload)]
    [HttpPost("upload/direct")]
    [RequestSizeLimit(104857600)]
    public async Task<ApiResponse<DirectUploadResponse>> UploadDirect([FromForm] IFormFile file, [FromForm] string? objectKey, CancellationToken ct)
    {
        var result = await _storageService.UploadDirectAsync(file, objectKey, ct);
        return ApiResponse.Success(new DirectUploadResponse(result.ObjectKey, result.ObjectUrl, result.Size));
    }

    /// <summary>
    /// 生成签名上传地址（客户端使用 URL 直接 PUT 到对象存储）
    /// </summary>
    [Authorize(Policy = PermissionCodes.StorageUpload)]
    [HttpPost("upload/presign")]
    public async Task<ApiResponse<PresignedUploadResponse>> CreatePresignedUpload([FromBody] CreatePresignedUploadRequest request, CancellationToken ct)
    {
        var result = await _uploadSessionService.CreateUploadSessionAsync(
            GetCurrentUserId(),
            new CreateUploadSessionInput(request.FileName, request.ContentType, request.Purpose),
            ct);

        return ApiResponse.Success(new PresignedUploadResponse(
            result.UploadId,
            result.ObjectKey,
            result.UploadUrl,
            result.ExpiresAtUtc));
    }

    /// <summary>
    /// 读取对象内容（仅用于本地联调和集成测试）
    /// </summary>
    [Authorize(Policy = PermissionCodes.StorageView)]
    [HttpGet("objects/{*objectKey}")]
    public async Task<IActionResult> GetObject([FromRoute] string objectKey, CancellationToken ct)
    {
        var bytes = await _storageService.GetObjectBytesAsync(objectKey, ct);
        return File(bytes, "application/octet-stream");
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new BusinessException("invalid token", ApiCodes.Auth.InvalidToken, StatusCodes.Status401Unauthorized);
    }
}
