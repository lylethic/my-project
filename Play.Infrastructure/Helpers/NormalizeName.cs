using System;
using System.Globalization;
using System.Text;

namespace Play.Infrastructure.Helpers;

public static class NormalizeName
{
  public static string ImplNormalizeName(string name)
  {
    string normalizedName = NormalizeVietnameseName(name);
    Console.WriteLine("Normalized Name: " + normalizedName);
    return normalizedName;
  }

  public static string NormalizeVietnameseName(string name)
  {
    // Remove accents
    string normalizedName = RemoveDiacritics(name);

    // Trim whitespace
    normalizedName = normalizedName.Trim();

    // Capitalize each part of the name
    normalizedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalizedName.ToLower());

    return normalizedName;
  }

  private static string RemoveDiacritics(string text)
  {
    var normalizedString = text.Normalize(NormalizationForm.FormD);
    var stringBuilder = new StringBuilder();

    foreach (char c in normalizedString)
    {
      var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
      if (unicodeCategory != UnicodeCategory.NonSpacingMark)
      {
        stringBuilder.Append(c);
      }
    }

    return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
  }
}
