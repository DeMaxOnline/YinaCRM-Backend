using System.ComponentModel.DataAnnotations;

namespace YinaCRM.Infrastructure.Caching;

public sealed class RedisOptions
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public string KeyPrefix { get; init; } = "yinacrm";

    public bool AllowAdmin { get; init; }

    public TimeSpan? DefaultAbsoluteExpiration { get; init; }

    public TimeSpan? DefaultSlidingExpiration { get; init; }
}
