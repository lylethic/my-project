using System;

namespace Play.Infrastructure.Common.Utilities;

public static class RandomNumber
{
    public static string GenerateRandomNumberList(int length = 6)
    {
        const string validChars = "0123456789";
        var random = new Random();
        var numbers = new char[length];
        for (int i = 0; i < length; i++)
        {
            numbers[i] = validChars[random.Next(validChars.Length)];
        }
        return new string(numbers);
    }
}
