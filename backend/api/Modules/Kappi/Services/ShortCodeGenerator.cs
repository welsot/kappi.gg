using api.Modules.Kappi.Repository;

namespace api.Modules.Kappi.Services;

public class ShortCodeGenerator(
    IAnonymousGalleryRepository anonymousGalleries,
    IGalleryRepository galleries,
    ILogger<ShortCodeGenerator> logger
)
{
    private const string Chars = "abcdefghkmnopqrstuwxyz23456789";
    private const int InitialLength = 4;
    private const int MaxLength = 10;
    private const int MaxRetries = 10;

    public async Task<string> GenerateUniqueShortCodeAsync()
    {
        var length = InitialLength;

        while (length <= MaxLength)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                var shortCode = GenerateShortCode(length);

                // Check if short code exists in both tables
                var existsInAnonymous = await anonymousGalleries.ExistsByShortCodeAsync(shortCode);
                var existsInGalleries = await galleries.ExistsByShortCodeAsync(shortCode);

                if (!existsInAnonymous && !existsInGalleries)
                {
                    logger.LogInformation("Generated unique short code: {ShortCode} with length {Length}", shortCode, length);
                    return shortCode;
                }

                logger.LogDebug("Short code collision detected: {ShortCode}, retrying...", shortCode);
            }

            // Increase length if we couldn't find a unique code after max retries
            length++;
            logger.LogWarning("Increasing short code length to {Length} due to collisions", length);
        }

        throw new InvalidOperationException($"Could not generate unique short code after trying up to length {MaxLength}");
    }

    private static string GenerateShortCode(int length)
    {
        var random = new Random();
        var stringChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = Chars[random.Next(Chars.Length)];
        }

        return new string(stringChars);
    }
}
