using Chirp.Core;
using Prometheus;

namespace Chirp.Web;

public static class MetricsRegistry
{
    public static readonly Gauge TotalUsers =
        Metrics.CreateGauge("minitwit_total_users", "Number of total users");

    public static readonly Gauge TotalCheepsPosted =
        Metrics.CreateGauge("minitwit_total_cheeps_posted", "Numbers of cheeps in the system");

}
