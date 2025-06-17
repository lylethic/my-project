using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Play.Infrastructure.Common.Caching;

public static class CacheOptions
{
    public static DistributedCacheEntryOptions DefaultExpiration =>
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20) };
}
