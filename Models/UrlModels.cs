namespace UrlShortener.Models;

public class ShortenedUrl
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ClickCount { get; set; }
    public string? Alias { get; set; }
    public DateTime? LastClickedAt { get; set; }
}

public record CreateUrlRequest(
    string OriginalUrl,
    string? Alias = null
);

public record ShortenedUrlResponse(
    Guid Id,
    string OriginalUrl,
    string ShortCode,
    string ShortUrl,
    DateTime CreatedAt,
    int ClickCount,
    string? Alias
);

public record UrlStatsResponse(
    string ShortCode,
    string OriginalUrl,
    int ClickCount,
    DateTime CreatedAt,
    DateTime? LastClickedAt
);

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data = default
);
