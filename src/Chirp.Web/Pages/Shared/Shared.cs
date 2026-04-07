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
        else if (timeDifference.TotalMinutes < 60)
        {
            if ((int)timeDifference.TotalMinutes == 1)
            {
                return $"{(int)timeDifference.TotalMinutes} minute ago";
            }
            return $"{(int)timeDifference.TotalMinutes} minutes ago";
        }
        else if (timeDifference.TotalHours < 24)
        {
            if ((int)timeDifference.TotalHours == 1)
            {
                return $"{(int)timeDifference.TotalHours} hour ago";
            }
            return $"{(int)timeDifference.TotalHours} hours ago";
        }
        else if (timeDifference.TotalDays < 30)
        {
            if ((int)timeDifference.TotalDays == 1)
            {
                return $"{(int)timeDifference.TotalDays} day ago";
            }
            return $"{(int)timeDifference.TotalDays} days ago";
        }
        else if (timeDifference.TotalDays < 365)
        {
            if ((int)(timeDifference.TotalDays / 30) == 1)
            {
                return $"{(int)(timeDifference.TotalDays / 30)} month ago";
            }
            return $"{(int)(timeDifference.TotalDays / 30)} months ago";
        }
        else
        {
            if ((int)(timeDifference.TotalDays / 365) == 1)
            {
                return $"{(int)(timeDifference.TotalDays / 365)} year ago";
            }
            return $"{(int)(timeDifference.TotalDays / 365)} years ago";
        }
    }
}
