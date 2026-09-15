using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Domain;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Replace Vietnamese 'đ' / 'Đ'
        text = text.Replace("đ", "d").Replace("Đ", "D");

        // Normalize and remove diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Replace any characters other than alphanumeric with hyphen
        var clean = new StringBuilder();
        var prevIsHyphen = false;

        foreach (var ch in result)
        {
            if (char.IsLetterOrDigit(ch))
            {
                clean.Append(ch);
                prevIsHyphen = false;
            }
            else if (!prevIsHyphen)
            {
                clean.Append('-');
                prevIsHyphen = true;
            }
        }

        var slug = clean.ToString().Trim('-');
        if (slug.Length > 120)
        {
            slug = slug[..120].TrimEnd('-');
        }

        return slug;
    }
}
