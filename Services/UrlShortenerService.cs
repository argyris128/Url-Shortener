using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Services;

public interface IUrlShortenerService
{
    Task<ShortenedUrl?> CreateShortUrlAsync(string originalUrl, string? alias = null);
    Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode);
    Task<ShortenedUrl?> GetByIdAsync(Guid id);
    Task<IEnumerable<ShortenedUrl>> GetAllAsync();
    Task<bool> DeleteAsync(Guid id);
    Task IncrementClickCountAsync(string shortCode, DateTime clickedAt);
}

public class UrlShortenerService : IUrlShortenerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UrlShortenerService> _logger;

    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    public UrlShortenerService(AppDbContext db, ILogger<UrlShortenerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ShortenedUrl?> CreateShortUrlAsync(string originalUrl, string? alias = null)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
            return null;

        var shortCode = alias ?? GenerateShortCode();

        var codeExists = await _db.ShortenedUrls
            .AnyAsync(u => u.ShortCode == shortCode);

        if (codeExists)
            return null;

        var shortened = new ShortenedUrl
        {
            OriginalUrl = originalUrl,
            ShortCode = shortCode,
            CreatedAt = DateTime.UtcNow,
            ClickCount = 0,
            Alias = alias
        };

        _db.ShortenedUrls.Add(shortened);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created {ShortCode} -> {OriginalUrl}", shortCode, originalUrl);
        return shortened;
    }

    public async Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode) =>
        await _db.ShortenedUrls
            .FirstOrDefaultAsync(u => u.ShortCode.ToLower() == shortCode.ToLower());

    public async Task<ShortenedUrl?> GetByIdAsync(Guid id) =>
        await _db.ShortenedUrls.FindAsync(id);

    public async Task<IEnumerable<ShortenedUrl>> GetAllAsync() =>
        await _db.ShortenedUrls
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entry = await _db.ShortenedUrls.FindAsync(id);
        if (entry is null) return false;

        _db.ShortenedUrls.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task IncrementClickCountAsync(string shortCode, DateTime clickedAt)
    {
        await _db.ShortenedUrls
            .Where(u => u.ShortCode == shortCode)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.ClickCount, u => u.ClickCount + 1)
                .SetProperty(u => u.LastClickedAt, clickedAt));
    }

    private static string GenerateShortCode()
    {
        var random = new Random();
        return new string(Enumerable.Range(0, CodeLength)
            .Select(_ => Chars[random.Next(Chars.Length)])
            .ToArray());
    }
}
