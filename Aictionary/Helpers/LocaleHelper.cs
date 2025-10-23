using System;
using System.Globalization;
using Aictionary.Models;

namespace Aictionary.Helpers;

public static class LocaleHelper
{
    public static DictionaryDownloadSource GetDefaultDownloadSource()
    {
        try
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var timeZone = TimeZoneInfo.Local;

            // Check if system is in Simplified Chinese and UTC+8
            var isSimplifiedChinese = currentCulture.Name.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                                     currentCulture.Name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase);

            var isUtcPlus8 = timeZone.BaseUtcOffset.TotalHours == 8;

            if (isSimplifiedChinese && isUtcPlus8)
            {
                return DictionaryDownloadSource.Gitee;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocaleHelper] Error detecting locale: {ex.Message}");
        }

        return DictionaryDownloadSource.GitHub;
    }
}
