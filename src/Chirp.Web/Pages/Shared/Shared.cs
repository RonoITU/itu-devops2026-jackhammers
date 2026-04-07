using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chirp.Web.Pages;

public static class Shared
{
    private static readonly Regex _ConvertLinksToAnchors_regex = new Regex(@"((http|https):\/\/)?(www\.)?[a-zA-Z0-9-]+\.[a-zA-Z]{2,}(\S*[^.,\s])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, matchTimeout: TimeSpan.FromMilliseconds(500));

    /// <summary>
    /// Converts URLs in a user-written message to clickable HTML hyperlinks.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ConvertLinksToAnchors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Replace URLs with anchor tags
        return _ConvertLinksToAnchors_regex.Replace(text, match => $"<a href=\"{match.Value}\" target=\"_blank\">{match.Value}</a>");
    }

    /// <summary>
    /// Returns a timestamp displayed in the style as ie "posted 2 hours ago"
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns>Time</returns>
    public static string GetFormattedTimeStamp(string timeStamp)
    {
        if (!DateTime.TryParse(timeStamp, out DateTime timeStampDateTime))
        {
            return "Invalid date";
        }

        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"));
        var localizedTimeStamp = TimeZoneInfo.ConvertTimeFromUtc(timeStampDateTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"));
        
        var timeDifference = timeNow - localizedTimeStamp;
        
        if (timeDifference.TotalSeconds < 60)
        {
            return "just now";
        }
        if (timeDifference.TotalMinutes < 60)
        {
            var n = (int) timeDifference.TotalMinutes;
            return $"{n} minute{(n == 1 ? "" : "s")} ago";
        }
        if (timeDifference.TotalHours < 24)
        {
            var n = (int) timeDifference.TotalHours;
            return $"{n} hour{(n == 1 ? "" : "s")} ago";
        }
        if (timeDifference.TotalDays < 30)
        {
            var n = (int) timeDifference.TotalDays;
            return $"{n} day{(n == 1 ? "" : "s")} ago";
        }
        if (timeDifference.TotalDays < 365)
        {
            var n = (int) (timeDifference.TotalDays / 30);
            return $"{n} month{(n == 1 ? "" : "s")} ago";
        }
        else
        {
            var n = (int) (timeDifference.TotalDays / 365);
            return $"{n} year{(n == 1 ? "" : "s")} ago";
        }
    }
}
