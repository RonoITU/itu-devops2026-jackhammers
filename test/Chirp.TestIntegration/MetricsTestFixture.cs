using Chirp.Web;
using Microsoft.AspNetCore.Hosting;

namespace Chirp.TestIntegration;

public class MetricsTestFixture : IDisposable
{
    public HttpClient _client;
    public WebApplicationFactory<Program> _factory;
    private readonly PostgreSqlContainer _postgresContainer;

    public MetricsTestFixture()
    {
        // Initialize the PostgreSQL container with desired configuration
        _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // Start the PostgreSQL container (This order is important: start the db container before creating the factory)
        _postgresContainer.StartAsync().Wait();

        // Create the factory with the postgresql container's connection string
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure for frequent metrics updates.
                services.Configure<MetricsUpdaterOptions>(o => 
                {
                    o.Interval = TimeSpan.FromMilliseconds(100);
                });

                // Re-register with test PostgreSQL
                services.AddDbContext<CheepDBContext>(options =>
                {
                    options.UseNpgsql(_postgresContainer.GetConnectionString());
                });
            });
        });

        _client = _factory.CreateClient();

        // Ensure database is created and migrated
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
        dbContext.Database.MigrateAsync().Wait();
    }

    public async void Dispose()
    {
        GC.SuppressFinalize(this);
        // Stop and clean up the container
        _postgresContainer.StopAsync().Wait();
        _postgresContainer.DisposeAsync().AsTask().Wait();
    }

}