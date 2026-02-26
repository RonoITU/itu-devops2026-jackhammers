namespace Chirp.TestIntegration;

public abstract class AbstractIntegration : IAsyncLifetime
{
    protected HttpClient _client = null!;
    protected WebApplicationFactory<Program> _factory = null!;
    protected readonly PostgreSqlContainer _postgresContainer;

    public AbstractIntegration()
    {
        // Initialize the PostgreSQL container with desired configuration
        _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// For reference see IntegrationTests.cs
    /// </summary>
    /// <returns></returns>
    public async Task InitializeAsync()
    {
        // Start the PostgreSQL container (This order is important: start the db container before creating the factory)
        await _postgresContainer.StartAsync();

        // Create the factory with the postgresql container's connection string
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
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
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Stop and clean up the container
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

}