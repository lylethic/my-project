using System;

namespace Play.Infrastructure.Common.Utilities;

public static class Otp
{
    public static string GenerateOTP(int length = 6)
    {
        var rng = new Random();
        return string.Concat(Enumerable.Range(0, length).Select(_ => rng.Next(0, 10).ToString()));
    }
}
