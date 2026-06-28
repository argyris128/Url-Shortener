using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/urls")]
[Produces("application/json")]
public class UrlController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly ILogger<UrlController> _logger;

    public UrlController(IUrlShortenerService service, ILogger<UrlController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Shorten a new URL
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShortenedUrlResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalUrl))
            return BadRequest(new ApiResponse<object>(false, "URL cannot be empty."));

        var result = await _service.CreateShortUrlAsync(request.OriginalUrl, request.Alias);

        if (result is null)
        {
            var isAliasConflict = request.Alias is not null;
            return isAliasConflict
                ? Conflict(new ApiResponse<object>(false, $"Alias '{request.Alias}' is already taken."))
                : BadRequest(new ApiResponse<object>(false, "Invalid URL format."));
        }

        _logger.LogInformation("Created short URL: {ShortCode} -> {OriginalUrl}", result.ShortCode, result.OriginalUrl);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<ShortenedUrlResponse>(
            true,
            "Short URL created successfully.",
            result.MapToResponse(baseUrl)
        ));
    }

    // List all shortened URLs
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShortenedUrlResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var all = await _service.GetAllAsync();
        var responses = all.Select(u => u.MapToResponse(baseUrl));
        return Ok(new ApiResponse<IEnumerable<ShortenedUrlResponse>>(true, "OK", responses));
    }

    // Get a shortened URL by its internal ID
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShortenedUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(new ApiResponse<object>(false, $"URL with ID '{id}' not found."));

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new ApiResponse<ShortenedUrlResponse>(true, "OK", result.MapToResponse(baseUrl)));
    }

    // Delete a shortened URL
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, $"URL with ID '{id}' not found."));

        _logger.LogInformation("Deleted short URL with ID: {Id}", id);
        return Ok(new ApiResponse<object>(true, "URL deleted successfully."));
    }

    // Get click stats for a short code
    [HttpGet("{shortCode}/stats")]
    [ProducesResponseType(typeof(ApiResponse<UrlStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStats(string shortCode)
    {
        var result = await _service.GetByShortCodeAsync(shortCode);
        if (result is null)
            return NotFound(new ApiResponse<object>(false, $"Short code '{shortCode}' not found."));

        var stats = new UrlStatsResponse(
            result.ShortCode,
            result.OriginalUrl,
            result.ClickCount,
            result.CreatedAt,
            result.LastClickedAt
        );

        return Ok(new ApiResponse<UrlStatsResponse>(true, "OK", stats));
    }
}

[ApiController]
[Route("r")]
public class RedirectController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly ILogger<RedirectController> _logger;

    public RedirectController(IUrlShortenerService service, ILogger<RedirectController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Redirect to the original URL by short code
    [HttpGet("{shortCode}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectURL(string shortCode)
    {
        var url = await _service.GetByShortCodeAsync(shortCode);
        if (url is null)
            return NotFound(new ApiResponse<object>(false, $"Short code '{shortCode}' not found."));

        await _service.IncrementClickCountAsync(shortCode, DateTime.UtcNow);
        _logger.LogInformation("Redirecting {ShortCode} -> {OriginalUrl}", shortCode, url.OriginalUrl);

        return Redirect(url.OriginalUrl);
    }
}

public static class MappingExtensions
{
    public static ShortenedUrlResponse MapToResponse(this ShortenedUrl url, string baseUrl) =>
        new(url.Id, url.OriginalUrl, url.ShortCode,
            $"{baseUrl}/r/{url.ShortCode}",
            url.CreatedAt, url.ClickCount, url.Alias);
}
