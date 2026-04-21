using Chirp.Web;

namespace Chirp.TestIntegration;

public class MetricsTests : IClassFixture<MetricsTestFixture>
{
    private HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public MetricsTests(MetricsTestFixture fixture)
    {
        _client = fixture._client;
        _factory = fixture._factory;
    }

    private async Task PrepareDatabase()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
            DbInitializer.SeedDatabase(dbContext);
        }
    }

    [Fact]
    public async Task PrometheusMetrics_CheckValuesInternally()
    {
        await PrepareDatabase();
        await Task.Delay(400);

        Assert.Equal(12, MetricsRegistry.TotalUsers.Value);
        Assert.Equal(602, MetricsRegistry.TotalCheepsPosted.Value);
        Assert.Equal(1.5, MetricsRegistry.AverageFollowers.Value);
        Assert.Equal(1.5, MetricsRegistry.MedianFollowers.Value);
    }

    [Fact]
    public async Task PrometheusMetrics_CheckEndpoint()
    {
        await PrepareDatabase();
        await Task.Delay(400);

        var response = await _client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        List<string> expectedSubstrings = 
        [
            "minitwit_total_users 12",
            "minitwit_total_cheeps_posted 602",
            "minitwit_active_users 0",
            "minitwit_average_followers 1.5",
            "minitwit_median_followers 1.5",
        ];

        foreach (var expectedSubstring in expectedSubstrings)
        {
            Assert.Contains(expectedSubstring, body);
        }
    }
}
