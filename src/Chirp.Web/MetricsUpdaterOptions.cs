namespace Chirp.Web;

public class MetricsUpdaterOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}
