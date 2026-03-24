using Prometheus;

namespace Chirp.Web;

public static class MetricsRegistry
{
    public static readonly Gauge TotalUsers =
        Metrics.CreateGauge("minitwit_total_users", "Number of total users");

    public static readonly Gauge TotalCheepsPosted =
        Metrics.CreateGauge("minitwit_total_cheeps_posted", "Number of cheeps in the system");
    
    public static readonly Gauge ActiveUsers =
        Metrics.CreateGauge("minitwit_active_users", "Users who posted in the last 30 days");

    public static readonly Gauge AverageFollowers =
        Metrics.CreateGauge("minitwit_average_followers", "Average number of followers per user");

    public static readonly Gauge MedianFollowers =
        Metrics.CreateGauge("minitwit_median_followers", "Median number of followers per user");

    // Labelled gauge so we can expose multiple authors
    public static readonly Gauge MostFollowed =
        Metrics.CreateGauge("minitwit_most_followed", "Follower count for top users", "author");

}
