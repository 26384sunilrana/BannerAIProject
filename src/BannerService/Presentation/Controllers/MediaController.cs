namespace BannerService.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BannerService.Application.Dto;
using BannerService.Application.Services;
using System.Security.Claims;

[ApiController]
[Route("api/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaUploadService _mediaUploadService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaUploadService mediaUploadService,
        ILogger<MediaController> logger)
    {
        _mediaUploadService = mediaUploadService;
        _logger = logger;
    }

    private Guid GetShopId()
    {
        var shopIdClaim = User.FindFirst("shop_id")?.Value;
        if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id");
        return shopId;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user ID");
        return userId;
    }

    [HttpPost("upload/initialize")]
    [ProducesResponseType(typeof(InitializeUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitializeUpload([FromBody] InitializeUploadRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("FileName required");
            if (string.IsNullOrWhiteSpace(request.ContentType))
                return BadRequest("ContentType required");
            if (request.TotalSizeBytes <= 0)
                return BadRequest("TotalSizeBytes must be > 0");
            if (request.FileType < 1 || request.FileType > 3)
                return BadRequest("FileType must be 1-3");

            var shopId = GetShopId();
            var userId = GetUserId();

            var mediaFile = await _mediaUploadService.InitializeUploadAsync(
                request.FileName, request.ContentType, request.TotalSizeBytes,
                request.FileType, shopId, userId);

            var totalChunks = (int)Math.Ceiling((double)request.TotalSizeBytes / (100 * 1024 * 1024));

            return Ok(new InitializeUploadResponseDto
            {
                MediaFileId = mediaFile.Id,
                FileName = mediaFile.FileName,
                TotalSizeBytes = mediaFile.SizeBytes,
                TotalChunks = totalChunks
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{mediaFileId}/chunks/{chunkNumber}")]
    [ProducesResponseType(typeof(ChunkUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadChunk(
        Guid mediaFileId, int chunkNumber,
        [FromHeader(Name = "X-Checksum-MD5")] string checksumMD5)
    {
        try
        {
            if (mediaFileId == Guid.Empty)
                return BadRequest("Invalid media file ID");
            if (chunkNumber < 0)
                return BadRequest("ChunkNumber must be >= 0");
            if (string.IsNullOrWhiteSpace(checksumMD5))
                return BadRequest("X-Checksum-MD5 header required");

            var shopId = GetShopId();

            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var result = await _mediaUploadService.UploadChunkAsync(
                mediaFileId, chunkNumber, ms, checksumMD5, shopId);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found");
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{mediaFileId}/complete")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteUpload(Guid mediaFileId)
    {
        try
        {
            var shopId = GetShopId();
            var mediaFile = await _mediaUploadService.CompleteUploadAsync(mediaFileId, shopId);
            return Ok(mediaFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found");
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{mediaFileId}")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaFile(Guid mediaFileId)
    {
        try
        {
            var shopId = GetShopId();
            var mediaFile = await _mediaUploadService.GetMediaFileAsync(mediaFileId, shopId);
            if (mediaFile == null)
                return NotFound();
            return Ok(mediaFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
    }

    [HttpGet("{mediaFileId}/url")]
    [ProducesResponseType(typeof(MediaUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaUrl(Guid mediaFileId, [FromQuery] int? expirationMinutes = null)
    {
        try
        {
            var shopId = GetShopId();
            var url = await _mediaUploadService.GetMediaUrlAsync(mediaFileId, shopId, expirationMinutes);
            return Ok(url);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found");
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { error = ex.Message });
        }
    }
}
